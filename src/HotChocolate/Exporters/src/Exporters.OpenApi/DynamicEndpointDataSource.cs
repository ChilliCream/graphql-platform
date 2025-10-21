using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class DynamicEndpointDataSource : EndpointDataSource, IDisposable
{
    private readonly List<Endpoint> _endpoints = new();
    private CancellationTokenSource _cts = new();
    private CancellationChangeToken _changeToken;

    public DynamicEndpointDataSource()
    {
        _changeToken = new CancellationChangeToken(_cts.Token);
    }

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public override IChangeToken GetChangeToken() => _changeToken;

    public void AddRoute(string pattern, RequestDelegate handler)
    {
        var routePattern = RoutePatternFactory.Parse(pattern);

        var builder = new RouteEndpointBuilder(
            requestDelegate: handler,
            routePattern: routePattern,
            order: 0)
        {
            DisplayName = pattern
        };

        _endpoints.Add(builder.Build());
    }

    // public void AddRoute(string pattern, Func<RouteValueDictionary, Task<IResult>> handler)
    // {
    //     AddRoute(pattern, async context =>
    //     {
    //         var routeValues = context.GetRouteData().Values;
    //         var result = await handler(routeValues);
    //         await result.ExecuteAsync(context);
    //     });
    // }

    public void NotifyChanged()
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
