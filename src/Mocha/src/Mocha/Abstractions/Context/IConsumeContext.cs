namespace Mocha;

/// <summary>
/// Represents the context available during message consumption, combining message metadata with
/// execution capabilities.
/// </summary>
public interface IConsumeContext : IMessageContext, IExecutionContext
{
    /// <summary>
    /// Creates an isolated copy of this context using the specified services.
    /// </summary>
    /// <param name="services">The service provider for the cloned execution.</param>
    /// <returns>
    /// A context of the same concrete type with copied message metadata and headers, and its own feature
    /// collection that falls back to the features of this context.
    /// </returns>
    IConsumeContext Clone(IServiceProvider services);
}
