using FileDialog.Model;
using System.Windows;

namespace FileDialogSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileDialog_FileDoubleClicked(object sender, FileDoubleClickedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Path);
        }
    }
}
