namespace Mocha.Mediator;

/// <summary>
/// Represents the context that flows through the mediator middleware pipeline.
/// </summary>
public interface IMediatorContext
{
    /// <summary>
    /// Gets the scoped service provider for resolving handlers and services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the message being dispatched (command, query, or notification).
    /// </summary>
    object Message { get; }

    /// <summary>
    /// Gets the runtime type of the message.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Gets the expected response type (<see cref="Unit"/> for void commands and notifications).
    /// </summary>
    Type ResponseType { get; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the mediator runtime that owns this context.
    /// </summary>
    IMediatorRuntime Runtime { get; }

    /// <summary>
    /// Gets the per-request feature collection.
    /// Middleware can use this to share state within a single pipeline invocation.
    /// </summary>
    IFeatureCollection Features { get; }

    /// <summary>
    /// Gets or sets the result of the pipeline execution.
    /// Set by the terminal handler delegate.
    /// </summary>
    object? Result { get; set; }
}
