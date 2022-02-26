namespace Talaria.AddIn;

public abstract class ComponentBase : IComponentBase
{
    public virtual ReadOnlySpan<string> FileEndings => ReadOnlySpan<string>.Empty;
    private protected ComponentBase()
    {

    }

    Task<ComponentInstanceBase> IComponentBase.Load(Stream stream) => this.InternalLoad(stream);
    private protected abstract Task<ComponentInstanceBase> InternalLoad(Stream stream);
}

public interface IComponentBase
{
    Task<ComponentInstanceBase> Load(Stream stream);

}
public abstract class ComponentBase<This> : ComponentBase
    where This : ComponentBase<This>
{

    private protected sealed override async Task<ComponentInstanceBase> InternalLoad(Stream stream) => await this.Load(stream);

    public abstract Task<ComponentInstanceBase<This>> Load(Stream stream);
}

public interface IMetadataComponent<TAtached>
    where TAtached : ComponentBase<TAtached>
{
    TAtached AttachedTo { get; }
}

public abstract class MetadataComponentBase<This, TAtached> : ComponentBase<This>, IMetadataComponent<TAtached>
    where This : MetadataComponentBase<This, TAtached>
    where TAtached : ComponentBase<TAtached>
{
    public abstract TAtached AttachedTo { get; }
}
