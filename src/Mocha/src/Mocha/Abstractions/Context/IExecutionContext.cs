using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides the runtime services and cancellation support required during middleware pipeline execution.
/// </summary>
public interface IExecutionContext : IFeatureProvider
{
    /// <summary>
    /// Gets or sets the messaging runtime that owns this execution.
    /// </summary>
    IMessagingRuntime Runtime { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token used to signal that the execution should be aborted.
    /// </summary>
    CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the scoped service provider available for dependency resolution during execution.
    /// </summary>
    IServiceProvider Services { get; set; }
}
