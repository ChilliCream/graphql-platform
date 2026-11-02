using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

internal sealed class MessagingTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly List<IHostedService> _hostedServices;

    private MessagingTestHost(ServiceProvider provider, List<IHostedService> hostedServices)
    {
        _provider = provider;
        _hostedServices = hostedServices;
    }

    public IServiceProvider Services => _provider;

    public static async Task<MessagingTestHost> StartAsync(
        ServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        foreach (var service in hostedServices)
        {
            await service.StartAsync(cancellationToken);
        }

        return new MessagingTestHost(provider, hostedServices);
    }

    public async ValueTask DisposeAsync()
    {
        for (var i = _hostedServices.Count - 1; i >= 0; i--)
        {
            await _hostedServices[i].StopAsync(CancellationToken.None);
        }

        await _provider.DisposeAsync();
    }
}
