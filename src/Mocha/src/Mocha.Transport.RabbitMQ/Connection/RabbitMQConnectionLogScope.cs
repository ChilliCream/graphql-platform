namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Logging scope state with the values that identify one RabbitMQ connection of a transport.
/// </summary>
internal sealed class RabbitMQConnectionLogScope(
    string serviceName,
    Guid instanceId,
    string transport,
    string connectionName,
    string connectionRole,
    string brokerHost,
    int brokerPort,
    string virtualHost)
    : LogScope
{
    public override int Count => 8;

    public override KeyValuePair<string, object?> this[int index]
        => index switch
        {
            0 => new("ServiceName", serviceName),
            1 => new("InstanceId", instanceId),
            2 => new("Transport", transport),
            3 => new("ConnectionName", connectionName),
            4 => new("ConnectionRole", connectionRole),
            5 => new("BrokerHost", brokerHost),
            6 => new("BrokerPort", brokerPort),
            7 => new("VirtualHost", virtualHost),
            _ => throw Mocha.ThrowHelper.LogScopeIndexOutOfRange(index)
        };
}
