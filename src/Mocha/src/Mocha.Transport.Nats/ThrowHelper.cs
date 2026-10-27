namespace Mocha.Transport.Nats;

internal static class ThrowHelper
{
    // Topology
    public static Exception TemporaryEndpointConsumerDeclaredPersistent(string? endpointName, string consumerName)
        => new InvalidOperationException(
            $"Endpoint '{endpointName}' is marked Temporary(), but consumer '{consumerName}' was already "
            + "declared without an inactive threshold. Remove the consumer declaration or Temporary() "
            + "from the endpoint.");
}
