using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Scheduling;
using Mocha.Transport.AzureServiceBus.Scheduling;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods for registering the Azure Service Bus messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus messaging transport to the message bus, applying the specified configuration
    /// delegate after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the Azure Service Bus transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddAzureServiceBus(
        this IMessageBusHostBuilder busBuilder,
        Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var transport = new AzureServiceBusMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        busBuilder.Services.TryAddScoped<IScheduledMessageStore>(
            _ => new AzureServiceBusScheduledMessageStore(transport.ClientManager));

        return busBuilder;
    }

    /// <summary>
    /// Adds an Azure Service Bus messaging transport to the message bus with the specified connection string.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="connectionString">The Azure Service Bus connection string.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddAzureServiceBus(
        this IMessageBusHostBuilder busBuilder,
        string connectionString)
    {
        return busBuilder.AddAzureServiceBus(x => x.ConnectionString(connectionString));
    }
}
