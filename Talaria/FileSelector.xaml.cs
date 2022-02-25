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

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria
{
    public sealed partial class FileSelector : UserControl
    {


        public FileInfo? File
        {
            get { return (FileInfo)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for File.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register("File", typeof(FileInfo), typeof(FileSelector), new PropertyMetadata(null));




        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            private set { SetValue(PathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(FileSelector), new PropertyMetadata(string.Empty, PathChanged));

        private static void PathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var me = (FileSelector)d;
            try {
                string path = (string)e.NewValue;
                if (string.IsNullOrWhiteSpace(path)) {
                    me.File = null;
                } else {
                    var info = new FileInfo(path);
                    me.File = info;
                }

            } catch (Exception) {
                me.File = null;
            }


        }

        public IEnumerable<string> FileTypes { get; set; } = Array.Empty<string>();


        public FileSelector()
        {
            this.InitializeComponent();

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = App.Current.Window.FileOpenPicker();
            foreach (var f in this.FileTypes) {
                dialog.FileTypeFilter.Add(f);
            }

            var result = await dialog.PickSingleFileAsync();
            if (result is not null) {
                this.Path = result.Path;
            }
        }
    }

}
