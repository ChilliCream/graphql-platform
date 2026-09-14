using DotNet.Testcontainers.Containers;
using Xunit;

namespace CookieCrumble.Resources;

/// <summary>
/// A test resource that runs a container for the lifetime of the fixture scope
/// (class, collection, or assembly) it is registered with.
/// </summary>
public abstract class ContainerResource<TContainer> : IAsyncLifetime
    where TContainer : IContainer
{
    private TContainer? _container;

    protected TContainer Container
        => _container
            ?? throw new InvalidOperationException(
                "The container has not been started. Call InitializeAsync() first.");

    public async ValueTask InitializeAsync()
    {
        _container = Build();
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeAsyncCore();
        }
        finally
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }

    protected abstract TContainer Build();

    /// <summary>
    /// Releases the clients this resource created on top of the container.
    /// Runs before the container itself is disposed.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
