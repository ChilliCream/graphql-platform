namespace HotChocolate.Types.Analyzers.Models;

public sealed class TypeInfo(string name) : SyntaxInfo
{
    public string Name { get; } = name;

    public override string OrderByKey => Name;

    public override bool Equals(object? obj)
        => obj is TypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? other)
        => other is TypeInfo info && Equals(info);

    private bool Equals(TypeInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Name);
}
