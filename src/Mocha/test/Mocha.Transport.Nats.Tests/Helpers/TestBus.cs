using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.Nats.Tests.Helpers;

public sealed class TestBus(ServiceProvider provider, MessagingRuntime runtime) : IAsyncDisposable
{
    public ServiceProvider Provider => provider;

    public MessagingRuntime Runtime => runtime;

    public NatsMessagingTopology Topology
        => (NatsMessagingTopology)runtime.Transports.OfType<NatsMessagingTransport>().Single().Topology;

    public IMessageBus CreateBus(out IServiceScope scope)
    {
        scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMessageBus>();
    }

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
}
