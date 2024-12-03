namespace HotChocolate.Types.Analyzers.Models;

public sealed class TypeExtensionInfo(string name, bool isStatic, OperationType type = OperationType.No) : SyntaxInfo
{
    public string Name { get; } = name;

    public bool IsStatic { get; } = isStatic;

    public OperationType Type { get; } = type;

    public override string OrderByKey => Name;

    public override bool Equals(object? obj)
        => obj is TypeExtensionInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? other)
        => other is TypeExtensionInfo info && Equals(info);

    private bool Equals(TypeExtensionInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal)
            && IsStatic == other.IsStatic
            && Type.Equals(other.Type);

    public override int GetHashCode()
        => HashCode.Combine(Name, IsStatic, Type);
}
