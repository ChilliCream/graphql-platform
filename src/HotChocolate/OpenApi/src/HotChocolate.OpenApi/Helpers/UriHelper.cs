using System.Text;

namespace HotChocolate.OpenApi.Helpers;

// Based on code from https://stackoverflow.com/a/853466/221528.
internal static class UriHelper
{
    /// <summary>
    /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
    /// </summary>
    private static readonly char[] UriRfc3986CharsToEscape = ['!', '*', '\'', '(', ')'];

    /// <summary>
    /// Escapes a string according to the URI data string rules given in RFC 3986.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value.</returns>
    public static string EscapeDataStringRfc3986(string value)
    {
        // Start with RFC 2396 escaping by calling the .NET method to do the work.
        // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
        // If it does, the escaping we do that follows it will be a no-op since the
        // characters we search for to replace can't possibly exist in the string.
        var escaped = new StringBuilder(Uri.EscapeDataString(value));

        // Upgrade the escaping to RFC 3986, if necessary.
        foreach (var character in UriRfc3986CharsToEscape)
        {
            escaped.Replace(character.ToString(), Uri.HexEscape(character));
        }

        // Return the fully-RFC3986-escaped string.
        return escaped.ToString();
    }
}
