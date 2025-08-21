using Chameleon_Hub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chameleon_Hub
{
    public partial class LoadingWindow : Window
    {
        private readonly GameEntry gameEntry;

        public LoadingWindow(GameEntry entry)
        {
            InitializeComponent();
            gameEntry = entry;

            // You can start loading here or later
            this.Loaded += async (s, e) =>
            {
                await LoadGameFilesAsync(Path.GetDirectoryName(gameEntry.ExePath));
            };
        }
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
