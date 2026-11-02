namespace Mocha;

/// <summary>
/// Provides lazy access to the <see cref="IMessagingRuntime"/>, deferring initialization until the runtime is fully built.
/// </summary>
public interface ILazyMessagingRuntime
{
    /// <summary>
    /// Gets the messaging runtime instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the runtime has been initialized.</exception>
    IMessagingRuntime Runtime { get; }
}
