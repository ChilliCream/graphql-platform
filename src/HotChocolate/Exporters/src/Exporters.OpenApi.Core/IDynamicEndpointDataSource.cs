using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

internal interface IDynamicEndpointDataSource
{
    IReadOnlyList<Endpoint> Endpoints { get; }

    void SetEndpoints(IReadOnlyList<Endpoint> endpoints);
}
