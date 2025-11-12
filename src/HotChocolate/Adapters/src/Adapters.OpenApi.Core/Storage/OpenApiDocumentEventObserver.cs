namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents an observer that can be used to subscribe to OpenAPI document events.
/// </summary>
public sealed class OpenApiDocumentEventObserver : IObserver<OpenApiDocumentEvent>
{
    private readonly Action<OpenApiDocumentEvent>? _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onCompleted;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiDocumentEventObserver" />.
    /// </summary>
    /// <param name="onNext">
    /// The action that is invoked when a new event is received.
    /// </param>
    /// <param name="onError">
    /// The action that is invoked when an error occurs.
    /// </param>
    /// <param name="onCompleted">
    /// The action that is invoked when the observer is completed.
    /// </param>
    public OpenApiDocumentEventObserver(
        Action<OpenApiDocumentEvent>? onNext = null,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
    {
        _onNext = onNext;
        _onError = onError;
        _onCompleted = onCompleted;
    }

    /// <summary>
    /// Invoked when a new event is received.
    /// </summary>
    /// <param name="value">
    /// The event that was received.
    /// </param>
    public void OnNext(OpenApiDocumentEvent value)
        => _onNext?.Invoke(value);

    /// <summary>
    /// Invoked when an error occurs.
    /// </summary>
    /// <param name="error">
    /// The error that occurred.
    /// </param>
    public void OnError(Exception error)
        => _onError?.Invoke(error);

    /// <summary>
    /// Invoked when the observer is completed.
    /// </summary>
    public void OnCompleted()
        => _onCompleted?.Invoke();
}
