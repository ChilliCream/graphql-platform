using StrawberryShake.Properties;

namespace StrawberryShake.Transport.InMemory;

internal static class ThrowHelper
{
    public static ArgumentException Argument_IsNullOrEmpty(string argumentName) =>
        new(string.Format(Resources.Argument_IsNullOrEmpty, argumentName), argumentName);

    public static GraphQLClientException InMemoryClient_NoExecutorConfigured(string name) =>
        new(string.Format(Resources.InMemoryClient_ExecuteAsync_NoExecutorFound, name));
}
