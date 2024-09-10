using static StrawberryShake.Properties.Resources;

namespace StrawberryShake;

/// <summary>
/// Represents a request result object containing the
/// server result and/or the transport exception.
/// </summary>
/// <typeparam name="TBody">
/// The response data.
/// </typeparam>
public sealed class Response<TBody> : IDisposable where TBody : class
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="Response{TBody}"/>.
    /// </summary>
    /// <param name="body">
    /// The serialized response payload.
    /// </param>
    /// <param name="exception">
    /// The transport exception.
    /// </param>
    /// <param name="isPatch">
    /// Defines if this is a partial result that
    /// shall be patched onto a previous one.
    /// </param>
    /// <param name="hasNext">
    /// Defines if there are more partial results expected.
    /// </param>
    /// <param name="extensions">
    /// Additional custom data provided by the server.
    /// </param>
    /// <param name="contextData">
    /// Additional custom data provided by client extensions.
    /// </param>
    public Response(
        TBody? body,
        Exception? exception,
        bool isPatch = false,
        bool hasNext = false,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        if (body is null && exception is null)
        {
            throw new ArgumentNullException(nameof(body), Response_BodyAndExceptionAreNull);
        }

        Body = body;
        Exception = exception;
        HasNext = hasNext;
        Extensions = extensions;
        ContextData = contextData;
        IsPatch = isPatch;
    }

    /// <summary>
    /// The serialized response body.
    /// </summary>
    public TBody? Body { get; }

    /// <summary>
    /// The transport exception.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Defines if this is a partial result that
    /// shall be patched onto a previous one.
    /// </summary>
    public bool IsPatch { get; }

    /// <summary>
    /// Defines if there are more partial results expected.
    /// </summary>
    public bool HasNext { get; }

    /// <summary>
    /// Gets additional custom data provided by the server.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets additional custom data provided by client extensions.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (Body is IDisposable d)
            {
                d.Dispose();
            }

            _disposed = true;
        }
    }
}
