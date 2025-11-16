using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class DynamicEndpointDataSource : EndpointDataSource, IDynamicEndpointDataSource, IDisposable
{
    private IReadOnlyList<Endpoint> _endpoints = [];
    private CancellationTokenSource _cts = new();
    private CancellationChangeToken _changeToken;
    private readonly Lock _lock = new();

    public DynamicEndpointDataSource()
    {
        _changeToken = new CancellationChangeToken(_cts.Token);
    }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            lock (_lock)
            {
                return _endpoints;
            }
        }
    }

    public override IChangeToken GetChangeToken() => _changeToken;

    public void SetEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        lock (_lock)
        {
            _endpoints = endpoints;

            NotifyChanged();
        }
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
