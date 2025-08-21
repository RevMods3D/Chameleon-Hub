using Chameleon_Hub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace Chameleon_Hub
{
    public partial class LoadingWindow : Window
    {
        private bool toggle = true;
        private int currentIndex = 0;
        private readonly DispatcherTimer timer;
        private readonly GameEntry gameEntry;
        private readonly string[] backgrounds = Enumerable.Range(0, 5)
    .Select(i => $"pack://application:,,,/ChameleonHub;component/Resources/LoadingScreen{i}.png")
    .ToArray();

        public LoadingWindow(GameEntry entry)
        {
            InitializeComponent();
            gameEntry = entry;

            // You can start loading here or later
            this.Loaded += async (s, e) =>
            {
                await LoadGameFilesAsync(Path.GetDirectoryName(gameEntry.ExePath));
            };

            // Show the first background
            BackgroundImage1.Source = new BitmapImage(new Uri(backgrounds[currentIndex], UriKind.Absolute));
            BackgroundImage2.Opacity = 0; // ensure the second image is hidden initially

            // Rotate every x seconds
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (s, e) => ChangeBackground();
            timer.Start();
        }

        ///////////////////// Visuals ///////////////////////
        private void ChangeBackground()
        {
            // Next image index
            currentIndex = (currentIndex + 1) % backgrounds.Length;

            Image fadeOutImg, fadeInImg;

            if (toggle)
            {
                fadeInImg = BackgroundImage2;
                fadeOutImg = BackgroundImage1;
            }
            else
            {
                fadeInImg = BackgroundImage1;
                fadeOutImg = BackgroundImage2;
            }

            // Load next image on top
            fadeInImg.Source = new BitmapImage(new Uri(backgrounds[currentIndex], UriKind.Absolute));
            fadeInImg.Opacity = 0;

            // Fade in the new image
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(2));
            fadeIn.Completed += (s, e) =>
            {
                // Fade out the old image after new is fully visible
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
                fadeOutImg.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            };

            fadeInImg.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            toggle = !toggle;
        }

        /////////////////////////////////////////////////////

        public void UpdateProgress(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                if (percent < progressBar.Minimum)
                    percent = (int)progressBar.Minimum;
                else if (percent > progressBar.Maximum)
                    percent = (int)progressBar.Maximum;

                progressBar.Value = percent;
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                labelStatus.Text = status;
            });
        }

        public async Task<List<string>> LoadGameFilesAsync(string folder)
        {
            List<string> files = new();

            var allFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".bndl", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int total = allFiles.Count;

            for (int i = 0; i < total; i++)
            {
                string file = allFiles[i];
                files.Add(file);

                int percent = (i + 1) * 100 / total;

                UpdateProgress(percent);
                UpdateStatus($"Loading: {Path.GetFileName(file)}");

                await Task.Delay(10); // Optional delay for UX
            }

            await Task.Delay(200); // Give a brief pause after finishing

            // ✅ Open EditorWindow after loading completes
            Dispatcher.Invoke(() =>
            {
                var editorWindow = new EditorWindow(Path.GetDirectoryName(gameEntry.ExePath));
                editorWindow.Show();
                this.Close();
            });

            return files;
        }
    }
}
