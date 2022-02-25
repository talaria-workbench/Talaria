using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Prism.Commands;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Talaria.AddIn;

namespace Talaria
{
    internal partial class NewProjectItemViewmodel
    {

        [ImportMany(AllowRecomposition = true)]
        private readonly ObservableCollection<ICreateItem> createableItems = new();

        [SourceGenerators.AutoNotify]
        private ProjectViewmodel? project;



        public ReadOnlyObservableCollection<ICreateItem> CreateableItems { get; }

        public NewProjectItemViewmodel()
        {
            ExtensionHub.Import(this);
            this.CreateableItems = new ReadOnlyObservableCollection<ICreateItem>(this.createableItems);
        }
    }

    internal partial class NewProjectItemElementViewmodel : DependencyObject
    {

        [SourceGenerators.AutoNotify]
        private ProjectViewmodel? project;

        //[SourceGenerators.AutoNotify]
        //private ICreateItem? createItem;




        public ICreateItem CreateItem
        {
            get { return (ICreateItem)this.GetValue(CreateItemProperty); }
            set { this.SetValue(CreateItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateItemProperty =
            DependencyProperty.Register("CreateItem", typeof(ICreateItem), typeof(NewProjectItemElementViewmodel), new PropertyMetadata(null, CreateItemChanged));

        private static void CreateItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (NewProjectItemElementViewmodel)d;
            ((DelegateCommand)me.ExecuteCommand).RaiseCanExecuteChanged();

        }

        public ICommand ExecuteCommand { get; }

        public NewProjectItemElementViewmodel()
        {
            this.ExecuteCommand = new DelegateCommand(this.Execute, () => this.CreateItem is not null);
        }

        private async void Execute()
        {
            //var dialog = new ContentDialog();
            var dialog = new CreateItemDialog(this.CreateItem,null);
            dialog.XamlRoot = App.Current.Window.XamlRoot;
            await dialog.ShowAsync();
            //this.CreateItem.Execute()
        }
    }
}
