using Microsoft.UI.Xaml;

using Prism.Commands;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Input;

using Talaria.AddIn;

namespace Talaria;

internal partial class NewProjectItemViewmodel
{

    private class UnknownComponent : ComponentBase<UnknownComponent>
    {
        private UnknownComponent()
        {

        }
        public static UnknownComponent Instance { get; } = new UnknownComponent();
        public override Task<ComponentInstanceBase?> TryLoad(IDataReference reference) => Task.FromResult<ComponentInstanceBase?>(new UnkwonInstance(reference));

        private class UnkwonInstance : ComponentInstanceBase<UnknownComponent>
        {
            private IDataReference reference;

            public UnkwonInstance(IDataReference reference)
            {
                this.reference = reference;
            }

            public override IEditor CreateEditor() => new UnkwonEditor(this.reference);
        }
        private class UnkwonEditor : IEditor
        {
            private IDataReference reference;

            public UnkwonEditor(IDataReference reference)
            {
                this.reference = reference;
                this.Editor = new Microsoft.UI.Xaml.Controls.TextBlock()
                {
                    Text = this.reference is IFileDataReference file ?
                $"{file.LocalPath} is an Unkown Type"
                : "Unkown",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            public string Title => this.reference is IFileDataReference file
                ? file.Name
                : "Unkown";

            public FrameworkElement Editor { get; }

            public bool IsDirty => false;

            event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged { add { } remove { } }

            public Task SaveChanges() => Task.CompletedTask;
        }
    }

    [ImportMany(AllowRecomposition = true)]
    private readonly ObservableCollection<CreateItemBase> createableItems = new();

    [ImportMany(AllowRecomposition = true)]
    private readonly ObservableCollection<ComponentBase> components = new();

    public ReadOnlyObservableCollection<CreateItemBase> CreateableItems { get; }
    public ReadOnlyObservableCollection<ComponentBase> Components { get; }
    public ProjectViewmodel ProjectViewmodel { get; }

    public NewProjectItemViewmodel(ProjectViewmodel projectViewmodel)
    {
        ExtensionHub.Import(this);
        this.CreateableItems = new(this.createableItems);
        this.Components = new(this.components);
        this.ProjectViewmodel = projectViewmodel;
    }

    private class DataReference : IFileDataReference
    {
        private ProjectEntry entry;

        public DataReference(ProjectEntry entry)
        {
            this.entry = entry;
        }

        public string Name => this.entry.Name ?? "";

        public string LocalPath => this.entry.LocalPath;

        public Task<Stream> OpenData()
        {
            return Task.FromResult<Stream>(File.OpenRead(this.entry.AbsolutePath));
        }

        public async Task SaveData(Stream stream)
        {
            using var file = new FileStream(this.entry.AbsolutePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(file);
        }
    }

    public async Task<ComponentInstanceBase> Open(ProjectEntry entry)
    {

        var reference = new DataReference(entry);
        var posiibleComponents = this.Components.ToAsyncEnumerable().WhereAwait(async x => await x.CanLoad(reference));
        var component = await posiibleComponents.SingleOrDefaultAsync();
        return component is not null
            ? await component.Load(reference)
            : await UnknownComponent.Instance.Load(reference);
    }
}

internal partial class NewProjectItemElementViewmodel : DependencyObject
{


    //[SourceGenerators.AutoNotify]
    //private ICreateItem? createItem;




    public ProjectViewmodel Project
    {
        get { return (ProjectViewmodel) this.GetValue(ProjectProperty); }
        set { this.SetValue(ProjectProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Project.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ProjectProperty =
    DependencyProperty.Register("Project", typeof(ProjectViewmodel), typeof(NewProjectItemElementViewmodel), new PropertyMetadata(null));




    public CreateItemBase CreateItem
    {
        get { return (CreateItemBase) this.GetValue(CreateItemProperty); }
        set { this.SetValue(CreateItemProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CreateItem.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CreateItemProperty =
        DependencyProperty.Register("CreateItem", typeof(CreateItemBase), typeof(NewProjectItemElementViewmodel), new PropertyMetadata(null, CreateItemChanged));

    private static void CreateItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var me = (NewProjectItemElementViewmodel) d;
        ((DelegateCommand<ProjectEntry?>) me.ExecuteCommand).RaiseCanExecuteChanged();

    }

    public ICommand ExecuteCommand { get; }

    public NewProjectItemElementViewmodel()
    {
        this.ExecuteCommand = new DelegateCommand<ProjectEntry?>(this.Execute, (entry) => this.CreateItem is not null);
    }

    private async void Execute(ProjectEntry? entry)
    {
        if (this.Project is null) {
            return;
        }

        var context = this.Project.GetContext(entry);
        var dialog = new CreateItemDialog(this.CreateItem, context);
        // the dialog will execute the command
        _ = await dialog.ShowAsync();
    }
}
