using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQConnectionExtensions
{
    /// <summary>
    /// Gets whether the broker of the connection reports a 3.x version in its server properties.
    /// </summary>
    public static bool IsRabbitMQ3(this IConnection connection)
    {
        if (connection.ServerProperties is not { } properties
            || !properties.TryGetValue("version", out var version))
        {
            return false;
        }

        // The client returns the version as UTF-8 bytes. A string is accepted for other implementations.
        return version switch
        {
            byte[] bytes => bytes.AsSpan().StartsWith("3."u8),
            string text => text.StartsWith("3.", StringComparison.Ordinal),
            _ => false
        };
    }
}
