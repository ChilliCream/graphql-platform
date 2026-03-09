using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal sealed class NextMessage : IDataMessage
{
    private NextMessage(string id, OperationResult payload)
    {
        Id = id;
        Payload = payload;
    }

    public string Id { get; }

    public string Type => Messages.Next;

    public OperationResult Payload { get; }

    public static NextMessage From(ReadOnlySequence<byte> message)
    {
        // The ArrayWriter is used to copy the message because otherwise, the buffer is reused and
        // causes problems. We bundle the arrayWriter and the document into a JsonDocumentOwner,
        // which will be passed on with the operation result as the memory owner.
        // The JsonDocumentOwner will be disposed when the OperationResult is disposed and in turn
        // returns the memory to the pool and disposes the JsonDocument.
        var arrayWriter = new PooledArrayWriter();
        arrayWriter.Write(message);

        var document = JsonDocument.Parse(arrayWriter.WrittenMemory);
        var documentOwner = new JsonDocumentOwner(document, arrayWriter);

        var root = document.RootElement;

        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString()!;
        var payload = root.GetProperty(Utf8MessageProperties.PayloadProp);

        var result = new OperationResult(
            documentOwner,
            TryGetProperty(payload, Utf8MessageProperties.DataProp),
            TryGetProperty(payload, Utf8MessageProperties.ErrorsProp),
            TryGetProperty(payload, Utf8MessageProperties.ExtensionsProp));

        return new NextMessage(id, result);
    }

    private static JsonElement TryGetProperty(JsonElement element, ReadOnlySpan<byte> name)
        => element.TryGetProperty(name, out var property)
            ? property
            : default;
}
