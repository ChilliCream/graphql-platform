#nullable enable

namespace HotChocolate.Internal;

public readonly struct ExtendedTypeId
{
    public ExtendedTypeId(Type type, ExtendedTypeKind kind, uint nullability)
    {
        Type = type;
        Kind = kind;
        Nullability = nullability;
    }

    public Type Type { get; }

    public ExtendedTypeKind Kind { get; }

    public uint Nullability { get; }

    public bool Equals(ExtendedTypeId other) =>
        Type == other.Type &&
        Nullability == other.Nullability &&
        Kind == other.Kind;

    public override bool Equals(object? obj) =>
        obj is ExtendedTypeId other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return Type.GetHashCode() * 397 ^
                   Nullability.GetHashCode() * 397 ^
                   Kind.GetHashCode() * 397;
        }
    }
}
