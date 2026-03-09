using System.Buffers;

namespace HotChocolate.Fusion;

public static class StringUtilities
{
    private const int StackallocThreshold = 256;

    public static string ToConstantCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var maxLength = checked(input.Length * 2);
        char[]? rented = null;
        var span = maxLength <= StackallocThreshold
            ? stackalloc char[maxLength]
            : rented = ArrayPool<char>.Shared.Rent(maxLength);

        try
        {
            var previousChar = '\0';
            var charCount = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var currentChar = input[i];

                if (currentChar is '-' or '.')
                {
                    currentChar = '_';
                }

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
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}
