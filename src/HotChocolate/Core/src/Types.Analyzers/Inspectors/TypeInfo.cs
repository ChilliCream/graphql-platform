namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class TypeInfo : ISyntaxInfo, IEquatable<TypeInfo>
{
    public TypeInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool Equals(TypeInfo? other)
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

        return other is TypeInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is TypeInfo other && Equals(other);

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(TypeInfo? left, TypeInfo? right)
        => Equals(left, right);

    public static bool operator !=(TypeInfo? left, TypeInfo? right)
        => !Equals(left, right);
}
