using Microsoft.UI.Xaml;

using Prism.Commands;

using System.Collections.ObjectModel;
using System.Windows.Input;

using Talaria.AddIn;

namespace Talaria;

internal partial class MainViewmodel
{
    [SourceGenerators.AutoNotify]
    private ProjectViewmodel? project;

    public ICommand OpenCommand { get; }
    public ICommand CreateNewCommand { get; }

    public MainViewmodel()
    {
        this.OpenCommand = new DelegateCommand(async () =>
        {
            var dialog = App.Current.Window.FileOpenPicker("Project");
            dialog.FileTypeFilter.Add(".talaria");
            var result = await dialog.PickSingleFileAsync("open_Project");
            if (result is not null) {
                this.Project = await ProjectViewmodel.Open(new FileInfo(result.Path));
            }
        });
        this.CreateNewCommand = new DelegateCommand(async () =>
        {
            var dialog = App.Current.Window.FileSavePicker("Project");
            dialog.FileTypeChoices.Add(".talaria", new[] { ".talaria" });
            dialog.DefaultFileExtension = ".talaria";
            var result = await dialog.PickSaveFileAsync();
            if (result is not null) {
                this.Project = await ProjectViewmodel.Create(new FileInfo(result.Path));
            }
        });
    }
}

internal partial class ProjectEntry
{
    public ProjectEntry? Parent { get; }
    public bool IsFolder { get; }

    [SourceGenerators.AutoNotify]
    private string? name;


    public ProjectEntry(ProjectEntry? parent, string name, bool isFolder)
    {
        this.Parent = parent;
        this.Name = name;
        this.IsFolder = isFolder;
        this.LocalPath = Path.Combine(parent?.LocalPath ?? "/", name);
    }

    public ObservableCollection<ProjectEntry> Childrean { get; } = new ObservableCollection<ProjectEntry>();

    public string LocalPath { get; }
}

internal partial class ProjectViewmodel : DependencyObject
{

    public ProjectEntry Root { get; }
    public TreeViewmodel Tree { get; }

    public FileInfo ProjectFile { get; }
    public DirectoryInfo ProjectFolder { get; }
    public NewProjectItemViewmodel ProjectItemViewmodel { get; }




    private ProjectViewmodel(FileInfo projectFile)
    {
        if (!projectFile.Exists) {
            throw new ArgumentException("Project does not exists");
        }

        if (projectFile.Directory is null) {
            throw new ArgumentException("Projectfile can't be in root");
        }
        this.ProjectFile = projectFile;
        this.ProjectFolder = projectFile.Directory;
        this.ProjectItemViewmodel = new NewProjectItemViewmodel(this);

        this.Root = new ProjectEntry(null, "", true);

        this.Tree = new TreeViewmodel(this);

    }

    internal IEditorContext GetContext(ProjectEntry? entry)
    {
        return new Context(this, entry);

    }

    private class Context : IEditorContext
    {
        private ProjectViewmodel projectViewmodel;
        private ProjectEntry? entry;

        public Context(ProjectViewmodel projectViewmodel, ProjectEntry? entry)
        {
            this.projectViewmodel = projectViewmodel;
            this.entry = entry;
        }

        public IEditorContext Root => this.projectViewmodel.GetContext(this.projectViewmodel.Root);

