namespace Talaria.AddIn;

public abstract class InstanceBase
{
    private protected InstanceBase()
    {

    }

    public abstract IEditor CreateEditor();

}


public abstract class InstanceBase<TComponent, TInstance> : InstanceBase
    where TComponent : ComponentBase<TComponent, TInstance>
    where TInstance : InstanceBase<TComponent, TInstance>
{
    // TODO: Reactiate Subcomponents
    //public ObservableCollection<IMetadataComponent<TComponent>> Metadata { get; } = new();

}
