using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// A stub <see cref="IRabbitMQConnectionProvider"/> that satisfies initialization requirements
/// for semi-integration tests that build a runtime but never start connections.
/// </summary>
internal sealed class StubConnectionProvider : IRabbitMQConnectionProvider
{
    public string Host => "localhost";
    public string VirtualHost => "/";
    public int Port => 5672;

    public ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("StubConnectionProvider does not create real connections.");
    }
}
