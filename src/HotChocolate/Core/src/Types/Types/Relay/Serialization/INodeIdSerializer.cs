#nullable enable
using System;

namespace HotChocolate.Types.Relay;

public interface INodeIdSerializer
{
    string Format(string typeName, object internalId);

    NodeId Parse(string formattedId);

    NodeId Parse(string formattedId, Type runtimeType);
}
