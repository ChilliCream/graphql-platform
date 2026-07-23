using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Retries one dispatch after re-provisioning an entity deleted by the broker.
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
        var lease = clientManager.AcquireSender(entityPath);
        var senderEntry = lease.Entry;
        var retry = false;

        try
        {
            await operation(lease.Sender, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            retry = true;
        }
        finally
        {
            lease.Dispose();
        }

        if (retry)
        {
            endpoint.InvalidateProvisioning();
            await clientManager.InvalidateSenderAsync(entityPath, senderEntry);
            await endpoint.EnsureProvisionedAsync(cancellationToken);

            using var retryLease = clientManager.AcquireSender(entityPath);
            await operation(retryLease.Sender, cancellationToken);
        }
    }

    public static async ValueTask<TResult> ExecuteAsync<TResult>(
        AzureServiceBusClientManager clientManager,
        AzureServiceBusDispatchEndpoint endpoint,
        string entityPath,
        Func<ServiceBusSender, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var lease = clientManager.AcquireSender(entityPath);
        var senderEntry = lease.Entry;

        try
        {
            return await operation(lease.Sender, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            // The lease is released in finally before the retired sender is awaited below.
        }
        finally
        {
            lease.Dispose();
        }

        endpoint.InvalidateProvisioning();
        await clientManager.InvalidateSenderAsync(entityPath, senderEntry);
        await endpoint.EnsureProvisionedAsync(cancellationToken);

        using var retryLease = clientManager.AcquireSender(entityPath);
        return await operation(retryLease.Sender, cancellationToken);
    }
}
