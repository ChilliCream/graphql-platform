using System.Diagnostics.CodeAnalysis;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods that surface the Azure Service Bus power-user message context from the
/// active <see cref="IMessageContext"/>.
/// </summary>
public static class AzureServiceBusContextExtensions
{
    /// <summary>
    /// Resolves the Azure Service Bus message context for the current message.
    /// </summary>
    /// <param name="context">The active message context.</param>
    /// <returns>The Azure Service Bus message context for the current invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current message did not originate from the Azure Service Bus transport.
    /// </exception>
    public static IAzureServiceBusMessageContext AzureServiceBus(this IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<IAzureServiceBusMessageContext>() is { } asb)
        {
            return asb;
        }

        throw new InvalidOperationException(
            "The current message context is not running on the Azure Service Bus transport. "
            + "IAzureServiceBusMessageContext is only available for handlers receiving from ASB.");
    }

    /// <summary>
    /// Tries to resolve the Azure Service Bus message context for the current message.
    /// </summary>
    /// <param name="context">The active message context.</param>
    /// <param name="azureServiceBus">
    /// When this method returns, contains the Azure Service Bus message context if one is present,
    /// otherwise <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current message originated from the Azure Service Bus transport; otherwise <c>false</c>.
    /// </returns>
    public static bool TryGetAzureServiceBus(
        this IMessageContext context,
        [NotNullWhen(true)] out IAzureServiceBusMessageContext? azureServiceBus)
    {
        ArgumentNullException.ThrowIfNull(context);

        azureServiceBus = context.Features.Get<IAzureServiceBusMessageContext>();
        return azureServiceBus is not null;
    }
}
