using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Talaria.AddIn;

public interface IEditor
{
    ReadOnlySpan<string> FileEnding { get; }
    DataTemplate Editor { get; }
    Task<object> Open(Stream stream);

}

public abstract class CreateItemBase
{
    public abstract string Label { get; }
    public abstract Microsoft.UI.Xaml.Media.ImageSource Icon { get; }
    public abstract Task Execute(CreateItemOptions options);
    public CreateItemOptions DefaultCreateItemOptions(IEditorContext context) => this.CreateDefaultCreateItemOptions(context);

    protected abstract CreateItemOptions CreateDefaultCreateItemOptions(IEditorContext context);

    protected SvgImageSource LoadSVG(string resourceName, System.Reflection.Assembly? assembly = null)
    {
        if (assembly is null) {
            assembly = this.GetType().Assembly;
        }
        var svg = new SvgImageSource();
        setImage(svg, resourceName, assembly);
        static async void setImage(SvgImageSource svg, string resourceName, System.Reflection.Assembly assembly)
        {
            try {
                svg.RasterizePixelHeight = 24;
                svg.RasterizePixelWidth = 24;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                var status = await svg.SetSourceAsync(stream.AsRandomAccessStream());

            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine($"Faild to load Image {e}");

            }
        }

        return svg;
    }
}

public interface IEditorContext
{
    IEditorContext Root { get; }

    Task<Stream> CreateFileStream(string path);
    Task<Stream> OpenFileStream(string path);

    Task CreateFile<T>(string path, T item);
    Task<T> OpenFile<T>(string path);


    bool ExistsFileOrDirectory(string path);
    Task CreateFolder(string path);

}
public abstract partial class CreateItemOptions
{
    public ImmutableArray<BaseOption> Options { get; }
    protected abstract BaseOption[] DefaultOptions();


    [SourceGenerators.AutoNotify(SetterVisibility = SourceGenerators.Visibility.Private)]
    private bool isValid;

    protected virtual bool AdditionalValidationSuccessfull => true;

    public IEditorContext Context { get; }

    public CreateItemOptions(IEditorContext context)
    {
        this.Options = ImmutableArray.Create(this.DefaultOptions());
        foreach (var o in this.Options) {
            o.PropertyChanged += (sender, e) =>
            {
                System.Diagnostics.Debug.Assert(sender is BaseOption);
                var o = ((BaseOption)sender);
                if (e.PropertyName == nameof(Talaria.AddIn.BaseOption.IsValid) && o.IsValid != this.IsValid) {
                    if (o.IsValid) {
                        this.UpdateIsValid();
                    } else {
                        this.IsValid = false;
                    }
                }
            };
        }
        this.UpdateIsValid();
        this.Context = context;
    }

    public Configuration[] GetConfiguration()
    {
        return this.Options.Select(x => x.GetConfiguration()).ToArray();
    }

    protected void UpdateIsValid()
    {
        var isValid = true;
        if (this.AdditionalValidationSuccessfull) {
            foreach (var o in this.Options) {
                if (!o.IsValid) {
                    isValid = false;
                    break;
                }
            }
        } else {
            isValid = false;
        }
        this.IsValid = isValid;
    }
}

public class Configuration
{
    private protected Configuration() { }
}
public sealed class Configuration<T> : Configuration
{
    public T Value { get; }

    internal Configuration(T value)
    {
        if (value is null) {
            NullabilityInfoContext context = new();
            var info = context.Create(this.GetType().GetProperty(nameof(this.Value))!);
            if (info.WriteState == NullabilityState.NotNull) {
                throw new ArgumentNullException(nameof(value));
            }
        }
        this.Value = value;
    }
}

public abstract class BaseOption : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private protected BaseOption(string label)
    {
        this.Label = label;
    }

    public string Label { get; }

    public abstract bool IsValid { get; }


    internal abstract Configuration GetConfiguration();

    protected void FireNotifyPropertyChanged([CallerMemberName] string propertyName = "") => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}
public abstract class BaseOption<T> : BaseOption
{
    private T? value;

    protected bool IsNullValid => false;

    internal override Configuration GetConfiguration()
    {
        return !this.IsValid
            ? throw new InvalidOperationException($"Can't generate configuration for invalid Option {this.Label}: {(this.value?.ToString() ?? "<NULL>")}")
            : new Configuration<T>(this.Value!);
    }

    public virtual T? Value
    {
        get => this.value; set
        {
            if (!Object.Equals(this.value, value)) {
                this.value = value;
                this.FireNotifyPropertyChanged();
                this.FireNotifyPropertyChanged(nameof(this.IsValid));
            }
        }
    }

    public override bool IsValid
    {
        get
        {
            return this.Value is not null || this.IsNullValid;
        }
    }


    private protected BaseOption(T? initialValue, string label) : base(label)
    {
        this.Value = initialValue;
    }
}


public sealed class TextOption : BaseOption<string>
{
    private readonly bool trim;
    private readonly bool allowEmptyText;
    private readonly Regex? allowPattern;
    private readonly Regex? forbiddPattern;

    public TextOption(string label, string initialValue = "", bool trim = true, bool allowEmptyText = false, Regex? allowPattern = null, Regex? forbiddPattern = null) : base(initialValue, label)
    {
        this.trim = trim;
        this.allowEmptyText = allowEmptyText;
        this.allowPattern = allowPattern;
        this.forbiddPattern = forbiddPattern;
    }

    public override string? Value { get => base.Value ?? string.Empty; set => base.Value = (this.trim ? value?.Trim() : value); }

    public override bool IsValid
    {
        get
        {
            if (!this.allowEmptyText && string.IsNullOrEmpty(this.Value)) {
                return false;
            } else if (this.allowPattern is not null && !this.allowPattern.IsMatch(this.Value ?? string.Empty)) {
                return false;
            } else if (this.forbiddPattern is not null && this.forbiddPattern.IsMatch(this.Value ?? string.Empty)) {
                return false;
            } else {
                return base.IsValid;
            }
        }
    }
}

public sealed class FileOption : BaseOption<FileInfo>
{
    public FileOption(string label, params string[]? validExtensions) : base(null, label)
    {
        this.ValidExtensions = validExtensions ?? Array.Empty<string>();
    }

    public string[] ValidExtensions { get; }
}
public sealed class RangeOption : BaseOption<Range>
{
    public RangeOption(string label, Range initialValue) : base(initialValue, label)
    {
    }
}
