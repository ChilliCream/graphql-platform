using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

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
