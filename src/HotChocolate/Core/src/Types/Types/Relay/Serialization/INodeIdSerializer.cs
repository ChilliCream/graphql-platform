#nullable enable
namespace HotChocolate.Types.Relay;

public interface INodeIdSerializer
{
    string Format(string typeName, object internalId);

    NodeId Parse(string formattedId);
}
