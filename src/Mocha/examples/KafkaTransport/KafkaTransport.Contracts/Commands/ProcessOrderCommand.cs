namespace KafkaTransport.Contracts.Commands;

public sealed class ProcessOrderCommand
{
    public required Guid OrderId { get; init; }

    public required string Action { get; init; }

    public required DateTimeOffset RequestedAt { get; init; }
}
