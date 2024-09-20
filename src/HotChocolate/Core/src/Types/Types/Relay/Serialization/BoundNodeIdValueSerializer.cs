#nullable enable
namespace HotChocolate.Types.Relay;

internal sealed class BoundNodeIdValueSerializer(
    string typeName,
    INodeIdValueSerializer serializer)
    : IEquatable<BoundNodeIdValueSerializer>
{
    public string TypeName { get; } = typeName;

    public INodeIdValueSerializer Serializer { get; } = serializer;

    public bool Equals(BoundNodeIdValueSerializer? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TypeName == other.TypeName &&
            Serializer.Equals(other.Serializer);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is BoundNodeIdValueSerializer other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, Serializer);

    public static bool operator ==(BoundNodeIdValueSerializer? left, BoundNodeIdValueSerializer? right)
        => Equals(left, right);

    public static bool operator !=(BoundNodeIdValueSerializer? left, BoundNodeIdValueSerializer? right)
        => !Equals(left, right);
}
