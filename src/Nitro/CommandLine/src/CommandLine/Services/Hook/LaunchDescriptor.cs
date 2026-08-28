using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// How this running <c>nitro</c> process should be launched again: an
/// executable plus the argv prefix (if any) needed to reach the
/// <c>nitro</c> entry point through it. A .NET global-tool shim is recorded
/// as the portable command name <c>nitro</c> with no prefix. A direct
/// <c>dotnet nitro.dll ...</c> development invocation needs the managed
/// assembly path as the prefix because the <c>dotnet</c> muxer alone would
/// launch nothing.
/// </summary>
internal sealed record LaunchDescriptor(string Executable, IReadOnlyList<string> ArgumentPrefix)
{
    /// <summary>
    /// Builds a shell command line invoking this descriptor followed by
    /// <paramref name="argv"/>, each token quoted only where needed so the
    /// literal words (in particular the ownership marker
    /// <see cref="ClaudeHooksTemplate.CommandMarker"/>) stay recognizable as
    /// plain substrings of the result.
    /// </summary>
    public string BuildCommandLine(IReadOnlyList<string> argv)
    {
        var tokens = new List<string>(1 + ArgumentPrefix.Count + argv.Count) { Executable };
        tokens.AddRange(ArgumentPrefix);
        tokens.AddRange(argv);

        return string.Join(' ', tokens.Select(ShellQuote));
    }

    /// <summary>
    /// POSIX-shell single-quote escaping, applied only to tokens containing
    /// a character a shell would otherwise treat specially. Claude Code runs
    /// installed hook commands through a shell, so an unquoted path
    /// containing a space would split into two arguments.
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
                // Close the quote, emit an escaped single quote outside it,
                // then reopen: POSIX shells have no escape character inside
                // single quotes.
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
