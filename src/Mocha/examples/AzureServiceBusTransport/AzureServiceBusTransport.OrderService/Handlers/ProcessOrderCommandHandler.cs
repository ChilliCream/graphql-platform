using Mocha;
using AzureServiceBusTransport.Contracts.Commands;

namespace AzureServiceBusTransport.OrderService.Handlers;

public sealed class ProcessOrderCommandHandler(ILogger<ProcessOrderCommandHandler> logger)
    : IEventHandler<ProcessOrderCommand>
{
    public ValueTask HandleAsync(ProcessOrderCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing command for order {OrderId}: {Action}",
            message.OrderId,
            message.Action);

        return ValueTask.CompletedTask;
    }
}
