using Chameleon_Hub;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Chameleon_Hub
{
public class NFSMWBNDL
{
    public string FilePath { get; private set; }
    public class BundleEntry
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int CountBlock { get; set; }
        public int Count { get; set; }
        public int DecompressedSize1 { get; set; }
        public int DecompressedSize2 { get; set; }
        public int CompressedSize1 { get; set; }
        public int CompressedSize2 { get; set; }
        public int Position1 { get; set; }
        public int Position2 { get; set; }
        public byte[] Data1 { get; set; }
        public byte[] Data2 { get; set; }


        public List<Tuple<string, byte[]>> GetContainedFiles()
        {
            var files = new List<Tuple<string, byte[]>>();
            string baseName = Name;

            if (CountBlock != 0)
            {
                baseName += $"_{CountBlock}";
                if (Count != 0)
                    baseName += $"_{Count}";
            }
            else if (Count != 0)
            {
                baseName += $"_0_{Count}";
            }

            if (Data1 != null && Data1.Length > 0)
            {
                string type1 = FileName.IdentifyContent(Data1); // Use your header recognizer
                string ext1 = GetExtension(type1);
                files.Add(Tuple.Create($"{baseName}_1{ext1}", Data1));
            }

            if (Data2 != null && Data2.Length > 0)
            {
                string type2 = FileName.IdentifyContent(Data2); // Use your header recognizer
                string ext2 = GetExtension(type2);
                files.Add(Tuple.Create($"{baseName}_2{ext2}", Data2));
            }

            return files;
        }
        private string GetExtension(string typeName)
        {
            return typeName switch
            {
                "Model (.mdl)" => ".mdl.dat",
                "Texture (.tex)" => ".tex.dat",
                "Vehicle Config (.veh)" => ".veh.dat",
                "Resource Descriptor (.resd)" => ".resd.dat",
                _ => ".unknown.dat"
            };
        }
    }


    public List<BundleEntry> Entries { get; private set; } = new List<BundleEntry>();
    public byte[] IDsTable { get; private set; }
    public byte[] ResourceStringTable { get; private set; }

    private int _block1Start;
    private int _block2Start;
    private int _compressionFlag;

    public NFSMWBNDL(string filePath)
    {
        Load(filePath);
    }

    public void Load(string filePath)
    {
        FilePath = filePath;
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
        if (magic != "bnd2")
            throw new InvalidDataException("Not a valid BNDL file.");

        fs.Seek(2, SeekOrigin.Current);
        var version = br.ReadBytes(2);
        if (version[0] != 1 || version[1] != 0)
            throw new NotSupportedException("Only PC version is supported.");

        fs.Seek(0xC, SeekOrigin.Begin);
        int numIDs = br.ReadInt32();
        int idsTabStart = br.ReadInt32();
        _block1Start = br.ReadInt32();
        _block2Start = br.ReadInt32();
        int block3Start = br.ReadInt32();
        int fullSize = br.ReadInt32();
        _compressionFlag = br.ReadInt32();

        // Read IDs Table
        fs.Seek(idsTabStart, SeekOrigin.Begin);
        IDsTable = br.ReadBytes(_block1Start - idsTabStart);

        // Read entries
        ReadEntries(br, numIDs, idsTabStart);

        // Remaining data is likely the resource string table
        if (fs.Position < fs.Length)
            ResourceStringTable = br.ReadBytes((int)(fs.Length - fs.Position));
    }

    private void ReadEntries(BinaryReader br, int numIDs, int idsTabStart)
    {
        for (int i = 0; i < numIDs; i++)
        {
            br.BaseStream.Seek(idsTabStart + i * 0x48, SeekOrigin.Begin);

            var entry = new BundleEntry
            {
                Name = BitConverter.ToString(br.ReadBytes(4)).Replace("-", "_"),
                CountBlock = br.ReadByte()
            };

            br.ReadByte(); // Skip
            entry.Count = br.ReadByte();
            br.ReadByte(); // Skip

            entry.DecompressedSize1 = AdjustSize(br.ReadInt32());
            entry.DecompressedSize2 = AdjustSize(br.ReadInt32());

            br.BaseStream.Seek(8, SeekOrigin.Current); // Skip unused

            entry.CompressedSize1 = br.ReadInt32();
            entry.CompressedSize2 = br.ReadInt32();

            br.BaseStream.Seek(8, SeekOrigin.Current); // Skip unused

            entry.Position1 = br.ReadInt32();
            entry.Position2 = br.ReadInt32();

            br.BaseStream.Seek(8, SeekOrigin.Current); // Skip unknown

            br.ReadInt32(); // called_block (not used)

            entry.Type = BitConverter.ToString(br.ReadBytes(4)).Replace("-", "_");

            br.ReadUInt16(); // Num_int_IDs (not used)

            // Read Data1 block if exists
            if (entry.DecompressedSize1 > 0 && entry.Position1 >= 0)
            {
                br.BaseStream.Seek(_block1Start + entry.Position1, SeekOrigin.Begin);
                entry.Data1 = ReadDataBlock(br, entry.CompressedSize1);
            }

            // Read Data2 block if exists
            if (entry.DecompressedSize2 > 0 && entry.Position2 >= 0)
            {
                br.BaseStream.Seek(_block2Start + entry.Position2, SeekOrigin.Begin);
                entry.Data2 = ReadDataBlock(br, entry.CompressedSize2);
            }

            Entries.Add(entry);
        }
    }

    private byte[] ReadDataBlock(BinaryReader br, int compressedSize)
    {
        if (compressedSize <= 0)
            return null;

        switch (_compressionFlag)
        {
            case 2: // uncompressed
                return br.ReadBytes(compressedSize);

            case 1: // zlib compressed
                var compressedData = br.ReadBytes(compressedSize);
                return DecompressData(compressedData);

            case 9: // unhandled compression - fallback
                return br.ReadBytes(compressedSize);

            default:
                throw new NotSupportedException($"Unsupported compression flag: {_compressionFlag}");
        }
    }

    private byte[] DecompressData(byte[] compressedData)
    {
        try
        {
            using var input = new MemoryStream(compressedData, 2, compressedData.Length - 6);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decompression failed: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    private int AdjustSize(int size)
    {
        unchecked
        {
            for (int i = 1; i <= 9; i++)
            {
                int flag = i << 28;
                if (size >= flag)
                    return size - flag;
            }
        }
        return size;
    }
        
        public class FileData
    {
        public byte[] Data { get; set; }
        public int Offset { get; set; }
        public int Size => Data?.Length ?? 0;
    }
    public class BndlFolder
    {
        public string Name { get; set; }
        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();
        // Children can be BndlFolder or DatFileReference, so we use object for flexibility
    }
}
}
