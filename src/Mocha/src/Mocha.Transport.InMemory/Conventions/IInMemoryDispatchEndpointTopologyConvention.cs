namespace Mocha.Transport.InMemory;

/// <summary>
/// Convention that discovers and provisions topology resources for in-memory dispatch endpoints.
/// </summary>
public interface IInMemoryDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<InMemoryDispatchEndpoint, InMemoryDispatchEndpointConfiguration>;
