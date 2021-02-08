using System.Collections.Generic;

namespace StrawberryShake
{
    internal static class ThrowHelper
    {
        internal static GraphQLClientException InputFormatter_InvalidType(
            string runtimeType,
            string scalarType) =>
            new(new ClientError(
                $"The runtime value is expected to be {runtimeType} for {scalarType}."));

        internal static GraphQLClientException DateTimeSerializer_InvalidFormat(
            string serializedValue) =>
            new(new ClientError(
                "The serialized format for DateTime must be `yyyy-MM-ddTHH\\:mm\\:ss.fffzzz`. " +
                "For more information read: `https://www.graphql-scalars.com/date-time`.",
                extensions: new Dictionary<string, object?>
                {
                    { "serializedValue", serializedValue }
                }));

        internal static GraphQLClientException DateSerializer_InvalidFormat(
            string serializedValue) =>
            new(new ClientError(
                "The serialized format for Date must be `yyyy-MM-dd`.",
                extensions: new Dictionary<string, object?>
                {
                    { "serializedValue", serializedValue }
                }));

    }
}
