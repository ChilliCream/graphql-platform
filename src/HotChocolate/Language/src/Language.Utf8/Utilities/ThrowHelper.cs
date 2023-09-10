using System;
using System.Globalization;
using HotChocolate.Language.Properties;
using static HotChocolate.Language.Properties.LangUtf8Resources;
using static HotChocolate.Language.Utf8GraphQLReader;

namespace HotChocolate.Language;

internal static class ThrowHelper
{
    public static SyntaxException InvalidRequestStructure(Utf8GraphQLReader reader)
        => new(reader, ThrowHelper_InvalidRequestStructure);

    public static SyntaxException NoIdAndNoQuery(Utf8GraphQLReader reader)
        => new(reader, ThrowHelper_NoIdAndNoQuery);

    public static SyntaxException QueryMustBeStringOrNull(Utf8GraphQLReader reader)
        => new(reader, ThrowHelper_QueryMustBeStringOrNull);

    public static SyntaxException UnexpectedProperty(
        Utf8GraphQLReader reader,
        ReadOnlySpan<byte> fieldName)
        => new(
            reader,
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_UnexpectedProperty,
                GetString(fieldName, false)));

    public static SyntaxException ExpectedObjectOrNull(Utf8GraphQLReader reader)
        => new(
            reader,
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_ExpectedObjectOrNull,
                reader.Kind.ToString(),
                reader.GetString()));

    public static SyntaxException ExpectedStringOrNull(Utf8GraphQLReader reader)
        => new(
            reader,
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_ExpectedStringOrNull,
                reader.Kind.ToString(),
                reader.GetString()));

    public static SyntaxException UnexpectedToken(Utf8GraphQLReader reader)
        => new(
            reader,
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_UnexpectedToken,
                reader.Kind));

    public static SyntaxException Reader_UnexpectedDigitAfterDot(Utf8GraphQLReader reader)
        => new(reader, LangUtf8Resources.Reader_UnexpectedDigitAfterDot);

    public static SyntaxException Reader_InvalidToken(
        Utf8GraphQLReader reader,
        TokenKind expected)
        => new(
            reader,
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Reader_InvalidToken,
                expected));
}
