using Chameleon_Hub.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
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

        private void HexSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformHexSearch(HexSearchBox.Text);
            }
        }
        private void HexSearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformHexSearch(HexSearchBox.Text);
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

        private void PerformHexSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            string hexContent = textBoxHex.Text;
            int index = hexContent.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                textBoxHex.Focus();
                textBoxHex.Select(index, text.Length);
                textBoxHex.ScrollToLine(textBoxHex.GetLineIndexFromCharacterIndex(index));
            }
            else
            {
                MessageBox.Show("Text not found in hex view.");
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
                FontWeight = FontWeights.Bold,
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
                        Foreground = Brushes.White
                    };

                    // Recursive call to load inside this folder
                    LoadFolderToTree(dirNode, dir);

                    parentNode.Items.Add(dirNode);
                }

                // Add only .bndl files (ignore other files)
                foreach (var file in Directory.GetFiles(folderPath, "*.bndl"))
                {
                    var fileNode = new TreeViewItem
                    {
                        Header = System.IO.Path.GetFileName(file),
                        Tag = file,
                        Foreground = Brushes.White
                    };

                    parentNode.Items.Add(fileNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading folder '{folderPath}': {ex.Message}");
            }
        }

        public void LoadBNDL(string filePath)
        {
            treeBndlFiles.Items.Clear();

            var bndl = new NFSMWBNDL(filePath);
            var rootNode = new TreeViewItem
            {
                Header = System.IO.Path.GetFileName(filePath),
                FontWeight = FontWeights.Bold
            };

            foreach (var entry in bndl.Entries)
            {
                var entryNode = new TreeViewItem
                {
                    Header = entry.Name,
                    Tag = entry
                };

                var containedFiles = entry.GetContainedFiles();
                foreach (var (name, content) in containedFiles)
                {
                    var childNode = new TreeViewItem
                    {
                        Header = name,
                        Tag = content
                    };
                    entryNode.Items.Add(childNode);
                }

                rootNode.Items.Add(entryNode);
            }

            treeBndlFiles.Items.Add(rootNode);
            rootNode.IsExpanded = true;
        }

        private void TreeBndlFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = treeBndlFiles.SelectedItem as TreeViewItem;
            if (selectedItem?.Tag is byte[] data)
            {
                // Show DAT file contents or hex here
                textBoxAscii.Text = ConvertToAscii(data); // Just an example method
                textBoxHex.Text = ConvertToHex(data);     // Placeholder
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
            // Convert bytes to ASCII string (replace invalid chars with '.')
            var chars = Data.Select(b => (b >= 32 && b <= 126) ? (char)b : '.').ToArray();
            return new string(chars);
        }

        public string ToHex()
        {
            if (Data == null) return string.Empty;
            // Convert bytes to hex string (space-separated for readability)
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
