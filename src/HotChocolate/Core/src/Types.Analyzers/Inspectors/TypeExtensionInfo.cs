namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class TypeExtensionInfo : ISyntaxInfo, IEquatable<TypeExtensionInfo>
{
    public TypeExtensionInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

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
