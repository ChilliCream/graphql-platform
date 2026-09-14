namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Provides the short operation document hash that generated source schema operation names carry.
/// </summary>
internal static class OperationShortHash
{
    /// <summary>
    /// Replaces every character of a short hash that a GraphQL name cannot hold with an underscore.
    /// Digits are kept in every position: a short hash is embedded in a name, never used as one.
    /// </summary>
    /// <param name="shortHash">
    /// The short hash of the operation document.
    /// </param>
    /// <returns>
    /// <paramref name="shortHash"/> itself when it holds GraphQL name characters only.
    /// </returns>
    public static string ToNameSafe(string shortHash)
    {
        var index = IndexOfNonNameCharacter(shortHash);

        if (index < 0)
        {
            return shortHash;
        }

        var buffer = shortHash.ToCharArray();

        for (var i = index; i < buffer.Length; i++)
        {
            if (!IsNameCharacter(buffer[i]))
            {
                buffer[i] = '_';
            }
        }

        return new string(buffer);
    }

    private static int IndexOfNonNameCharacter(string shortHash)
    {
        for (var i = 0; i < shortHash.Length; i++)
        {
            if (!IsNameCharacter(shortHash[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsNameCharacter(char value)
        => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';
}
