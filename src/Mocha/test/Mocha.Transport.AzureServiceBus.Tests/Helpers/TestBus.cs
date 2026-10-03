using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

public sealed class TestBus(ServiceProvider provider) : IAsyncDisposable
{
    public ServiceProvider Provider => provider;

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}

internal static class MessageBusHostBuilderTestExtensions
{
    public static IMessageBusHostBuilder AddAzureServiceBus(
        this IMessageBusHostBuilder builder,
        TestContext ctx)
    {
        return builder.AddAzureServiceBus(t =>
        {
            t.ConnectionString(ctx.ConnectionString);
            t.AdministrationConnectionString(ctx.AdminConnectionString);
        });
    }

    public static async Task<TestBus> BuildTestBusAsync(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return new TestBus(provider);
    }

    public static MessagingRuntime BuildRuntime(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
