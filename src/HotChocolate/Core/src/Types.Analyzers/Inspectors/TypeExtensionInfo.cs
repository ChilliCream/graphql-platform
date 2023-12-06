namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class TypeExtensionInfo : ISyntaxInfo, IEquatable<TypeExtensionInfo>
{
    public TypeExtensionInfo(string name, bool isStatic, OperationType type = OperationType.No)
    {
        Name = name;
        IsStatic = isStatic;
        Type = type;
    }

    public string Name { get; }

    public bool IsStatic { get; }

    public OperationType Type { get; }

    public bool Equals(TypeExtensionInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
    }
    
    public bool Equals(ISyntaxInfo other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is TypeExtensionInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is TypeExtensionInfo other && Equals(other);

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(TypeExtensionInfo? left, TypeExtensionInfo? right)
        => Equals(left, right);

    public static bool operator !=(TypeExtensionInfo? left, TypeExtensionInfo? right)
        => !Equals(left, right);
}
