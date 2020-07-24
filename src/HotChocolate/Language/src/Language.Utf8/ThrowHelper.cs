using System;
using System.Globalization;

namespace HotChocolate.Language
{
    internal static class ThrowHelper
    {
        // TODO : resources
        public static void InvalidRequestStructure(Utf8GraphQLReader reader) =>
            throw new SyntaxException(
                reader,
                "Expected `{` or `[` as first syntax token.");

        // TODO : resources
        public static void NoIdAndNoQuery(Utf8GraphQLReader reader) =>
            throw new SyntaxException(
                reader,
                "The request is missing the `query` property and the `id` property.");

        // TODO : resources
        public static void QueryMustBeStringOrNull(Utf8GraphQLReader reader) =>
            throw new SyntaxException(
                reader,
                "The query field must be a string or null.");

        // TODO : resources
        public static void UnexpectedProperty(
            Utf8GraphQLReader reader,
            ReadOnlySpan<byte> fieldName) =>
            throw new SyntaxException(
                reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected request property name `{0}` found.",
                    Utf8GraphQLReader.GetString(fieldName, false)));
    }
}
