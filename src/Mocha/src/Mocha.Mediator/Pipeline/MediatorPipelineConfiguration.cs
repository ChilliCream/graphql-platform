using System.ComponentModel;

namespace Mocha.Mediator;

/// <summary>
/// Describes a pipeline to be compiled for a specific message type.
/// Carries all metadata needed by the middleware compiler so it does not
/// have to derive information from the message type at runtime.
/// This type is intended for use by source-generated code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MediatorPipelineConfiguration
{
    /// <summary>
    /// Gets the message type this pipeline handles.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the response type produced by the handler,
    /// or <see langword="null"/> for void commands and notifications.
    /// For stream handlers this is <c>IAsyncEnumerable&lt;TResponse&gt;</c>.
    /// </summary>
    public Type? ResponseType { get; init; }

    /// <summary>
    /// Gets the terminal delegate that invokes the handler.
    /// This is the innermost layer of the middleware pipeline.
    /// </summary>
    public required MediatorDelegate Terminal { get; init; }
}
