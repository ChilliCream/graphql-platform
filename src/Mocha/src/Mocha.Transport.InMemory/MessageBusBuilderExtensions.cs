using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mocha.Scheduling;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Extension methods for registering the in-memory messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class InMemoryMessageBusBuilderExtensions
{
    /// <summary>
    /// Adds an in-memory messaging transport to the message bus and configures it with the supplied delegate.
    /// </summary>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <param name="configure">A delegate to configure endpoints, topology, middleware, and conventions.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddInMemory(
        this IMessageBusHostBuilder busBuilder,
        Action<IInMemoryMessagingTransportDescriptor> configure)
    {
        var transport = new InMemoryMessagingTransport(configure);

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        busBuilder.Services.TryAddSingleton<InMemoryScheduledMessageStore>();
        busBuilder.Services.TryAddSingleton<IScheduledMessageStore>(
            sp => sp.GetRequiredService<InMemoryScheduledMessageStore>());
        busBuilder.Services.TryAddSingleton(sp => new InMemoryScheduledMessageWorker(
            sp,
            sp.GetRequiredService<IMessagingRuntime>(),
            sp.GetRequiredService<IMessagingPools>(),
            sp.GetRequiredService<ISchedulerSignal>(),
            sp.GetRequiredService<InMemoryScheduledMessageStore>(),
            sp.GetService<TimeProvider>() ?? TimeProvider.System,
            sp.GetRequiredService<ILogger<InMemoryScheduledMessageWorker>>()));
        busBuilder.Services.AddHostedService(
            sp => sp.GetRequiredService<InMemoryScheduledMessageWorker>());

        busBuilder.UseSchedulerCore();

        return busBuilder;
    }

    /// <summary>
    /// Adds an in-memory messaging transport to the message bus with default configuration.
    /// </summary>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddInMemory(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddInMemory(static _ => { });
    }
}
