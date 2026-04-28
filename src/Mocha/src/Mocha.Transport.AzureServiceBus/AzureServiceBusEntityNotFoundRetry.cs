using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Wraps a Service Bus operation with a single re-provision + retry on
/// <see cref="ServiceBusFailureReason.MessagingEntityNotFound"/>. The broker can delete an entity
/// out from under us (AutoDeleteOnIdle, manual deletion) — re-provision and retry once.
/// </summary>
internal static class AzureServiceBusEntityNotFoundRetry
{
    public static async ValueTask ExecuteAsync(
        AzureServiceBusClientManager clientManager,
        AzureServiceBusDispatchEndpoint endpoint,
        string entityPath,
        Func<ServiceBusSender, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        var sender = clientManager.GetSender(entityPath);

        try
        {
            await operation(sender, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            endpoint.InvalidateProvisioning();
            await endpoint.EnsureProvisionedAsync(cancellationToken);
            clientManager.InvalidateSender(entityPath);
            sender = clientManager.GetSender(entityPath);
            await operation(sender, cancellationToken);
        }
    }

    public static async ValueTask<TResult> ExecuteAsync<TResult>(
        AzureServiceBusClientManager clientManager,
        AzureServiceBusDispatchEndpoint endpoint,
        string entityPath,
        Func<ServiceBusSender, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var sender = clientManager.GetSender(entityPath);

        try
        {
            return await operation(sender, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            endpoint.InvalidateProvisioning();
            await endpoint.EnsureProvisionedAsync(cancellationToken);
            clientManager.InvalidateSender(entityPath);
            sender = clientManager.GetSender(entityPath);
            return await operation(sender, cancellationToken);
        }
    }
}
