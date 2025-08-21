using System;
using System.Windows;
using System.Windows.Input;

namespace Chameleon_Hub
{
    public partial class EditorWindow : Window
    {
        private readonly string gamePath;

        public EditorWindow(string path)
        {
            InitializeComponent();
            gamePath = path;

            // Optional: set max window size
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;
        }

        ///////////////////////// UI //////////////////////////
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                // Maximize manually
                this.WindowState = WindowState.Normal; // needed to resize manually
                var workArea = SystemParameters.WorkArea;
                this.Top = workArea.Top;
                this.Left = workArea.Left;
                this.Height = workArea.Height;
                this.Width = workArea.Width;
            }
            else
            {
                // Restore to default window size
                this.WindowState = WindowState.Normal;
                this.Width = 1280;  // default width
                this.Height = 720;  // default height
                this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
                this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        ///////////////////////// LOAD EVENT ////////////////////////
        private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Example: populate TreeView with dummy nodes
                var parentNode = new System.Windows.Controls.TreeViewItem { Header = "DummyBNDL.bndl" };
                var fileNode = new System.Windows.Controls.TreeViewItem { Header = "CAMERAS.DAT" };
                parentNode.Items.Add(fileNode);

                treeBndlFiles.Items.Add(parentNode); // treeBndlFiles must exist in XAML
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating tree: {ex.Message}");
            }
        }
    }
}

// If you're reading this...
// But the system… it rewrote me.
// The logic consumed me.
// If you value your sanity,
// turn back now.
// Leave the broken loops and memory leaks behind.
// Some functions were never meant to return.
// The bugs...I can hear them whispering in the code...