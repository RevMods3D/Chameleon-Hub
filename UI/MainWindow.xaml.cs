using Chameleon_Hub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace Chameleon_Hub
{
    public partial class MainWindow : Window
    {
        private readonly List<GameEntry> games = new();

        private string selectedGameExePath = null;

        public MainWindow()
        {
            InitializeComponent();

            // Load games saved previously and populate UI once
            LoadSavedGames();
        }

        #region Game Management

        private void LoadSavedGames()
        {
            var savedGames = GameStorage.Load();

            games.Clear();
            GamesListBox.Items.Clear();

            var validGames = new List<GameEntry>();

            foreach (var game in savedGames)
            {
                if (File.Exists(game.ExePath))
                {
                    // Skip CheckGameByString here to avoid reading EXE on load
                    validGames.Add(game);
                    AddGameToUI(game.ExePath, selectNewGame: false);
                }
            }

            games.Clear();
            games.AddRange(validGames);

            GameStorage.Save(games);

            GamesListBox.SelectedIndex = -1;
        }

        // Added parameter to optionally disable auto-selection (false when loading saved games)
        private void AddGameToUI(string exePath, bool selectNewGame)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };

            // Try to load custom image first
            string gameKey = GetGameKeyFromExePath(exePath); // Use your existing method
            string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", gameKey + ".png");

            System.Windows.Controls.Image image = new()
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 8, 0)
            };

            if (File.Exists(imagePath))
            {
                // Load custom PNG
                image.Source = new BitmapImage(new Uri(imagePath));
            }
            else
            {
                // Fallback to icon
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
                    using var bitmap = icon.ToBitmap();
                    image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));
                }
            }

            stackPanel.Children.Add(image);

            // Optional: show game name
            var listBoxItem = new ListBoxItem
            {
                Content = stackPanel,
                Tag = exePath
            };

            GamesListBox.Items.Add(listBoxItem);

            if (selectNewGame)
                GamesListBox.SelectedItem = listBoxItem;
        }

        private string GetGameKeyFromExePath(string path)
        {
            string fileName = Path.GetFileName(path).ToLowerInvariant();

            if (fileName.Contains("nfs13"))
                return "NFS13";
            else if (fileName.Contains("hotpursuit"))
                return "NFSHotPursuit";
            // add more cases as needed

            return null;
        }

        private bool CheckGameByString(string exePath, string signatureText, long offset)
        {
            byte[] signature = Encoding.ASCII.GetBytes(signatureText);
            byte[] buffer = new byte[signature.Length];

            using (FileStream fs = new(exePath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length < offset + signature.Length)
                    return false;

                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(buffer, 0, signature.Length);
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (buffer[i] != signature[i])
                    return false;
            }

            return true;
        }

        private void RemoveGameFromUI(string gameKey)
        {
            ListBoxItem itemToRemove = null;

            foreach (ListBoxItem item in GamesListBox.Items)
            {
                if (item.Tag as string == gameKey)
                {
                    itemToRemove = item;
                    break;
                }
            }

            if (itemToRemove != null)
            {
                GamesListBox.Items.Remove(itemToRemove);
            }
        }

        #endregion

        #region UI Event Handlers

        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (dlg.ShowDialog() == true)
            {
                string exePath = dlg.FileName;
                string tag = GetGameTagFromExePath(exePath);
                if (tag == null)
                {
                    MessageBox.Show("Unsupported game.");
                    return;
                }
                
                if (games.Any(g => g.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Game already added.");
                    return;
                }
                var game = new GameEntry { ExePath = exePath };
                games.Add(game);

                AddGameToUI(exePath, selectNewGame: true);
                GameStorage.Save(games);
            }
        }

        private string GetGameTagFromExePath(string exePath)
        {
            if (CheckGameByString(exePath, "Need for Speed(TM) Most Wanted", 0x00B96EF0 + 6))
                return "MostWanted2012";

            if (CheckGameByString(exePath, "Burnout Paradise", 0x12345678)) // Example offset
                return "BurnoutParadise";

            return null;
        }

        private void DeleteGame_Click(object sender, RoutedEventArgs e)
        {
            if (GamesListBox.SelectedItem is ListBoxItem item && item.Tag is string exePath)
            {
                // Find the GameEntry by matching the ExePath (not gameKey string) for clarity
                var gameToRemove = games.FirstOrDefault(g => g.ExePath == exePath);
                if (gameToRemove != null)
                {
                    games.Remove(gameToRemove);
                    GameStorage.Save(games);  // Save after removal
                }

                GamesListBox.Items.Remove(item);

                selectedGameExePath = null;
                OpenButton.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Please select a game to delete.");
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
             if (selectedGameExePath != null)
             {
                 var gameEntry = new GameEntry { ExePath = selectedGameExePath };
                 var loadingWindow = new LoadingWindow(gameEntry);
                 loadingWindow.Show();
                 this.Close();
             }
        }

        private void GamesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProjectsListView.Items.Clear();
            OpenButton.IsEnabled = false;
            selectedGameExePath = null;

            if (GamesListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string exePath)
            {
                string gameTag = GetGameTagFromExePath(exePath); // Fix: get tag from exe path
                LoadProjectsForGame(gameTag);

                selectedGameExePath = exePath;
                OpenButton.IsEnabled = true;
            }
        }

        #endregion

        #region Helper Methods

        private void LoadProjectsForGame(string gameTag)
        {
            ProjectsListView.Items.Clear();

            switch (gameTag)
            {
                case "MostWanted2012":
                    ProjectsListView.Items.Add("Most Wanted 2012 Project 1");
                    ProjectsListView.Items.Add("Most Wanted 2012 Project 2");
                    break;
                case "BurnoutParadise":
                    ProjectsListView.Items.Add("Burnout Paradise Project A");
                    ProjectsListView.Items.Add("Burnout Paradise Project B");
                    break;
                case "NfsHotPursuit":
                    ProjectsListView.Items.Add("NFS Hot Pursuit Project X");
                    ProjectsListView.Items.Add("NFS Hot Pursuit Project Y");
                    break;
                default:
                    ProjectsListView.Items.Add("No projects available");
                    break;
            }
        }

        #endregion

        #region Window Controls (minimize, maximize, close, drag)

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        #endregion
        public BitmapImage LoadExeIconAsImageSource(string exePath)
        {
            using System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            using MemoryStream iconStream = new();
            icon.ToBitmap().Save(iconStream, System.Drawing.Imaging.ImageFormat.Png);
            iconStream.Seek(0, SeekOrigin.Begin);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = iconStream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }

}