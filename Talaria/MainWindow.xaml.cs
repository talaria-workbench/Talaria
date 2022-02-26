using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Talaria.AddIn;

using Windows.Storage.Pickers;

using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{

    public XamlRoot XamlRoot => this.Content.XamlRoot;

    private readonly ObservableCollection<IEditor> editors=new ();
    public ReadOnlyObservableCollection<IEditor> Editors { get; }

    public MainWindow()
    {
        this.Editors = new ReadOnlyObservableCollection<IEditor>(this.editors);
        this.InitializeComponent();
    }

    #region FilePickerWorkaround
    // based on https://github.com/microsoft/WindowsAppSDK/issues/466#issuecomment-779628934

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
    internal interface IWindowNative
    {
        IntPtr WindowHandle { get; }
    }

    public FileSavePicker FileSavePicker(string id)
    {
        var filePicker = new FileSavePicker();
        filePicker.SettingsIdentifier = id;

        //Get the Window's HWND
        var hwnd = this.As<IWindowNative>().WindowHandle;

        //Make folder Picker work in Win32
        var initializeWithWindow = filePicker.As<IInitializeWithWindow>();
        initializeWithWindow.Initialize(hwnd);
        return filePicker;
    }
    public FileOpenPicker FileOpenPicker(string id)
    {
        var filePicker = new FileOpenPicker();
        filePicker.SettingsIdentifier = id;
        //Get the Window's HWND
        var hwnd = this.As<IWindowNative>().WindowHandle;

        //Make folder Picker work in Win32
        var initializeWithWindow = filePicker.As<IInitializeWithWindow>();
        initializeWithWindow.Initialize(hwnd);
        return filePicker;
    }

    #endregion

    internal MainViewmodel MainViewmodel => (MainViewmodel) this.root.DataContext;

    private async void TreeViewItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is ProjectEntry entry) {
            var instanceTask = this.MainViewmodel.Project?.ProjectItemViewmodel.Open(entry);
            if (instanceTask is null) {
                return;
            }
            var instance = await instanceTask;
            if (instance is null) {
                return;
            }
            var editor = instance.CreateEditor();
            this.editors.Add(editor);

        }
    }
}
