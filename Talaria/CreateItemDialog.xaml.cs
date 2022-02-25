using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Talaria.AddIn;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Markup;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateItemDialog : ContentDialog
    {
        public bool IsValid
        {
            get { return (bool)this.GetValue(IsValidProperty); }
            set { this.SetValue(IsValidProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsValid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register("IsValid", typeof(bool), typeof(CreateItemDialog), new PropertyMetadata(true));







        private readonly ICreateItem createItem;

        public CreateItemOptions Options { get; }

        private readonly IEditorContext context;

        public CreateItemDialog(ICreateItem createItem, IEditorContext context)
        {
            this.createItem = createItem;
            this.Options = createItem.DefaultCreateItemOptions;

            this.Options.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(CreateItemOptions.IsValid)) {
                    this.UpdateIsValid();
                }
            };
            this.UpdateIsValid();

            this.context = context;
            this.InitializeComponent();
        }

        private void UpdateIsValid()
        {
            this.IsValid = this.Options.IsValid;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //// Ensure the user name and password fields aren't empty. If a required field
            //// is empty, set args.Cancel = true to keep the dialog open.
            //if (string.IsNullOrEmpty(userNameTextBox.Text)) {
            //    args.Cancel = true;
            //    errorTextBlock.Text = "User name is required.";
            //} else if (string.IsNullOrEmpty(passwordTextBox.Password)) {
            //    args.Cancel = true;
            //    errorTextBlock.Text = "Password is required.";
            //}

            // If you're performing async operations in the button click handler,
            // get a deferral before you await the operation. Then, complete the
            // deferral when the async operation is complete.

            ContentDialogButtonClickDeferral deferral = args.GetDeferral();
            await this.createItem.Execute(this.context, this.Options);
            deferral.Complete();
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }


    public sealed class TemplateSelector : DataTemplateSelector
    {


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => this.SelectTemplateCore(item);
        public DataTemplate? TextTemplate { get; set; }
        public DataTemplate? FileTemplate { get; set; }
        public DataTemplate? RangeTemplate { get; set; }
        protected override DataTemplate SelectTemplateCore(object item)
        {


            //new Grid().RowDefinitions
            if (item is TextOption) {
                return this.TextTemplate ?? base.SelectTemplateCore(item);
            }

            if (item is FileOption) {
                return this.FileTemplate ?? base.SelectTemplateCore(item);
            }

            if (item is RangeOption) {
                return this.RangeTemplate ?? base.SelectTemplateCore(item);
            }

            return base.SelectTemplateCore(item);


        }

    }
}
