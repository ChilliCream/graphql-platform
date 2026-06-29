namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a field argument on a FieldSelectionMap path segment.
/// </summary>
public sealed class PathArgumentNode
{
    public PathArgumentNode(NameNode name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrEmpty(value);

        Name = name;
        Value = value;
    }

    public NameNode Name { get; }

    public string Value { get; }

    public override string ToString() => $"{Name.Value}: {Value}";
}
