namespace FileDialog.Model
{
    public class DirectoryItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }

        public DirectoryItem(string path, bool isFolder)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
            IsFolder = isFolder;
        }
    }
}
