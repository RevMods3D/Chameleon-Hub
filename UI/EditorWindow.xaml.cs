using System.IO;
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
            Loaded += EditorWindow_Loaded;

            // Prevent window from overlapping taskbar when maximized
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;
        }

        //////////////////////////UI///////////////////////////////////
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                // Remove WindowState so we can fully control the bounds
                this.WindowState = WindowState.Normal;

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
                this.Width = 1680; // Or whatever default width
                this.Height = 1050; // Or whatever default height
                this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
                this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        ////////////////////////////////////////////////////////////////

        private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                MessageBox.Show($"Game folder not found: {gamePath}");
                return;
            }

            treeBndlFiles.Items.Clear();
        }
    }
}





// If you're reading this...
// I may have gone insane.
// The logic consumed me.
// The bugs… they stare back.

// I tried to solve it all.
// I chased the perfect code.
// I thought I could control the system.

// But the system… it rewrote me.

// If you value your sanity,
// turn back now.
// Leave the broken loops and memory leaks behind.
// Some functions were never meant to return.
// The bugs...I can hear them whispering in the code...