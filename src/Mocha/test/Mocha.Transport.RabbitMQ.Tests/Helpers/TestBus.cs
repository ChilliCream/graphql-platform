using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

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

internal static class MessageBusHostBuilderTestExtensions
{
    public static async Task<TestBus> BuildTestBusAsync(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return new TestBus(provider, runtime);
    }

    public static MessagingRuntime BuildRuntime(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
