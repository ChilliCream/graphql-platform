using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Add an abstraction that can be added to schema services
internal sealed class DynamicEndpointDataSource : EndpointDataSource, IDisposable
{
    private List<Endpoint> _endpoints = [];
    private CancellationTokenSource _cts = new();
    private CancellationChangeToken _changeToken;

    public DynamicEndpointDataSource()
    {
        _changeToken = new CancellationChangeToken(_cts.Token);
    }

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public override IChangeToken GetChangeToken() => _changeToken;

    // TODO: Synchronization
    public void SetEndpoints(IEnumerable<ExecutableOpenApiDocument> documents)
    {
        var newEndpoints = new List<Endpoint>();

        foreach (var document in documents)
        {
            var endpoint = CreateEndpoint(document);

            newEndpoints.Add(endpoint);
        }

        _endpoints = newEndpoints;

        NotifyChanged();
    }

    private static Endpoint CreateEndpoint(ExecutableOpenApiDocument document)
    {
        // TODO: Use proper schema name
        var schemaName = ISchemaDefinition.DefaultName;

        var builder = new RouteEndpointBuilder(

            requestDelegate: CreateRequestDelegate(schemaName, document),
            routePattern: document.Route,
            // TODO: What does this control?
            order: 0)
        {
            DisplayName = document.HttpMethod + " " + document.Route.RawText
        };

        builder.Metadata.Add(new HttpMethodMetadata([document.HttpMethod]));

        return builder.Build();
    }

    private static RequestDelegate CreateRequestDelegate(string schemaName, ExecutableOpenApiDocument document)
    {
        var middleware = new DynamicEndpointMiddleware(schemaName, document);
        return context => middleware.InvokeAsync(context);
    }

    private void NotifyChanged()
    {
        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cts.Token);
        oldCts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
