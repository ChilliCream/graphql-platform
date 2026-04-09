using Mocha;
using AzureEventHubTransport.Contracts.Commands;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.OrderService.Handlers;

/// <summary>
/// Bus event handler — processes the payment for an order.
/// The saga sends <see cref="ProcessPaymentCommand"/>; this handler processes it
/// and publishes <see cref="PaymentProcessedEvent"/> which the saga correlates.
/// </summary>
public sealed class ProcessPaymentCommandHandler(
    IMessageBus messageBus,
    ILogger<ProcessPaymentCommandHandler> logger)
    : IEventHandler<ProcessPaymentCommand>
{
    public async ValueTask HandleAsync(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing payment of ${Amount} for order {OrderId}",
            command.Amount, command.OrderId);

        // Simulate payment gateway latency
        await Task.Delay(300, cancellationToken);

        var paymentId = Guid.NewGuid();

        await messageBus.PublishAsync(
            new PaymentProcessedEvent
            {
                OrderId = command.OrderId,
                PaymentId = paymentId,
                Success = true,
                ProcessedAt = DateTimeOffset.UtcNow,
                CorrelationId = command.CorrelationId
            },
            cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} completed for order {OrderId}",
            paymentId, command.OrderId);
    }
}
