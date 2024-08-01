#nullable enable
namespace HotChocolate.Types.Relay;

public readonly struct NodeId(string typeName, object internalId)
{
    public string TypeName { get; } = typeName;

    public object InternalId { get; } = internalId;

    public bool Equals(NodeId other)
        => TypeName == other.TypeName &&
            InternalId.Equals(other.InternalId);

    public override bool Equals(object? obj)
        => obj is NodeId other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, InternalId);

    public override string ToString()
        => $"{TypeName}:{InternalId}";

    public static bool operator ==(NodeId left, NodeId right)
        => left.Equals(right);

    public static bool operator !=(NodeId left, NodeId right)
        => !left.Equals(right);
}