        public Task CreateFile<T>(string path, T item) => throw new NotImplementedException();
        public Task<Stream> CreateFileStream(string path)
        {
            CheckFileName(path);
            var fullPath = this.GetAbsoulutePathForFile(path);
            return Task.FromResult<Stream>(File.Open(fullPath, FileMode.Create));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private string GetAbsoulutePathForFile(string path)
        {
            var parent = this.entry?.LocalPath ?? "/";
            if (this.entry?.IsFolder is false) {
                parent = NormalizePath(Path.GetDirectoryName(parent) ?? "/");
            }

            return NormalizePath(Path.Combine(this.projectViewmodel.ProjectFolder.FullName, parent.TrimStart('/'), path));
        }

        public Task CreateFolder(string path)
        {
            CheckFileName(path);
            var fullPath = this.GetAbsoulutePathForFile(path);
            _ = Directory.CreateDirectory(fullPath);
            return Task.CompletedTask;
        }

        public bool ExistsFileOrDirectory(string path)
        {
            CheckFileName(path);
            var fullPath = this.GetAbsoulutePathForFile(path);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        private static void CheckFileName(string path)
        {
            if (path.IndexOfAny(Path.GetInvalidFileNameChars()) is int index && index is not -1) {
                throw new ArgumentException($"Path {path} contains invalid characters `{path[index]}`");
            }
        }

        public Task<T> OpenFile<T>(string path) => throw new NotImplementedException();
        public Task<Stream> OpenFileStream(string path) => throw new NotImplementedException();
    }

    internal class TreeViewmodel : DependencyObject
    {
        private readonly FileSystemWatcher watcher;
        private readonly ProjectViewmodel projectViewmodel;

        private readonly Dictionary<string, ProjectEntry> projectEntries = new Dictionary<string, ProjectEntry>();

        public ProjectEntry? SelectedEntry
        {
            get { return (ProjectEntry?) this.GetValue(SelectedEntryProperty); }
            set { this.SetValue(SelectedEntryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for projectEntry.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntryProperty =
    DependencyProperty.Register("SelectedEntry", typeof(ProjectEntry), typeof(TreeViewmodel), new PropertyMetadata(null));


        public TreeViewmodel(ProjectViewmodel projectViewmodel)
        {
            this.projectViewmodel = projectViewmodel;

            this.projectEntries.Add("/", projectViewmodel.Root);

            this.watcher = new FileSystemWatcher(this.projectViewmodel.ProjectFolder.FullName)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            this.watcher.Created += this.Watcher_Changed;
            this.watcher.Changed += this.Watcher_Changed;
            this.watcher.Deleted += this.Watcher_Changed;
            this.watcher.Error += this.Watcher_Error;
            this.RescanEntrys();
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            this.RescanEntrys();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!this.DispatcherQueue.HasThreadAccess) {
                _ = this.DispatcherQueue.TryEnqueue(() => this.Watcher_Changed(sender, e));
                return;
            }

            switch (e.ChangeType) {
                case WatcherChangeTypes.Created:
                    this.AddEntry(e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    this.RemoveEntry(e.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    this.UpdateEntry(e.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                    if (e is RenamedEventArgs renamed) {
                        this.RenameEntry(renamed.FullPath, renamed.OldFullPath);
                    } else {
                        this.RescanEntrys();
                    }
                    break;
                case WatcherChangeTypes.All:
                default:
                    this.RescanEntrys();
                    break;
            }
        }

        private void UpdateEntry(string fullPath)
        {

            //var localPath = Path.GetRelativePath(this.projectViewmodel.ProjectFolder.FullName, fullPath);
            //if (localPath is null) {
            //    throw new ArgumentException("Path is not under root");
            //}
            //var parent = Path.GetDirectoryName(localPath) ?? "/";

        }

        private void RescanEntrys()
        {
            this.projectViewmodel.Root.Childrean.Clear();
            Scan(this.projectViewmodel.ProjectFolder);
            void Scan(DirectoryInfo directory)
            {
                foreach (var file in directory.GetFiles()) {
                    this.AddEntry(file.FullName);
                }
                foreach (var d in directory.GetDirectories()) {
                    Scan(d);
                }
            }

        }

        private void RemoveEntry(string fullPath)
        {
            var localPath = Path.GetRelativePath(this.projectViewmodel.ProjectFolder.FullName, fullPath);
            if (localPath is null) {
                throw new ArgumentException("Path is not under root");
            }
            var current = this.projectEntries[localPath];
            _ = this.projectEntries.Remove(localPath);
            if (current.Parent is not null) {
                _ = current.Parent.Childrean.Remove(current);
            }
        }
        private void AddEntry(string fullPath)
        {
            var localPath = Path.GetRelativePath(this.projectViewmodel.ProjectFolder.FullName, fullPath);
            if (localPath is null) {
                throw new ArgumentException("Path is not under root");
            }
            var parentPath = Path.GetDirectoryName(localPath) ;
            if (string.IsNullOrWhiteSpace(parentPath)) {
                parentPath = "/";
            }
            var parent = this.GetFromLocalPath(parentPath);
            var isFolder = Directory.Exists(fullPath);
            var current = new ProjectEntry(parent,Path.GetFileName(localPath), isFolder);
            if (parent is not null) {
                parent.Childrean.Add(current);
            }
            this.projectEntries[localPath] = current;
        }

        private void RenameEntry(string fullPath, string oldFullPath)
        {
            this.RemoveEntry(oldFullPath);
            this.AddEntry(fullPath);
        }

        public ProjectEntry? GetFromLocalPath(string path)
        {
            return this.projectEntries.TryGetValue(path, out var entry) ? entry : null;
        }
    }

    public static Task<ProjectViewmodel> Open(FileInfo projectFile)
    {
        return Task.FromResult(new ProjectViewmodel(projectFile));
    }
    public static async Task<ProjectViewmodel> Create(FileInfo projectFile)
    {
        if (projectFile.Exists && projectFile.Length > 0) {
            throw new ArgumentException("Project already Exists");
        }
        using (var writer = projectFile.AppendText()) {
            await writer.WriteLineAsync("<project />");
        }
        return new ProjectViewmodel(projectFile);
    }
}
