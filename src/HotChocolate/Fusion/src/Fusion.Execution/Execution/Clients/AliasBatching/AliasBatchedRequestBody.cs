using HotChocolate.Fusion.Transport;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Carries the pre-serialized body of an alias batched GraphQL request. The merged
/// <c>{ "query", "variables" }</c> object is built once into a pooled buffer so the merged
/// variables can be assembled with a single writer, then emitted verbatim when the transport
/// serializes the request.
/// </summary>
internal sealed class AliasBatchedRequestBody : IRequestBody
{
    private readonly ReadOnlyMemory<byte> _body;

    /// <summary>
    /// Initializes a new instance of <see cref="AliasBatchedRequestBody"/>.
    /// </summary>
    /// <param name="body">
    /// The complete UTF-8 JSON request object to send. The memory must remain valid until the
    /// request has been serialized by the transport.
    /// </param>
    public AliasBatchedRequestBody(ReadOnlyMemory<byte> body)
    {
        _body = body;
    }

    /// <inheritdoc />
    public void WriteTo(JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteRawValue(_body.Span);
    }
}
