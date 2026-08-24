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

        return Resolve(processPath, arg0);
    }

    internal static LaunchDescriptor Resolve(string processPath, string? arg0)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);

        // A framework-dependent .NET global tool runs through its `nitro`
        // shim while argv[0] points at the package's internal .store DLL.
        // The shim already selects that DLL, so appending argv[0] would pass
        // it to Nitro as a user argument and break every installed hook.
        // Store the stable command name instead; it also survives tool
        // updates that replace the versioned .store directory.
        if (string.Equals(processName, "nitro", StringComparison.OrdinalIgnoreCase)
            && arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new LaunchDescriptor("nitro", []);
        }

        // Framework-dependent invocation ("dotnet nitro.dll ..."): the
        // running process is the dotnet muxer, and argv[0] is the managed
        // assembly path, distinct from the muxer executable itself.
        if (arg0?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true
            && string.Equals(processName, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return new LaunchDescriptor(processPath, [Path.GetFullPath(arg0)]);
        }

        return new LaunchDescriptor(processPath, []);
    }
}
