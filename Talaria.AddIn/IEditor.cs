using Microsoft.UI.Xaml;

using System.ComponentModel;

namespace Talaria.AddIn;

public interface IEditor : INotifyPropertyChanged
{
    string Title { get; }
    FrameworkElement Editor { get; }
    Task SaveChanges();
    bool IsDirty { get; }

}
