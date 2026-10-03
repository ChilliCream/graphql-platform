#if FUSION
using System.Buffers;
#else
using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
#endif

#if FUSION
internal sealed class ErrorMessage(
    string id,
    PooledSocketPayload payload) : FusionDataMessage(id, payload)
{
    public override string Type => Messages.Error;

    public static ErrorMessage From(
        WebSocketMessageLocation location,
        ArrayPool<byte> pool)
    {
        var id = location.Id ?? throw ThrowHelper.MessageHasNoId();
        return new ErrorMessage(
            id,
            WebSocketMessageParser.CopyPayload(
                location,
                pool,
                "{\"errors\":"u8,
                "}"u8));
    }
}
#else
internal sealed class ErrorMessage : IDataMessage
{
    private ErrorMessage(string id, OperationResult payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Id { get; }

    public string Type => Messages.Error;

    public OperationResult Payload { get; }

    public void Dispose()
        => Payload.Dispose();

    public static ErrorMessage From(ReadOnlySequence<byte> message)
    {
        // The ArrayWriter is used to copy the message because otherwise the buffer is reused and
        // causes problems. The ArrayWriter is passed to the OperationResult where it's stored as
        // the memory owner and disposed when the OperationResult is disposed.
        var arrayWriter = new PooledArrayWriter();
        arrayWriter.Write(message);

        var document = JsonDocument.Parse(arrayWriter.WrittenMemory);

        var root = document.RootElement;
        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString();

        if (id is null)
        {
            arrayWriter.Dispose();
            document.Dispose();
            throw ThrowHelper.MessageHasNoId();
        }

        var documentOwner = new JsonDocumentOwner(document, arrayWriter);
        var payload = root.GetProperty(Utf8MessageProperties.PayloadProp);
        var result = new OperationResult(documentOwner, errors: payload);

        return new ErrorMessage(id, result);
    }
}
#endif
