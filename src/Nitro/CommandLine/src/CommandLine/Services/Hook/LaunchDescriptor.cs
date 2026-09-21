using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// How this running <c>nitro</c> process should be launched again: an
/// executable plus the argv prefix (if any) needed to reach the
/// <c>nitro</c> entry point through it.
/// </summary>
internal sealed record LaunchDescriptor(string Executable, IReadOnlyList<string> ArgumentPrefix)
{
    /// <summary>
    /// Builds a POSIX shell command from the executable, argument prefix, and
    /// <paramref name="argv"/>, quoting tokens that require escaping.
    /// </summary>
    public string BuildCommandLine(IReadOnlyList<string> argv)
    {
        var tokens = new List<string>(1 + ArgumentPrefix.Count + argv.Count) { Executable };
        tokens.AddRange(ArgumentPrefix);
        tokens.AddRange(argv);

        return string.Join(' ', tokens.Select(ShellQuote));
    }

    /// <summary>
    /// Quotes a token for use as one POSIX shell argument, including an empty token.
    /// </summary>
    internal static string ShellQuote(string token)
    {
        if (token.Length > 0 && token.All(IsSafeUnquoted))
        {
            return token;
        }

        var builder = new StringBuilder(token.Length + 2);
        builder.Append('\'');

        foreach (var ch in token)
        {
            if (ch == '\'')
            {
                // Close the quoted segment, emit an escaped quote, then reopen the segment.
                builder.Append("'\\''");
            }
            else
            {
                builder.Append(ch);
            }
        }

        builder.Append('\'');

        return builder.ToString();
    }

    private static bool IsSafeUnquoted(char ch)
        => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.' or '/' or ':' or '+' or ',';
}
