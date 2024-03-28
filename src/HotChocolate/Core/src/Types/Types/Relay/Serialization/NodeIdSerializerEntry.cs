#nullable enable
using System;

namespace HotChocolate.Types.Relay;

public sealed class NodeIdSerializerEntry(
    string typeName,
    INodeIdValueSerializer serializer)
    : IEquatable<NodeIdSerializerEntry>
{
    public string TypeName { get; } = typeName;

    public INodeIdValueSerializer Serializer { get; } = serializer;

    public bool Equals(NodeIdSerializerEntry? other)
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
        => ReferenceEquals(this, obj) || obj is NodeIdSerializerEntry other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, Serializer);

    public static bool operator ==(NodeIdSerializerEntry? left, NodeIdSerializerEntry? right)
        => Equals(left, right);

    public static bool operator !=(NodeIdSerializerEntry? left, NodeIdSerializerEntry? right)
        => !Equals(left, right);
}
