namespace HotChocolate.Fusion;

public static class StringUtilities
{
    public static string ToConstantCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Allocate enough space for potential underscores.
        Span<char> span = stackalloc char[input.Length * 2];
        var previousChar = '\0';
        var charCount = 0;

        for (var i = 0; i < input.Length; i++)
        {
            var currentChar = input[i];

            // Ignore consecutive underscores.
            if (currentChar == '_' && previousChar == '_')
            {
                continue;
            }

            if (char.IsUpper(currentChar))
            {
                if (
                    // Lower followed by upper, f.e. "Fo[oB]ar" -> "FOO_BAR".
                    char.IsLower(previousChar)
                    // Two upper followed by one lower, f.e. "I[PAd]dress" -> "IP_ADDRESS".
                    || (
                        i != input.Length - 1 // Not the last character.
                        && char.IsUpper(previousChar)
                        && char.IsUpper(currentChar)
                        && char.IsLower(input[i + 1])))
                {
                    span[charCount++] = '_';
                }
            }

            span[charCount++] = char.ToUpper(currentChar);
            previousChar = currentChar;
        }

        return new string(span[..charCount]);
    }
}
