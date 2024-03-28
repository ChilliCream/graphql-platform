#nullable enable
namespace HotChocolate.Types.Relay;

public interface INodeIdSerializerAccessor
{
    INodeIdSerializer Serializer { get; }
}
