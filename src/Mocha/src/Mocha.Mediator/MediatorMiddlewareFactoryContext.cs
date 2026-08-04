using System.Diagnostics.CodeAnalysis;

namespace Mocha.Mediator;

/// <summary>
/// Provides context to <see cref="MediatorMiddleware"/> factories during pipeline compilation.
/// Contains the message type, response type, and feature collection of the pipeline being compiled,
/// allowing middleware to opt out at compile time by returning <c>next</c> directly.
/// </summary>
public sealed class MediatorMiddlewareFactoryContext
{
    /// <summary>
    /// Gets the root service provider, used to resolve singleton or scoped services
    /// that the middleware instance needs at construction time.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the feature collection for the mediator being built.
    /// Use this to read configuration that was registered via
    /// <see cref="IMediatorBuilder.ConfigureFeature"/>.
    /// </summary>
    public required IFeatureCollection Features { get; init; }

    /// <summary>
    /// Gets the message type of the pipeline being compiled.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
    public Type MessageType { get; internal set; } = null!;

    /// <summary>
    /// Gets the response type of the pipeline being compiled,
    /// or <see langword="null"/> for void commands and notifications.
    /// For stream pipelines this is <c>IAsyncEnumerable&lt;T&gt;</c>.
    /// </summary>
    public Type? ResponseType { get; internal set; }
}
