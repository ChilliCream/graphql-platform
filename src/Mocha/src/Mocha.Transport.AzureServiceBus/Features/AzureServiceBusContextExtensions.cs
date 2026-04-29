using Azure.Messaging.ServiceBus;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods that surface the Azure Service Bus SDK delivery context from the active
/// <see cref="IMessageContext"/>. Handlers running on the Azure Service Bus transport reach the
/// raw SDK event-args here for native broker primitives (dead-lettering, abandon with property
/// modifications, session id, session state, etc.).
/// </summary>
public static class AzureServiceBusContextExtensions
{
    /// <summary>
    /// Gets the Azure Service Bus <see cref="ProcessMessageEventArgs"/> for the current message
    /// on a non-session endpoint.
    /// </summary>
    /// <param name="context">The active message context.</param>
    /// <returns>The <see cref="ProcessMessageEventArgs"/> for the current invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current message did not originate from the Azure Service Bus transport, or
    /// when the endpoint is session-bound. On a session endpoint, call
    /// <see cref="GetAzureServiceBusSessionEventArgs"/> instead.
    /// </exception>
    public static ProcessMessageEventArgs GetAzureServiceBusEventArgs(this IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<AzureServiceBusReceiveFeature>() is { } feature)
        {
            if (feature.ProcessMessageEventArgs is { } args)
            {
                return args;
            }

            if (feature.ProcessSessionMessageEventArgs is not null)
            {
                throw new InvalidOperationException(
                    "The current Azure Service Bus endpoint is session-bound. "
                    + "Call GetAzureServiceBusSessionEventArgs() instead.");
            }
        }

        throw new InvalidOperationException(
            "The current message context is not running on a non-session Azure Service Bus endpoint. "
            + "ProcessMessageEventArgs is only available for handlers receiving from ASB.");
    }

    /// <summary>
    /// Gets the Azure Service Bus <see cref="ProcessSessionMessageEventArgs"/> for the current
    /// message on a session-bound endpoint. The returned args expose the session-only primitives
    /// (<c>SessionId</c>, <c>SessionLockedUntil</c>, <c>GetSessionStateAsync</c>,
    /// <c>SetSessionStateAsync</c>, <c>RenewSessionLockAsync</c>, <c>ReleaseSession</c>) directly.
    /// </summary>
    /// <param name="context">The active message context.</param>
    /// <returns>The <see cref="ProcessSessionMessageEventArgs"/> for the current invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current message did not originate from a session-bound Azure Service Bus
    /// endpoint.
    /// </exception>
    public static ProcessSessionMessageEventArgs GetAzureServiceBusSessionEventArgs(this IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Features.Get<AzureServiceBusReceiveFeature>() is { ProcessSessionMessageEventArgs: { } args })
        {
            return args;
        }

        throw new InvalidOperationException(
            "The current message context is not running on a session-bound Azure Service Bus endpoint. "
            + "ProcessSessionMessageEventArgs is only available for handlers receiving from a session-required queue.");
    }
}
