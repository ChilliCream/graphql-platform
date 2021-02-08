namespace StrawberryShake
{
    internal static class ThrowHelper
    {
        internal static GraphQLClientException InputFormatter_InvalidType(
            string runtimeType,
            string scalarType) =>
            new(new ClientError(
                $"The runtime value is expected to be {runtimeType} for {scalarType}."));
    }
}
