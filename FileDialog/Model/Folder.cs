using System;
using System.Collections.Generic;
using System.IO;

namespace FileDialog.Model
{
    /// <summary>
    /// Folder View Model Data - stores the folder hierarchical structure
    /// </summary>
    /// <remarks></remarks>
    public class Folder
    {
        private static readonly Folder dummyFolder = null;

        public string Path { get; set; }
        public string Name { get; set; }
        public Folder Parent { get; set; }
        public List<Folder> SubItems { get; set; }
        public bool IsDrive { get; set; }
        public bool IsInaccessible { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <param name="parent">Reference to the parent</param>
        public Folder(string path, Folder parent)
        {
            Path = path;
            Parent = parent;
            SubItems = new List<Folder>();
            IsDrive = parent == null & path.Length == 3;
            Name = IsDrive ? path.ToUpper() : System.IO.Path.GetFileName(path);

            if (Name == "Links")
                Name = "Favorites";

            CheckIfFolderHasSubFolder();
        }

        /// <summary>
        /// Checks if sub-directories exist below it. If they do, it'll add a dummy item to show the expander
        /// </summary>
        private void CheckIfFolderHasSubFolder()
        {
            try
            {
                // Check if the directory contains subItems - if it does, add a dummy
                if (Directory.GetDirectories(Path, "*", SearchOption.TopDirectoryOnly).Length > 0 ||
                    Path == System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Links"))
                    SubItems.Add(dummyFolder);
            }
            catch (UnauthorizedAccessException)
            { IsInaccessible = true; }
            catch (IOException)
            { IsInaccessible = true; }
        }

        /// <summary>
        /// Reads all the sub-directories below the current folder
        /// </summary>
        public void ReadSubItemsForFolder()
        {
            if (SubItems.Count == 1 && SubItems[0] == null)
            {
                // Clear the dummy item out
                SubItems.Clear();

                try
                {
                    // Read the directories below
                    foreach (string dir in Directory.GetDirectories(Path, "*", SearchOption.TopDirectoryOnly))
                        SubItems.Add(new Folder(dir, this));

                    // Get any links in the directory
                    foreach (string file in Directory.GetFiles(Path, "*.lnk", SearchOption.TopDirectoryOnly))
                        SubItems.Add(new Folder(LinkConverter.GetLnkTarget(file), this));

                    // Remove any inaccessible folders
                    for (int i = SubItems.Count - 1; i >= 0; i--)
                        if (SubItems[i].IsInaccessible)
                            SubItems.RemoveAt(i);
                }
                catch (UnauthorizedAccessException)
                {
                    // This means we don't have access, don't worry about it
                }
            }
        }
    }
}
