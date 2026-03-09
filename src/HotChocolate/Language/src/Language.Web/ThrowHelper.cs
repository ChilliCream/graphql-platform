using System.Globalization;
using System.Text;
using System.Text.Json;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

internal static class ThrowHelper
{
    public static InvalidGraphQLRequestException InvalidQueryValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidQueryValue,
            tokenType));

    public static InvalidGraphQLRequestException InvalidDocumentIdValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidDocumentIdValue,
            tokenType));

    public static InvalidGraphQLRequestException InvalidDocumentIdFormat()
        => new("The operation id has an invalid format.");

    public static InvalidGraphQLRequestException InvalidOperationNameValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidOperationNameValue,
            tokenType));

    public static InvalidGraphQLRequestException InvalidOnErrorValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidOnErrorValue,
            tokenType));

    public static InvalidGraphQLRequestException InvalidVariablesValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidVariablesValue,
            tokenType));

    public static InvalidGraphQLRequestException InvalidExtensionsValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidExtensionsValue,
            tokenType));

    public static InvalidGraphQLRequestException UnknownRequestProperty(ReadOnlySpan<byte> propertyName)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_UnknownRequestProperty,
            Encoding.UTF8.GetString(propertyName)));

    public static InvalidGraphQLRequestException InvalidOperationTypeValue(JsonTokenType tokenType)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_InvalidOperationTypeStructure,
            tokenType));
}
