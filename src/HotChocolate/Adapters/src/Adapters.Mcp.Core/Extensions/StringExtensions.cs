using System.Globalization;

namespace System.Text;

internal static class StringExtensions
{
    extension(string text)
    {
        public string InsertSpaceBeforeUpperCase()
        {
            var sb = new StringBuilder();
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

        public string ToSnakeCase()
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

                if (currentChar == '_')
                {
                    builder.Append('_');
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
                            builder.Append('_');
                        }

                        currentChar = char.ToLower(currentChar, CultureInfo.InvariantCulture);
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        if (previousCategory == UnicodeCategory.SpaceSeparator)
                        {
                            builder.Append('_');
                        }
                        break;

                    default:
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

        public string ToKebabCase()
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var result = new StringBuilder();
            var previousCharacterIsSeparator = true;

            for (var i = 0; i < text.Length; i++)
            {
                var currentChar = text[i];

                if (char.IsUpper(currentChar) || char.IsDigit(currentChar))
                {
                    if (!previousCharacterIsSeparator
                        && i > 0
                        && (char.IsLower(text[i - 1])
                            || (i < text.Length - 1 && char.IsLower(text[i + 1]))))
                    {
                        result.Append('-');
                    }

                    result.Append(char.ToLowerInvariant(currentChar));
                    previousCharacterIsSeparator = false;
                }
                else if (char.IsLower(currentChar))
                {
                    result.Append(currentChar);
                    previousCharacterIsSeparator = false;
                }
                else if (currentChar is ' ' or '_' or '-')
                {
                    if (!previousCharacterIsSeparator)
                    {
                        result.Append('-');
                    }
                    previousCharacterIsSeparator = true;
                }
            }

            return result.ToString();
        }
    }
}
