using System.Text;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Narrow, line-based editing for the top-level <c>notify = [...]</c> key in a Codex
/// CLI <c>config.toml</c>. Every other line, including other top-level keys and
/// tables, is left untouched; a <c>notify</c> key inside a table is unrelated and
/// never touched. Only the single-line, double-quoted string array form this
/// installer writes is understood; anything else under a top-level <c>notify</c>
/// key throws <see cref="ExitException"/>.
/// </summary>
internal static partial class CodexConfigTomlNotifyEditor
{
    [GeneratedRegex(@"^\s*notify\s*=")]
    private static partial Regex NotifyKeyPresence();

    [GeneratedRegex(@"^\s*\[")]
    private static partial Regex SectionHeader();

    public sealed record InstallResult(
        string ConfigToml, HookInstallOutcome Outcome, IReadOnlyList<string>? NewPriorForeign);

    public sealed record UninstallResult(string ConfigToml, HookUninstallOutcome Outcome);

    /// <summary>
    /// Installs <paramref name="ourArgv"/> as the top-level <c>notify</c> value.
    /// <paramref name="recordedOurArgv"/> is the sidecar's own last-installed argv,
    /// or null on a first install. Returns the <c>NewPriorForeign</c> the caller
    /// should persist: null when nothing foreign exists, <paramref name="recordedPriorForeign"/>
    /// carried forward when the on-disk value was our own stale entry, or the
    /// freshly captured value when the on-disk value belongs to a different program.
    /// </summary>
    public static InstallResult Install(
        string? existingConfigToml,
        IReadOnlyList<string> ourArgv,
        IReadOnlyList<string>? recordedOurArgv,
        IReadOnlyList<string>? recordedPriorForeign)
    {
        var lines = SplitLines(existingConfigToml);
        var (lineIndex, insertBeforeIndex) = FindTopLevelNotifyLine(lines);

        if (lineIndex < 0)
        {
            lines.Insert(insertBeforeIndex, BuildNotifyLine(ourArgv));

            return new InstallResult(JoinLines(lines), HookInstallOutcome.Installed, null);
        }

        var existingArgv = ParseNotifyArray(lines[lineIndex]);

        if (ArgvEquals(existingArgv, ourArgv))
        {
            return new InstallResult(JoinLines(lines), HookInstallOutcome.Unchanged, recordedPriorForeign);
        }

        var isOurStaleEntry = recordedOurArgv is not null && ArgvEquals(existingArgv, recordedOurArgv);

        lines[lineIndex] = BuildNotifyLine(ourArgv);

        return new InstallResult(
            JoinLines(lines),
            HookInstallOutcome.Updated,
            isOurStaleEntry ? recordedPriorForeign : existingArgv);
    }

    /// <summary>
    /// Returns Missing when no top-level <c>notify</c> key exists, Installed when it
    /// matches <paramref name="ourArgv"/> exactly, or Outdated when it holds a
    /// different value. Never mutates.
    /// </summary>
    public static HookStatusOutcome Status(string? existingConfigToml, IReadOnlyList<string> ourArgv)
    {
        var lines = SplitLines(existingConfigToml);
        var (lineIndex, _) = FindTopLevelNotifyLine(lines);

        if (lineIndex < 0)
        {
            return HookStatusOutcome.Missing;
        }

        var existingArgv = ParseNotifyArray(lines[lineIndex]);

        return ArgvEquals(existingArgv, ourArgv) ? HookStatusOutcome.Installed : HookStatusOutcome.Outdated;
    }

    /// <summary>
    /// Restores <paramref name="recordedPriorForeign"/> verbatim, or removes the key
    /// when it is null, but only when the on-disk value is still exactly
    /// <paramref name="ourArgv"/>. A foreign edit since install is left untouched.
    /// </summary>
    public static UninstallResult Uninstall(
        string? existingConfigToml,
        IReadOnlyList<string> ourArgv,
        IReadOnlyList<string>? recordedPriorForeign)
    {
        var lines = SplitLines(existingConfigToml);
        var (lineIndex, _) = FindTopLevelNotifyLine(lines);

        if (lineIndex < 0)
        {
            return new UninstallResult(JoinLines(lines), HookUninstallOutcome.NotPresent);
        }

        var existingArgv = ParseNotifyArray(lines[lineIndex]);

        if (!ArgvEquals(existingArgv, ourArgv))
        {
            // Not ours (never installed, or edited since): leave it exactly
            // as found.
            return new UninstallResult(JoinLines(lines), HookUninstallOutcome.NotPresent);
        }

        if (recordedPriorForeign is null)
        {
            lines.RemoveAt(lineIndex);
        }
        else
        {
            lines[lineIndex] = BuildNotifyLine(recordedPriorForeign);
        }

        return new UninstallResult(JoinLines(lines), HookUninstallOutcome.Removed);
    }

