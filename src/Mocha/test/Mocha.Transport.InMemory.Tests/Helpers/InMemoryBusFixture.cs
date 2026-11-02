using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.InMemory.Tests.Helpers;

public static class InMemoryBusFixture
{
    public static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public static async Task<ServiceProvider> CreateBusWithTransportAsync(
        Action<IMessageBusHostBuilder> configureHost,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureHost(builder);
        builder.AddInMemory(configureTransport);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    public static MessagingRuntime CreateRuntimeWithTransport(
        Action<IMessageBusHostBuilder> configureHost,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureHost(builder);
        builder.AddInMemory(configureTransport);

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
