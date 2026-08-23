using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// How this running <c>nitro</c> process was launched: an absolute
/// executable path plus the argv prefix (if any) needed to reach the
/// <c>nitro</c> entry point through it. A self-contained build or a
/// global-tool shim needs no prefix; <c>dotnet nitro.dll ...</c> needs the
/// managed assembly path as the prefix, because <see cref="Executable"/>
/// alone (the <c>dotnet</c> muxer) would launch nothing on its own. Hook
/// entries embed this descriptor instead of a bare <c>nitro</c> command
/// name: a bare name depends on the harness's own <c>PATH</c> at hook-run
/// time, which is not guaranteed to match this install's launch mode.
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

/// <summary>
/// Resolves <see cref="LaunchDescriptor"/> for the currently running
/// process.
/// </summary>
internal interface ILaunchDescriptorResolver
{
    LaunchDescriptor Resolve();
}

internal sealed class LaunchDescriptorResolver : ILaunchDescriptorResolver
{
    public LaunchDescriptor Resolve()
    {
        var processPath = Environment.ProcessPath
            ?? throw new ExitException(
                "Could not resolve this process's executable path; cannot install hooks without a "
                + "reliable launch descriptor.");

        var args = Environment.GetCommandLineArgs();
        var arg0 = args.Length > 0 ? args[0] : null;

        // Framework-dependent invocation ("dotnet nitro.dll ..."): the
        // running process is the dotnet muxer, and argv[0] is the managed
        // assembly path, distinct from the muxer executable itself. A
        // global-tool shim or a self-contained/apphost build IS nitro, so
        // argv[0] resolves to the same executable as ProcessPath.
        if (arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true
            && !PathsEqual(arg0, processPath))
        {
            return new LaunchDescriptor(processPath, [Path.GetFullPath(arg0)]);
        }

        return new LaunchDescriptor(processPath, []);
    }

    private static bool PathsEqual(string a, string b)
        => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
