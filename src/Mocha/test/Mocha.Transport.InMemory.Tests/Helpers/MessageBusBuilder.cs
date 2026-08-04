using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.InMemory.Tests.Helpers;

internal static class MessageBusHostBuilderTestExtensions
{
    public static async Task<ServiceProvider> BuildServiceProvider(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public static MessagingRuntime BuildRuntime(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
