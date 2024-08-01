namespace HotChocolate.Resolvers;

/// <summary>
/// This is a base class allows to copy state from a single service of the
/// request scope to the same service in the resolver scope.
/// </summary>
/// <typeparam name="TService">
/// The service type for which state shall be copied.
/// </typeparam>
public abstract class ServiceInitializer<TService> : IServiceScopeInitializer
{
    public void Initialize(IServiceProvider requestScope, IServiceProvider resolverScope)
        => Initialize(requestScope.GetRequiredService<TService>(), resolverScope.GetRequiredService<TService>());

    protected abstract void Initialize(TService requestScopeService, TService resolverScopeService);
}
