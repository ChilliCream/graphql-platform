namespace Mocha.Transport.InMemory;

/// <summary>
/// Convention that discovers and provisions topology resources for in-memory receive endpoints.
/// </summary>
public interface IInMemoryReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<InMemoryReceiveEndpoint, InMemoryReceiveEndpointConfiguration>;
