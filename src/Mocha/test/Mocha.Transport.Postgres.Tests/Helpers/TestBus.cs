using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.Postgres.Tests.Helpers;

public sealed class TestBus(ServiceProvider provider, MessagingRuntime runtime) : IAsyncDisposable
{
    public ServiceProvider Provider => provider;

    public async ValueTask DisposeAsync()
    {
        foreach (var transport in runtime.Transports)
        {
            if (transport.IsStarted)
            {
                await transport.StopAsync(runtime, CancellationToken.None);
            }
        }

        await provider.DisposeAsync();
    }
}

internal static class PostgresTestBusExtensions
{
    public static async Task<TestBus> BuildTestBusAsync(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return new TestBus(provider, runtime);
    }
}
