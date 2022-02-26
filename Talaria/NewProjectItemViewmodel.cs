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
        public override async Task<ComponentInstanceBase<UnknownComponent>> Load(IDataReference stream) => new UnkwonInstance();


        private class UnkwonInstance : ComponentInstanceBase<UnknownComponent>
        {
            public override IEditor CreateEditor() => new UnkwonEditor();
        }
        private class UnkwonEditor : IEditor
        {
            public string Title => "Unknown";

            public FrameworkElement Editor { get; } = new Microsoft.UI.Xaml.Controls.TextBlock() { Text = "Unkown", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            public bool IsDirty => false;

            public event PropertyChangedEventHandler? PropertyChanged;

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

    private class DataReference : IDataReference
    {
        private ProjectEntry entry;

        public DataReference(ProjectEntry entry)
        {
            this.entry = entry;
        }

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
        var posiibleComponents =this.Components.Where(x => x.FileEndings.Contains(Path.GetExtension(entry.Name) ?? string.Empty));
        var component = posiibleComponents.SingleOrDefault();
        var reference= new DataReference(entry);
        if (component is null) {
            return await UnknownComponent.Instance.Load(reference);
        }
        return await component.Load(reference);
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
        var me = (NewProjectItemElementViewmodel)d;
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
