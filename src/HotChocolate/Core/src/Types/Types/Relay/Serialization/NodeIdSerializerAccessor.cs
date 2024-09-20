#nullable enable
namespace HotChocolate.Types.Relay;

internal sealed class NodeIdSerializerAccessor : INodeIdSerializerAccessor
{
    private INodeIdSerializer? _serializer;

    public INodeIdSerializer Serializer
    {
        get => _serializer ??
            throw new InvalidOperationException(
                "The node id serializer has not been initialized yet.");
    }

    public void OnSchemaCreated(ISchema schema)
    {
        _serializer ??= schema.Services.GetRequiredService<INodeIdSerializer>();
    }
}
