using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Chameleon_Hub
{
    public class DatFileData
    {
        public string FileName;
        public byte[] RawData;

        // Add parsed data fields here if needed
        public string TextPreview => Encoding.ASCII.GetString(RawData.Take(200).ToArray()); // Preview
    }
}
