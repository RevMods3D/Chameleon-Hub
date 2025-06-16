using Chameleon_Hub.Core;
using ChameleonHub.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfHexaEditor;
using static Chameleon_Hub.NFSMWBNDL;

namespace Chameleon_Hub
{
    public partial class EditorWindow : Window
    {
        private readonly string gamePath;
        public EditorWindow(string path)
        {
            InitializeComponent();
            gamePath = path;
            Loaded += EditorWindow_Loaded;
            treeBndlFiles.SelectedItemChanged += TreeBndlFiles_SelectedItemChanged;
        }


        /////////////////////////Buttons//////////////////////////////
        private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformBndlSearch(SearchBox.Text);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformBndlSearch(SearchBox.Text);
        }

        private void PerformBndlSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            foreach (TreeViewItem root in treeBndlFiles.Items)
            {
                root.IsExpanded = true;
                foreach (TreeViewItem entry in root.Items)
                {
                    entry.IsExpanded = true;

                    var headerText = entry.Header.ToString();
                    if (headerText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        entry.Foreground = Brushes.Yellow;
                    else
                        entry.Foreground = Brushes.White;
                }
            }
        }
        ///////////////////////////////////////////////////////

        private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                MessageBox.Show($"Game folder not found: {gamePath}");
                return;
            }

            treeBndlFiles.Items.Clear();

            var rootNode = new TreeViewItem
            {
                Header = System.IO.Path.GetFileName(gamePath.TrimEnd('\\', '/')),
                Foreground = Brushes.White,
                IsExpanded = true
            };

            LoadFolderToTree(rootNode, gamePath);

            treeBndlFiles.Items.Add(rootNode);
        }

        private void LoadFolderToTree(TreeViewItem parentNode, string folderPath)
        {
            try
            {
                // Add subfolders
                foreach (var dir in Directory.GetDirectories(folderPath))
                {
                    var dirNode = new TreeViewItem
                    {
                        Header = System.IO.Path.GetFileName(dir),
                        Tag = dir,
                        IsExpanded = false,
                        Foreground = Brushes.White,
                    };

                    LoadFolderToTree(dirNode, dir);

                    parentNode.Items.Add(dirNode);
                }

                // Add .bndl files only
                foreach (var file in Directory.GetFiles(folderPath, "*.bndl"))
                {
                    var fileNode = new TreeViewItem
                    {
                        Header = System.IO.Path.GetFileName(file),
                        Tag = file,
                        Foreground = Brushes.White
                    };

                    // Add a dummy child to enable the expand arrow
                    fileNode.Items.Add(null);

                    // Attach Expanded event handler
                    fileNode.Expanded += BndlNode_Expanded;

                    parentNode.Items.Add(fileNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading folder '{folderPath}': {ex.Message}");
            }
        }

        private void BndlNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem node) return;

            // Prevent event from bubbling up
            e.Handled = true;

            // Check if dummy child is still present
            if (node.Items.Count == 1 && node.Items[0] == null)
            {
                node.Items.Clear(); // Remove dummy child

                if (node.Tag is string filePath && File.Exists(filePath))
                {
                    LoadBNDL(node, filePath);
                }
            }
        }

        public void LoadBNDL(TreeViewItem bndlNode, string filePath)
        {
            var bndl = new NFSMWBNDL(filePath);

            foreach (var entry in bndl.Entries) //The printer couldn't print hehehe.
            {
                var entryNode = new TreeViewItem
                {
                    Header = entry.Name,
                    Tag = entry,
                    Foreground = Brushes.White
                };

                // Get contained files from entry
                var containedFiles = entry.GetContainedFiles();

                foreach (var (name, content) in containedFiles)
                {
                    var childNode = new TreeViewItem
                    {
                        Header = name,
                        Tag = content,
                        Foreground = Brushes.White
                    };

                    // If this contained file is itself a .dat file, parse it further
                    if (name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                    {
                        var datEntries = ParseDatEntries(content);
                        foreach (var (datName, datContent) in datEntries)
                        {
                            var datChildNode = new TreeViewItem
                            {
                                Header = datName,
                                Tag = datContent,
                                Foreground = Brushes.LightGray
                            };
                            datChildNode.Expanded += DatNode_Expanded; // <-- Needed
                            datChildNode.Items.Add(null); // So it shows the expand arrow
                            childNode.Items.Add(datChildNode);
                        }
                    }

                    entryNode.Items.Add(childNode);
                }

                bndlNode.Items.Add(entryNode);
            }

            bndlNode.IsExpanded = true;
        }

        public IEnumerable<(string Name, byte[] Content)> ParseDatEntries(byte[] datFileContent)
        {
            return new List<(string, byte[])>()
            {
                ("RawData", datFileContent)
            };
        }

        private void DatNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem node) return;

            e.Handled = true;

            // Only load contents if dummy child exists
            if (node.Items.Count == 1 && node.Items[0] == null)
            {
                node.Items.Clear();

                if (node.Tag is byte[] data)
                {
                    // Parse .dat data and add contained files if any
                    var datEntries = ParseDatEntries(data); // You need to implement this

                    if (datEntries != null)
                    {
                        foreach (var (name, content) in datEntries)
                        {
                            var childNode = new TreeViewItem
                            {
                                Header = name,
                                Tag = content,
                                Foreground = Brushes.White
                            };
                            node.Items.Add(childNode);
                        }
                    }
                    else
                    {
                        // If no further files, just keep raw data node or display hex/ascii when selected
                    }
                }
            }

            void ParseDatFile(byte[] datData, TreeViewItem parentItem)
            {
                List<DatEntry> entries = GetDatEntries(datData); // Your logic to extract offsets, sizes, etc.

                foreach (var entry in entries)
                {
                    byte[] innerData = datData.Skip(entry.Offset).Take(entry.Length).ToArray();

                    var childItem = new TreeViewItem
                    {
                        Header = entry.ResourceId ?? $"Offset_{entry.Offset:X}",
                        Tag = innerData
                    };

                    parentItem.Items.Add(childItem);

                    if (IsLikelyDatFile(innerData))
                    {
                        childItem.Header += " (DAT)";
                        ParseDatFile(innerData, childItem); // 🔁 RECURSION!
                    }
                    else
                    {
                        childItem.Header += $" ({innerData.Length} bytes)";
                    }
                }
            }
        }
        private void ParseDatFile(byte[] datData, TreeViewItem parentItem)
        {
            List<DatEntry> entries = GetDatEntries(datData); // You need this method too

            foreach (var entry in entries)
            {
                byte[] innerData = datData.Skip(entry.Offset).Take(entry.Length).ToArray();

                var childItem = new TreeViewItem
                {
                    Header = entry.ResourceId ?? $"Offset_{entry.Offset:X}",
                    Tag = innerData
                };

                parentItem.Items.Add(childItem);

                if (IsLikelyDatFile(innerData))
                {
                    childItem.Header += " (DAT)";
                    ParseDatFile(innerData, childItem); // recursion
                }
                else
                {
                    childItem.Header += $" ({innerData.Length} bytes)";
                }
            }
        }

        private struct DatEntry
        {
            public int Offset;
            public int Length;
            public string ResourceId;
        }
        private bool IsLikelyDatFile(byte[] data)
        {
            return data.Length > 3 &&
                   data[0] == 0x44 && data[1] == 0x41 && data[2] == 0x54; // ASCII: DAT
        }

        /// Checks if the byte array is likely a .dat file based on its structure.
        private List<DatEntry> GetDatEntries(byte[] datData)
        {
            List<DatEntry> entries = new();
            int index = 0;

            while (index < datData.Length - 8)
            {
                // Look for the start of a "DAT" signature
                if (datData[index] == 'D' && datData[index + 1] == 'A' && datData[index + 2] == 'T')
                {
                    // Assume 4 bytes for header, 4 bytes for length (or until next DAT block)
                    int lengthGuess = datData.Length - index;

                    entries.Add(new DatEntry
                    {
                        Offset = index,
                        Length = lengthGuess, // or adjust based on actual format
                        ResourceId = $"DAT@0x{index:X}"
                    });

                    // Move forward, prevent infinite loop
                    index += lengthGuess;
                }
                else
                {
                    index++;
                }
            }

            return entries;
        }
        //////////////////////////////////////////////////////////////////////////////////

        private async void TreeBndlFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeBndlFiles.SelectedItem is TreeViewItem selectedItem)
            {
                byte[] fileData = null;

                if (selectedItem.Tag is string path && File.Exists(path))
                {
                    // Load file on background thread
                    fileData = await Task.Run(() => File.ReadAllBytes(path));

                    string alignmentInfo = AlignmentChecker.GetAlignmentInfo(System.IO.Path.GetFileName(path), fileData.Length);
                    AlignmentInfoTextBlock.Text = alignmentInfo;
                }
                else if (selectedItem.Tag is byte[] data)
                {
                    fileData = data;
                    AlignmentInfoTextBlock.Text = $"Inline data size: {data.Length} bytes";
                }

                if (fileData != null) //owo
                {
                    Data = fileData;

                    // Load data into the WPFHexaEditor
                    using (var ms = new MemoryStream(fileData))
                    {
                        HexEditor.Stream = ms;
                    }
                }
                else
                {
                    // Clear HexEditor if nothing to load
                    HexEditor.Clear();
                    AlignmentInfoTextBlock.Text = "";
                }
            }
        }

        private void TreeBndlFiles_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item)
            {
                if (item.Items.Count == 1 && item.Items[0] is string) // dummy node
                {
                    item.Items.Clear();

                    byte[] fileData = null;

                    if (item.Tag is string path && path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                    {
                        fileData = File.ReadAllBytes(path);
                    }
                    else if (item.Tag is byte[] data)
                    {
                        fileData = data;
                    }

                    if (fileData != null)
                    {
                        var entries = GetDatEntries(fileData);
                        foreach (var entry in entries)
                        {
                            TreeViewItem child = new()
                            {
                                Header = $"{entry.ResourceId} @ 0x{entry.Offset:X}",
                                Tag = fileData.Skip(entry.Offset).Take(entry.Length).ToArray()
                            };

                            // Add dummy for lazy-loading nested .dat
                            if (entry.ResourceId.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                            {
                                child.Items.Add("Loading...");
                            }

                            item.Items.Add(child);
                        }
                    }
                }
            }
        }

        private string ConvertToAscii(byte[] data)
        {
            return new string(data.Select(b => (b >= 32 && b <= 126) ? (char)b : '.').ToArray());
        }

        private string ConvertToHex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }

        public byte[] Data { get; set; }  // Or however you store the raw content
        public string ToAscii()
        {
            if (Data == null) return string.Empty;
            return new string(Data.Select(b => (b >= 32 && b <= 126) ? (char)b : '.').ToArray());
        }

        public string ToHex()
        {
            if (Data == null) return string.Empty;
            return BitConverter.ToString(Data).Replace("-", " ");
        }

        

        private static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new(AppendDirectorySeparatorChar(basePath));
            Uri fullUri = new(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                return path + System.IO.Path.DirectorySeparatorChar;
            }
            return path;
        }
    }
}
