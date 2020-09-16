using System;
using System.Globalization;

namespace HotChocolate.Language
{
    internal static class ThrowHelper
    {
        public static SyntaxException InvalidRequestStructure(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                "Expected `{` or `[` as first syntax token.");

        public static SyntaxException NoIdAndNoQuery(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                "The request is missing the `query` property and the `id` property.");

        public static SyntaxException QueryMustBeStringOrNull(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                "The query field must be a string or null.");

        public static SyntaxException UnexpectedProperty(
            Utf8GraphQLReader reader,
            ReadOnlySpan<byte> fieldName) =>
            new SyntaxException(
                reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected request property name `{0}` found.",
                    Utf8GraphQLReader.GetString(fieldName, false)));

        public static SyntaxException ExpectedObjectOrNull(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected an object or a null-token, " +
                    "but found a {0}-token with value `{1}`.",
                    reader.Kind.ToString(),
                    reader.GetString()));

        public static SyntaxException ExpectedStringOrNull(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected a string-token or a null-token, " +
                    "but found a {0}-token with value `{1}`.",
                    reader.Kind.ToString(),
                    reader.GetString()));

        public static SyntaxException UnexpectedToken(Utf8GraphQLReader reader) =>
            new SyntaxException(
                reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected token found `{0}` " +
                    "while expecting a scalar value.",
                    reader.Kind));
    }
}
