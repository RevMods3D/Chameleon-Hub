using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private void LoadHexBytes(byte[] data)
        {
            if (FileTabControl.SelectedItem is TabItem tab &&
                tab.Content is WpfHexaEditor.HexEditor editor)
            {
                editor.Stream = new MemoryStream(data);
            }
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
        private void HexSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformHexSearch();
            }
        }

        private void HexSearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformHexSearch();
        }

        private void PerformHexSearch()
        {
            string searchText = HexSearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchText)) return;

            // Add your hex search logic here if supported by the control.
            MessageBox.Show($"Searching for: {searchText}");
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

            // Directly load the contents into the root of the tree
            foreach (var dir in Directory.GetDirectories(gamePath))
            {
                LoadTreeNode(treeBndlFiles, dir, Path.GetFileName(dir));
            }
            foreach (var file in Directory.GetFiles(gamePath, "*.bndl"))
            {
                LoadTreeNode(treeBndlFiles, file, Path.GetFileName(file));
            }
        }

        /*private void LoadFolderToTree(TreeViewItem parentNode, string folderPath)
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
        }*/

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

            foreach (var entry in bndl.Entries)
            {
                var entryNode = new TreeViewItem
                {
                    Header = entry.Name,
                    Tag = entry,
                    Foreground = Brushes.White
                };

                var containedFiles = entry.GetContainedFiles();

                foreach (var (fileName, content) in containedFiles)
                {
                    // Get base name without extension, so extensions like .dat or .bndl are hidden in UI
                    var displayName = System.IO.Path.GetFileNameWithoutExtension(fileName);

                    var childNode = new TreeViewItem
                    {
                        Header = displayName,
                        Tag = content,
                        Foreground = Brushes.White
                    };

                    if (IsLikelyDatFile(content))
                    {
                        childNode.Items.Add(null);
                        childNode.Expanded += DatNode_Expanded;
                    }

                    entryNode.Items.Add(childNode);
                }

                bndlNode.Items.Add(entryNode);
            }

            bndlNode.IsExpanded = true;
        }

        public IEnumerable<(string Name, byte[] Content)> ParseDatEntries(byte[] datFileContent)
        {
            var entries = GetDatEntries(datFileContent);
            if (entries.Count > 1)
            {
                foreach (var entry in entries)
                {
                    var name = entry.ResourceId ?? $"DAT_0x{entry.Offset:X}";
                    var content = datFileContent.Skip(entry.Offset).Take(entry.Length).ToArray();
                    yield return (name, content);
                }
            }
            else
            {
                yield return ("RawData", datFileContent);
            }
        }

        private void TreeBndlFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeBndlFiles.SelectedItem is TreeViewItem selectedItem)
            {
                string fileName = selectedItem.Header.ToString();

                // Try to get inline data and open it
                if (selectedItem.Tag is byte[] inlineData)
                {
                    AddHexEditorTab(fileName, inlineData);
                }
                // If it's a file on disk (like from base folder), open from path
                else if (selectedItem.Tag is string path && File.Exists(path))
                {
                    // ⛔️ Skip .bndl files
                    if (Path.GetExtension(path).Equals(".bndl", StringComparison.OrdinalIgnoreCase))
                        return;

                    byte[] fileData = File.ReadAllBytes(path);
                    AddHexEditorTab(System.IO.Path.GetFileName(path), fileData);
                }
            }
        }

        private void DatNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem node) return;

            e.Handled = true;

            if (node.Items.Count == 1 && node.Items[0] == null)
            {
                node.Items.Clear();

                if (node.Tag is byte[] data)
                {
                    var datEntries = GetDatEntries(data);

                    foreach (var entry in datEntries)
                    {
                        var name = entry.ResourceId ?? $"DAT_0x{entry.Offset:X}";
                        var content = data.Skip(entry.Offset).Take(entry.Length).ToArray();

                        var childNode = new TreeViewItem
                        {
                            Header = name,
                            Tag = content,
                            Foreground = Brushes.White
                        };

                        if (IsLikelyDatFile(content))
                        {
                            childNode.Items.Add(null);
                            childNode.Expanded += DatNode_Expanded;
                        }

                        node.Items.Add(childNode);
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

        private void TreeBndlFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeBndlFiles.SelectedItem is TreeViewItem selectedItem)
            {
                string fileName = selectedItem.Header.ToString();

                if (selectedItem.Tag is string path && File.Exists(path))
                {
                    byte[] fileData = File.ReadAllBytes(path);

                    // Create new tab
                    AddHexEditorTab(fileName, fileData);
                }
                else if (selectedItem.Tag is byte[] inlineData)
                {
                    AddHexEditorTab(fileName, inlineData);
                }
            }
        }

        private void AddHexEditorTab(string title, byte[] data)
        {
            foreach (TabItem existingTab in FileTabControl.Items)
            {
                if (existingTab.Header is StackPanel header &&
                    header.Children[0] is TextBlock tb &&
                    tb.Text == title)
                {
                    FileTabControl.SelectedItem = existingTab;
                    return;
                }
            }

            var hexEditor = new WpfHexaEditor.HexEditor
            {
                Stream = new MemoryStream(data),
                ReadOnlyMode = true,
                Background = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255)),
                Foreground = Brushes.Black,
                Width = 590,
                Height = 400,
                Margin = new Thickness(5)
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var titleText = new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var closeBtn = new Button
            {
                Content = "×",
                Width = 16,
                Height = 16,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            var tabItem = new TabItem { Content = hexEditor };
            closeBtn.Click += (s, e) => FileTabControl.Items.Remove(tabItem);

            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(closeBtn);
            tabItem.Header = headerPanel;

            FileTabControl.Items.Add(tabItem);
            FileTabControl.SelectedItem = tabItem;
        }

        private void LoadTreeNode(ItemsControl parentNode, object tag, string displayName, byte[] data = null)
        {
            var node = new TreeViewItem
            {
                Header = displayName,
                Tag = tag,
                Foreground = Brushes.White
            };

            if (data != null)
            {
                if (IsLikelyDatFile(data))
                {
                    node.Items.Add(null); // Enable expand arrow
                    node.Expanded += DatNode_Expanded;
                }
            }
            else if (tag is string path)
            {
                if (Directory.Exists(path))
                {
                    // Folder: Recursively load its children
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        LoadTreeNode(node, dir, Path.GetFileName(dir));
                    }
                    foreach (var file in Directory.GetFiles(path, "*.bndl"))
                    {
                        var bndlNode = new TreeViewItem
                        {
                            Header = Path.GetFileName(file),
                            Tag = file,
                            Foreground = Brushes.White
                        };
                        bndlNode.Items.Add(null);
                        bndlNode.Expanded += BndlNode_Expanded;
                        node.Items.Add(bndlNode);
                    }
                }
                else if (File.Exists(path))
                {
                    var fileData = File.ReadAllBytes(path);
                    if (Path.GetExtension(path).Equals(".dat", StringComparison.OrdinalIgnoreCase) && IsLikelyDatFile(fileData))
                    {
                        node.Items.Add(null);
                        node.Expanded += DatNode_Expanded;
                    }
                }
            }

            parentNode.Items.Add(node);
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

        public byte[] Data { get; set; }  // Or however you store the raw content

        

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
