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
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    protected abstract TContainer Build();
}
