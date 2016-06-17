using System.Windows;

namespace FileDialog.Model
{
    public delegate void FileDoubleClickedRoutedEventHandler(object sender, FileDoubleClickedRoutedEventArgs e);
    public class FileDoubleClickedRoutedEventArgs : RoutedEventArgs
    {
        public string Path { get; set; }

        public FileDoubleClickedRoutedEventArgs(RoutedEvent routedEvent, string path)
            : base(routedEvent)
        {
            Path = path;
        }
    }
}
