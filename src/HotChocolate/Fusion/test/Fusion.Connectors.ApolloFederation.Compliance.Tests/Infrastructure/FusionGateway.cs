using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// A composed Fusion gateway over a set of Apollo Federation subgraph
/// <see cref="WebApplication"/>s running in-process under
/// <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>. Disposing the gateway tears
/// down the subgraph hosts and the gateway service provider.
/// </summary>
public sealed class FusionGateway : IAsyncDisposable
{
    private readonly IReadOnlyList<SubgraphHost> _subgraphs;
    private readonly ServiceProvider _services;
    private bool _disposed;

    internal FusionGateway(
        IRequestExecutor executor,
        ServiceProvider services,
        IReadOnlyList<SubgraphHost> subgraphs)
    {
        Executor = executor;
        _services = services;
        _subgraphs = subgraphs;
    }

    /// <summary>
    /// The Fusion gateway <see cref="IRequestExecutor"/>.
    /// </summary>
    public IRequestExecutor Executor { get; }

    /// <summary>
    /// Tears down subgraph hosts and disposes the gateway service provider.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var subgraph in _subgraphs)
        {
            await subgraph.DisposeAsync().ConfigureAwait(false);
        }

        await _services.DisposeAsync().ConfigureAwait(false);
    }
}