    /// <summary>
    /// Finds the line index of a top-level <c>notify = ...</c> assignment among
    /// lines before the first section header, or -1 when none exists. The second
    /// tuple element is where a new <c>notify</c> line should be inserted when none
    /// is found.
    /// </summary>
    private static (int LineIndex, int InsertBeforeIndex) FindTopLevelNotifyLine(List<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (SectionHeader().IsMatch(lines[i]))
            {
                return (-1, i);
            }

            if (NotifyKeyPresence().IsMatch(lines[i]))
            {
                return (i, i);
            }
        }

        return (-1, lines.Count);
    }

    /// <summary>
    /// Parses a line already known to match <see cref="NotifyKeyPresence"/> into its
    /// array-of-strings value. Throws <see cref="ExitException"/> for anything other
    /// than a single-line, double-quoted string array with nothing trailing the
    /// closing bracket.
    /// </summary>
    private static IReadOnlyList<string> ParseNotifyArray(string line)
    {
        var equalsIndex = line.IndexOf('=');
        var value = line[(equalsIndex + 1)..].Trim();

        if (value.Length < 2 || value[0] != '[' || value[^1] != ']')
        {
            throw new ExitException(
                "config.toml's top-level 'notify' value is not a single-line array this installer can safely "
                + "parse (a multi-line array, an inline comment, or a non-string-array value). Resolve it by "
                + "hand, then re-run install.");
        }

        var inner = value[1..^1].Trim();

        if (inner.Length == 0)
        {
            return [];
        }

        var result = new List<string>();
        var i = 0;

        while (i < inner.Length)
        {
            while (i < inner.Length && (inner[i] == ',' || char.IsWhiteSpace(inner[i])))
            {
                i++;
            }

            if (i >= inner.Length)
            {
                break;
            }

            if (inner[i] != '"')
            {
                throw new ExitException(
                    "config.toml's top-level 'notify' array contains a value this installer does not "
                    + "understand (only double-quoted strings are supported). Resolve it by hand, then "
                    + "re-run install.");
            }

            var builder = new StringBuilder();
            i++;

            while (i < inner.Length && inner[i] != '"')
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    var escaped = inner[i + 1];

                    builder.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        _ => throw new ExitException(
                            "config.toml's top-level 'notify' array uses an escape sequence this installer "
                            + "does not understand. Resolve it by hand, then re-run install.")
                    });

                    i += 2;
                }
                else
                {
                    builder.Append(inner[i]);
                    i++;
                }
            }

            if (i >= inner.Length)
            {
                throw new ExitException(
                    "config.toml's top-level 'notify' array has an unterminated string. Resolve it by hand, "
                    + "then re-run install.");
            }

            i++; // closing quote
            result.Add(builder.ToString());
        }

        return result;
    }

    private static string BuildNotifyLine(IReadOnlyList<string> argv)
        => $"notify = [{string.Join(", ", argv.Select(QuoteTomlString))}]";

    private static string QuoteTomlString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        builder.Append('"');

        return builder.ToString();
    }

    private static bool ArgvEquals(IReadOnlyList<string> a, IReadOnlyList<string> b)
        => a.SequenceEqual(b, StringComparer.Ordinal);

    /// <summary>
    /// Normalizes CRLF to LF on read. A CRLF-authored config.toml round-trips as LF
    /// even through a no-op call.
    /// </summary>
    private static List<string> SplitLines(string? text)
        => string.IsNullOrEmpty(text)
            ? []
            : text.Replace("\r\n", "\n").Split('\n').ToList();

    private static string JoinLines(List<string> lines) => string.Join('\n', lines);
}
