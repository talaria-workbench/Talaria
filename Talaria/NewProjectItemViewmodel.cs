using Microsoft.UI.Xaml;

using Prism.Commands;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows.Input;

using Talaria.AddIn;

namespace Talaria;

internal partial class NewProjectItemViewmodel
{

    [ImportMany(AllowRecomposition = true)]
    private readonly ObservableCollection<CreateItemBase> createableItems = new();



    public ReadOnlyObservableCollection<CreateItemBase> CreateableItems { get; }
    public ProjectViewmodel ProjectViewmodel { get; }

    public NewProjectItemViewmodel(ProjectViewmodel projectViewmodel)
    {
        ExtensionHub.Import(this);
        this.CreateableItems = new ReadOnlyObservableCollection<CreateItemBase>(this.createableItems);
        this.ProjectViewmodel = projectViewmodel;
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
