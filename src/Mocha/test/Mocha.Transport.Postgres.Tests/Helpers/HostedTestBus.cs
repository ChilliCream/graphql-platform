using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha.Transport.Postgres.Tests.Helpers;

/// <summary>
/// A bus whose lifecycle is driven through its hosted services, the same way a host drives it.
/// </summary>
public sealed class HostedTestBus(ServiceProvider provider) : IAsyncDisposable
{
    private readonly IHostedService[] _hostedServices = [.. provider.GetServices<IHostedService>()];

    public ServiceProvider Provider => provider;

    public MessagingRuntime Runtime => (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var hostedService in _hostedServices)
        {
            await hostedService.StartAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        for (var i = _hostedServices.Length - 1; i >= 0; i--)
        {
            await _hostedServices[i].StopAsync(cancellationToken);
        }
    }

    public async Task PublishAsync<T>(T message) where T : notnull
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(message, CancellationToken.None);
    }

    public async Task PublishAsync<T>(T message, DateTimeOffset scheduledTime) where T : notnull
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(message, new PublishOptions { ScheduledTime = scheduledTime }, CancellationToken.None);
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}

internal static class HostedTestBusExtensions
{
    public static async Task<HostedTestBus> StartHostedTestBusAsync(this IMessageBusHostBuilder builder)
    {
        var bus = new HostedTestBus(builder.Services.BuildServiceProvider());
        await bus.StartAsync(CancellationToken.None);
        return bus;
    }
}
