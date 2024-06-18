namespace HotChocolate.Types.Analyzers.Models;

public sealed class TypeInfo(string name) : ISyntaxInfo
{
    public string Name { get; } = name;

    public override bool Equals(object? obj)
        => obj is TypeInfo other && Equals(other);

    public bool Equals(ISyntaxInfo? other)
        => other is TypeInfo info && Equals(info);

    private bool Equals(TypeInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Name);
}
