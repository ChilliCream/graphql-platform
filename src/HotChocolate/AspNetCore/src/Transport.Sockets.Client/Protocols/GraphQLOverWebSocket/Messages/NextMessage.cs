using System.Buffers;
using System.Text.Json;
using HotChocolate.Utilities;

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
        // The ArrayWriter is used to copy the message because otherwise the buffer is reused and
        // causes problems. The ArrayWriter is passed to the OperationResult where it's stored as
        // the memory owner and disposed when the OperationResult is disposed.
        var arrayWriter = new ArrayWriter();
        arrayWriter.Write(message);

        var document = JsonDocument.Parse(arrayWriter.GetWrittenMemory());

        var root = document.RootElement;
        var id = root.GetProperty(Utf8MessageProperties.IdProp).GetString()!;

        var payload = root.GetProperty(Utf8MessageProperties.PayloadProp);
        var result = new OperationResult(
            arrayWriter,
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
