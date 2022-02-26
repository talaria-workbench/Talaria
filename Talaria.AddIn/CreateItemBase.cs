using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Immutable;

namespace Talaria.AddIn;


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
