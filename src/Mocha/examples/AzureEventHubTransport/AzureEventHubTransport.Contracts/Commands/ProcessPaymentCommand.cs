using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Commands;

/// <summary>
/// Sent by the order fulfillment saga to the order service to process payment.
/// </summary>
public sealed class ProcessPaymentCommand : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required decimal Amount { get; init; }

    public required string CustomerEmail { get; init; }

    public Guid? CorrelationId { get; init; }
}
