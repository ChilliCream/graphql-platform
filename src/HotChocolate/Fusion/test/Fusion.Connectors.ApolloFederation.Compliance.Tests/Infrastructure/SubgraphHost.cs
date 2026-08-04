using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion;

/// <summary>
/// An Apollo Federation subgraph hosted in-process under
/// <see cref="TestServer"/>. Disposing stops and releases the web application.
/// </summary>
public sealed class SubgraphHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private bool _disposed;

    internal SubgraphHost(string name, WebApplication app)
    {
        Name = name;
        _app = app;
        Server = app.GetTestServer();
    }

    /// <summary>
    /// The Fusion source-schema name (used by the gateway to route requests).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The underlying <see cref="TestServer"/>. Fresh <see cref="HttpClient"/>
    /// instances are produced by <see cref="TestServer.CreateClient"/>.
    /// </summary>
    public TestServer Server { get; }

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> that routes to the subgraph's
    /// in-process <see cref="TestServer"/>. The caller owns the returned client.
    /// </summary>
    public HttpClient CreateClient() => Server.CreateClient();

    /// <summary>
    /// Stops the subgraph's web application and disposes its resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
