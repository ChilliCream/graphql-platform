using StrawberryShake.Properties;
using StrawberryShake.Serialization;

namespace StrawberryShake;

internal static class ThrowHelper
{
    internal static GraphQLClientException InputFormatter_InvalidType(
        string runtimeType,
        string scalarType) =>
        new(new ClientError(
            string.Format(
                Resources.ThrowHelper_InputFormatter_InvalidType,
                runtimeType,
                scalarType),
            code: ErrorCodes.InvalidRuntimeType));

    internal static GraphQLClientException DateTimeSerializer_InvalidFormat(
        string serializedValue) =>
        new(new ClientError(
            "The serialized format for DateTime must be `yyyy-MM-ddTHH\\:mm\\:ss.fffzzz`. " +
            "For more information read: `https://www.graphql-scalars.com/date-time`.",
            extensions: new Dictionary<string, object?>
            {
                    { "serializedValue", serializedValue },
            }));

    internal static GraphQLClientException DateSerializer_InvalidFormat(
        string serializedValue) =>
        new(new ClientError(
            "The serialized format for Date must be `yyyy-MM-dd`.",
            extensions: new Dictionary<string, object?>
            {
                    { "serializedValue", serializedValue },
            }));

    internal static GraphQLClientException UrlFormatter_CouldNotParseUri(string value) =>
        new(new ClientError(
            $"The URL serializer could not parse value{value}. Invalid format. "));

    internal static GraphQLClientException TimeSpanSerializer_CouldNotParseValue(
        string value,
        TimeSpanFormat format) =>
        new(new ClientError(
            $"The TimeSpan serializer could not parse value {value}. The provided value was " +
            $"not in format {format.ToString()}"));

    internal static GraphQLClientException TimeSpanSerializer_CouldNotFormatValue(
        TimeSpan value,
        TimeSpanFormat format) =>
        new(new ClientError(
            $"The TimeSpan serializer could not serialize value {value}. The provided value " +
            $"was not in format {format.ToString()}"));

    internal static GraphQLClientException UuidSerializer_CouldNotParse(string guid) =>
        new(new ClientError(
            $"The Guid serializer could not parse value {guid}. Invalid format."));

    internal static NotSupportedException UploadScalar_OutputNotSupported() =>
        new("The upload scalar can only be used in upload");
}
