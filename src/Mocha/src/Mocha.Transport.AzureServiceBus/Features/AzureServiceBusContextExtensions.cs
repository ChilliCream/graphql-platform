using Azure.Messaging.ServiceBus;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods that surface the Azure Service Bus SDK delivery context from the active
/// <see cref="IMessageContext"/>. Handlers running on the Azure Service Bus transport can reach the
/// raw <see cref="ProcessMessageEventArgs"/> to drive native broker primitives
/// (dead-lettering, abandon with property modifications, lock renewal).
/// </summary>
internal static class AzureServiceBusContextExtensions
{
    /// <summary>
    /// Gets the Azure Service Bus <see cref="ProcessMessageEventArgs"/> for the current message.
    /// </summary>
    /// <param name="context">The active message context.</param>
    /// <returns>The <see cref="ProcessMessageEventArgs"/> for the current invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current message did not originate from the Azure Service Bus transport.
    /// </exception>
    public static ProcessMessageEventArgs GetAzureServiceBusEventArgs(this IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<AzureServiceBusReceiveFeature>() is { ProcessMessageEventArgs: { } args })
        {
            return args;
        }

        throw new InvalidOperationException(
            "The current message context is not running on the Azure Service Bus transport. "
            + "ProcessMessageEventArgs is only available for handlers receiving from ASB.");
    }
}
