namespace Mocha;

/// <summary>
/// An interceptor that can hook into various topology initialization events
/// of the messaging bus. This is useful to transform topology shape and behavior.
/// Modeled after HotChocolate's TypeInterceptor pattern for consistent framework composition.
/// </summary>
public abstract class BusInterceptor
{
    private const uint DefaultPosition = uint.MaxValue / 2;

    /// <summary>
    /// Gets a weight to order interceptors.
    /// Interceptors are executed in ascending order by position.
    /// </summary>
    public virtual uint Position => DefaultPosition;

    /// <summary>
    /// Determines whether this interceptor is enabled in the given service context.
    /// </summary>
    /// <param name="services">
    /// The service provider.
    /// </param>
    /// <returns>
    /// <c>true</c> if the interceptor is enabled; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsEnabled(IServiceProvider services) => true;

    /// <summary>
    /// This hook is invoked before the topology is finalized.
    /// At this point, all message types have been registered, all routes have been
    /// initialized and completed, and endpoint discovery is finished.
    /// Interceptors can inspect the topology model at this point and apply mutations.
    /// </summary>
    /// <param name="context">
    /// The bus interceptor topology context.
    /// </param>
    public virtual void OnBeforeTopologyComplete(IBusInterceptorTopologyContext context)
    {
    }

    /// <summary>
    /// This hook is invoked after the topology is finalized.
    /// At this point, the topology is complete and all entities are finalized.
    /// This is useful for validation or logging of the complete topology.
    /// </summary>
    /// <param name="context">
    /// The bus interceptor topology context.
    /// </param>
    public virtual void OnAfterTopologyComplete(IBusInterceptorTopologyContext context)
    {
    }
}
