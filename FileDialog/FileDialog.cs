using FileDialog.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDialog
{
    public class FileDialog : Control
    {
        private TreeView _folderTreeView;
        private ListView _filesListView;

        private string _startingPath;
        private Stack<string> _previousPaths = new Stack<string>();
        private Stack<string> _nextPaths = new Stack<string>();
        private bool _updatePreviousStack = false;
        private bool _updateFolderLocation = true;

        // ROUTED COMMANDS
        private static RoutedCommand _forwardCommand = new RoutedCommand();
        public static RoutedCommand ForwardCommand
        {
            get { return _forwardCommand; }
        }

        private static RoutedCommand _backCommand = new RoutedCommand();
        public static RoutedCommand BackCommand
        {
            get { return _backCommand; }
        }

        private static RoutedCommand _upCommand = new RoutedCommand();
        public static RoutedCommand UpCommand
        {
            get { return _upCommand; }
        }

        // ROUTED EVENTS
        public static readonly RoutedEvent FileDoubleClickedEvent =
            EventManager.RegisterRoutedEvent("FileDoubleClicked",
                                             RoutingStrategy.Bubble,
                                             typeof(FileDoubleClickedRoutedEventHandler),
                                             typeof(FileDialog));

        public event FileDoubleClickedRoutedEventHandler FileDoubleClicked
        {
            add { AddHandler(FileDoubleClickedEvent, value); }
            remove { RemoveHandler(FileDoubleClickedEvent, value); }
        }


        // DEPENDENCY PROPERTIES
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path",
                                        typeof(string),
                                        typeof(FileDialog),
                                        new FrameworkPropertyMetadata(null, 
                                                                      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                                                                      new PropertyChangedCallback(OnPathChanged)));

        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }

        public static readonly DependencyProperty BindableSelectedItemProperty =
            DependencyProperty.Register("BindableSelectedItem",
                                        typeof(Folder),
                                        typeof(FileDialog),
                                        new FrameworkPropertyMetadata(null));

        public Folder BindableSelectedItem
        {
            get { return (Folder)GetValue(BindableSelectedItemProperty); }
            set { SetValue(BindableSelectedItemProperty, value); }
        }

        public static readonly DependencyProperty ShowFilesProperty =
            DependencyProperty.Register("ShowFiles",
                                        typeof(bool),
                                        typeof(FileDialog),
                                        new FrameworkPropertyMetadata(true));

        public bool ShowFiles
        {
            get { return (bool)GetValue(ShowFilesProperty); }
            set { SetValue(ShowFilesProperty, value); }
        }

        public static readonly DependencyProperty FolderWidthProperty =
            DependencyProperty.Register("FolderWidth",
                                        typeof(double),
                                        typeof(FileDialog),
                                        new FrameworkPropertyMetadata(200.0));
        
        public double FolderWidth
        {
            get { return (double)GetValue(FolderWidthProperty); }
            set { SetValue(FolderWidthProperty, value); }
        }
        
        private static readonly DependencyPropertyKey SelectedFolderFilesPropertyKey =
            DependencyProperty.RegisterReadOnly("SelectedFolderFiles",
                                                typeof(List<DirectoryItem>),
                                                typeof(FileDialog),
                                                new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedFolderFilesProperty = SelectedFolderFilesPropertyKey.DependencyProperty;
        public List<DirectoryItem> SelectedFolderFiles
        {
            get { return (List<DirectoryItem>)GetValue(SelectedFolderFilesProperty); }
        }

        public FileDialog()
        {
            CommandBindings.Add(new CommandBinding(ForwardCommand, ExecuteForwardCommand, CanExecuteForwardCommand));
            CommandBindings.Add(new CommandBinding(BackCommand, ExecuteBackCommand, CanExecuteBackCommand));
            CommandBindings.Add(new CommandBinding(UpCommand, ExecuteUpCommand, CanExecuteUpCommand));

            Loaded += (sender, e) =>
            {
                if (_startingPath != null)
                    SelectFolder(_folderTreeView, _startingPath);
            };
        }

        static FileDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FileDialog), new FrameworkPropertyMetadata(typeof(FileDialog)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _filesListView = GetTemplateChild("PART_Files") as ListView;
            _filesListView.MouseDoubleClick += FilesListView_MouseDoubleClick;

            _folderTreeView = GetTemplateChild("PART_FolderTreeView") as TreeView;

            _folderTreeView.SelectedItemChanged += FolderTreeView_SelectedItemChanged;
            _folderTreeView.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FolderTreeView_Expanded));

            _folderTreeView.Items.Add(new Folder(System.IO.Path.Combine
                    (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Links"), null));
            foreach (var drive in Directory.GetLogicalDrives())
                _folderTreeView.Items.Add(new Folder(drive, null));

            this.MouseUp += FileDialog_MouseUp;
        }

        private void FileDialog_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 && _previousPaths.Count > 0)
                ExecuteBackCommand(null, null);

            else if (e.ChangedButton == MouseButton.XButton2 && _nextPaths.Count > 0)
                ExecuteForwardCommand(null, null);
        }

        private static void OnPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fileDialog = d as FileDialog;
            string newVal = (string)e.NewValue;

            if (fileDialog._folderTreeView == null)
                fileDialog._startingPath = newVal;
            else
            {
                if (fileDialog._updateFolderLocation)
                    fileDialog.SelectFolder(fileDialog._folderTreeView, newVal);
            }
        }

        private void CanExecuteForwardCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _nextPaths.Count > 0;
        }

        private void ExecuteForwardCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // First put the current path in the previous stack
            _previousPaths.Push(Path);

            // Now jump to the next path on the stack
            _updatePreviousStack = false;
            SelectFolder(_folderTreeView, _nextPaths.Pop());
        }

        private void CanExecuteBackCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _previousPaths.Count > 0;
        }

        private void ExecuteBackCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // First put the current path in the previous stack
            _nextPaths.Push(Path);

            // Now jump to the next path on the stack
            _updatePreviousStack = false;
            SelectFolder(_folderTreeView, _previousPaths.Pop());
        }

        private void CanExecuteUpCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = BindableSelectedItem?.IsDrive == false;
        }

        private void ExecuteUpCommand(object sender, ExecutedRoutedEventArgs e)
        {
            SelectFolder(_folderTreeView, Directory.GetParent(Path).FullName);
        }

        private void FolderTreeView_Expanded(object sender, RoutedEventArgs e)
        {
            var folder = (e.OriginalSource as TreeViewItem).Header as Folder;
            folder.ReadSubItemsForFolder();
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var folder = e.NewValue as Folder;

            if (_updatePreviousStack && folder.Path != Path)
                _previousPaths.Push(Path);
            else
                _updatePreviousStack = true;

            // The user selected the folder location, don't force it to update again
            _updateFolderLocation = false;
            SetValue(PathProperty, folder.Path);
            SetValue(BindableSelectedItemProperty, folder);
            SetValue(SelectedFolderFilesPropertyKey, GetFolderFiles(folder.Path));
            _updateFolderLocation = true;
        }

        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListView).SelectedItem as DirectoryItem;

            if (item.IsFolder)
            {
                SelectFolder(_folderTreeView, item.Path);
            }
            else
            {
                // Raise event to subscribers that a file has been double clicked/selected
                var args = new FileDoubleClickedRoutedEventArgs(FileDoubleClickedEvent, item.Path);
                RaiseEvent(args);
            }
        }

        public List<DirectoryItem> GetFolderFiles(string path)
        {
            var allItems = new List<DirectoryItem>();

            foreach (var info in new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                if (info.Extension == ".lnk")
                    allItems.Add(new DirectoryItem(LinkConverter.GetLnkTarget(info.FullName), isFolder: true));
                else if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    allItems.Add(new DirectoryItem(info.FullName, isFolder: true));
                else if (ShowFiles)
                    allItems.Add(new DirectoryItem(info.FullName, isFolder: false));
            }

            return allItems.OrderBy(x => !x.IsFolder).ToList();
        }

        /// <summary>
        /// Selects the correct path from the treeview
        /// </summary>
        /// <param name="targetFolder">Desired path</param>
        private void SelectFolder(TreeView treeView, string targetFolder)
        {
            // Don't try and find the folder if it's empty
            if (string.IsNullOrEmpty(targetFolder))
                return;

            // Loop through all drives
            foreach (Folder folder in treeView.Items)
            {
                if (targetFolder.StartsWith(folder.Path))
                {
                    TreeViewItem item = treeView.ItemContainerGenerator.ContainerFromItem(folder) as TreeViewItem;
                    RecursivelySelectFolder(treeView, targetFolder + "\\", item);
                    return;
                }
                else if (folder.Name == "Favorites")
                {
                    foreach (var subFolder in folder.SubItems.Where(f => f != null))
                    {
                        if (targetFolder.StartsWith(subFolder.Path))
                        {
                            TreeViewItem item = treeView.ItemContainerGenerator.ContainerFromItem(folder) as TreeViewItem;
                            TreeViewItem subItem = item.ItemContainerGenerator.ContainerFromItem(subFolder) as TreeViewItem;
                            RecursivelySelectFolder(treeView, targetFolder + "\\", subItem);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively loops through the treeview structure to get the correct
        /// folder with the right path
        /// </summary>
        /// <param name="targetPath">Desired path</param>
        /// <param name="tvItem">Current treeview item</param>
        private static void RecursivelySelectFolder(TreeView treeView, string targetPath, TreeViewItem tvItem)
        {
            Folder thisFolder = tvItem.DataContext as Folder;

            // We found this item - select it
            if (targetPath.Equals(thisFolder.Path, StringComparison.CurrentCultureIgnoreCase) ||
                targetPath.Equals(thisFolder.Path + "\\", StringComparison.CurrentCultureIgnoreCase))
            {
                tvItem.IsSelected = true;
                tvItem.BringIntoView();
            }
            // We're on the right path, keep moving down a level
            else if (targetPath.StartsWith(thisFolder.Path + (thisFolder.IsDrive ? "" : "\\"), StringComparison.CurrentCultureIgnoreCase))
            {
                thisFolder.ReadSubItemsForFolder();
                tvItem.IsExpanded = true;
                treeView.UpdateLayout();

                foreach (Folder subFolder in tvItem.Items)
                {
                    TreeViewItem subItem = tvItem.ItemContainerGenerator.ContainerFromItem(subFolder) as TreeViewItem;
                    RecursivelySelectFolder(treeView, targetPath, subItem);
                }
            }
            else // This isn't it, break the tree
            {
                tvItem.IsExpanded = false;
                treeView.UpdateLayout();
                return;
            }
        }
    }
}
