using System.Reflection;

namespace Talaria.AddIn;

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
