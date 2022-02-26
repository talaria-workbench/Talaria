using System.Xml;

namespace Talaria.AddIn;

public abstract class ComponentBase : IComponentBase
{
    public virtual ReadOnlySpan<string> FileEndings => ReadOnlySpan<string>.Empty;
    private protected ComponentBase()
    {

    }

    public Task<ComponentInstanceBase> Load(IDataReference stream) => this.InternalLoad(stream);
    private protected abstract Task<ComponentInstanceBase> InternalLoad(IDataReference stream);
}

public abstract class ComponentBase<This> : ComponentBase
    where This : ComponentBase<This>
{

    private protected sealed override async Task<ComponentInstanceBase> InternalLoad(IDataReference stream) => await this.Load(stream);

    public new abstract Task<ComponentInstanceBase<This>> Load(IDataReference stream);
}


public interface IComponentBase
{
    Task<ComponentInstanceBase> Load(IDataReference stream);

}

public interface IDataReference
{
    Task<Stream> OpenData();
    Task SaveData(Stream stream);
    public async Task<XmlDocument> OpenXml()
    {
        using var stream =await this.OpenData();
        var doc = new XmlDocument();
        doc.Load(stream);
        return doc;
    }
    public async Task SaveXml(XmlDocument doc)
    {

        using var stream = new MemoryStream();
        doc.Save(stream);
        _ = stream.Seek(0, SeekOrigin.Begin);
        await this.SaveData(stream);
    }
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
