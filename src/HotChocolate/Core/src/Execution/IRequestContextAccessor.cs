namespace HotChocolate.Execution;

/// <summary>
/// The <see cref="IRequestContextAccessor"/> allows access to the
/// <see cref="RequestContext"/> during request execution.
/// Be aware that the <see cref="RequestContext"/> is not thread-safe and should
/// not be mutated within resolvers.
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>
    /// Gets the <see cref="RequestContext"/>.
    /// </summary>
    RequestContext RequestContext { get; }
}
