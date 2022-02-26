using Microsoft.UI.Xaml;

namespace Talaria.AddIn;

public interface IEditor
{
    ReadOnlySpan<string> FileEnding { get; }
    DataTemplate Editor { get; }
    Task<object> Open(Stream stream);

}
