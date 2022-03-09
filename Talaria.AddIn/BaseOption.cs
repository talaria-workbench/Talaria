using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Talaria.AddIn;

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

public sealed class ReferenceInstanceOption<TComponent, TInstance> : BaseOption<TInstance>
    where TInstance : InstanceBase<TComponent, TInstance>
    where TComponent : ComponentBase<TComponent, TInstance>
{
    public ReferenceInstanceOption(string label) : base(null, label)
    {
    }
}
public sealed class ReferenceComponentOption<TComponent, TInstance> : BaseOption<TComponent>
    where TInstance : InstanceBase<TComponent, TInstance>
    where TComponent : ComponentBase<TComponent, TInstance>
{
    public ReferenceComponentOption(string label) : base(null, label)
    {
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
public sealed class IntOption : BaseOption<int>
{
    public int? Min { get; }
    public int? Max { get; }


    public IntOption(string label, int? initialValue = null, int? min = null, int? max = null) : base(initialValue ?? min ?? max ?? 0, label)
    {
    }
}
