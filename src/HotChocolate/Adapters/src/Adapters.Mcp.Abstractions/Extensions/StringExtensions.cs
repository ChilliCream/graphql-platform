using System.Globalization;
using System.Text;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class StringExtensions
{
    extension(string text)
    {
        public string InsertSpaceBeforeUpperCase()
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            var previousChar = char.MinValue;

            foreach (var c in text)
            {
                if (char.IsUpper(c) && sb.Length != 0 && previousChar != ' ')
                {
                    sb.Append(' ');
                }

                sb.Append(c);
                previousChar = c;
            }

            return sb.ToString();
        }

        public string ToSnakeCase() => ConvertCase(text, '_');

        public string ToKebabCase() => ConvertCase(text, '-');
    }

    private static string ConvertCase(string text, char separator)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + Math.Min(2, text.Length / 5));
        UnicodeCategory? previousCategory = default;

        for (var currentIndex = 0; currentIndex < text.Length; currentIndex++)
        {
            var currentChar = text[currentIndex];

            if (currentChar == '_' || currentChar == '-')
            {
                builder.Append(separator);
                previousCategory = null;
                continue;
            }

            var currentCategory = char.GetUnicodeCategory(currentChar);

            switch (currentCategory)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                    if (previousCategory == UnicodeCategory.SpaceSeparator
                        || previousCategory == UnicodeCategory.LowercaseLetter
                        || (previousCategory != UnicodeCategory.DecimalDigitNumber
                            && previousCategory is not null
                            && currentIndex > 0
                            && currentIndex + 1 < text.Length
                            && char.IsLower(text[currentIndex + 1])))
                    {
                        builder.Append(separator);
                    }

                    currentChar = char.ToLowerInvariant(currentChar);
                    break;

                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (previousCategory == UnicodeCategory.SpaceSeparator)
                    {
                        builder.Append(separator);
                    }
                    break;

                default:
                    // Treat punctuation/symbols as a separator boundary: skip the character but
                    // ensure the next letter/digit gets a separator prepended.
                    if (previousCategory is not null)
                    {
                        previousCategory = UnicodeCategory.SpaceSeparator;
                    }
                    continue;
            }

            builder.Append(currentChar);
            previousCategory = currentCategory;
        }

        return builder.ToString();
    }
}
