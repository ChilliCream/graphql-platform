using System.Text;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Narrow, line-based editing for a single top-level key in a Codex CLI
/// <c>config.toml</c>: <c>notify = [...]</c>. Deliberately NOT a general
/// TOML parser/writer (no such library is available to this project, and
/// hand-rolling one is out of scope): every OTHER line, including every
/// other top-level key and every table, round-trips completely untouched -
/// only the exact line(s) holding a TOP-LEVEL <c>notify</c> assignment are
/// read or replaced. "Top-level" means before the file's first
/// <c>[table]</c>/<c>[[array-of-tables]]</c> header line; a <c>notify</c> key
/// inside some other table is a different, unrelated key and is never
/// touched. Only the single-line, double-quoted-basic-string-array form this
/// installer itself always writes is understood - anything else found under
/// a top-level <c>notify</c> key (a multi-line array, single-quoted literal
/// strings, an inline table, a trailing comment) is refused with an
/// <see cref="ExitException"/> rather than risked: the safe failure mode is
/// "ask the operator to resolve it by hand", never "silently write something
/// that might not mean what the original author intended".
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
    /// Installs <paramref name="ourArgv"/> as the top-level <c>notify</c>
    /// value. <paramref name="recordedOurArgv"/> is what THIS sidecar
    /// recorded as our own last-installed argv (null on a first-ever
    /// install); <paramref name="recordedPriorForeign"/> is what the sidecar
    /// already has recorded as the foreign value from before we ever wrapped
    /// it. Returns the <c>NewPriorForeign</c> the caller should persist:
    /// null when there was and is nothing foreign, unchanged
    /// (<paramref name="recordedPriorForeign"/> carried forward) when the
    /// on-disk value was our OWN stale entry (a reinstall after the launch
    /// descriptor changed), or freshly captured when the on-disk value was
    /// something else entirely (a genuine foreign program being wrapped for
    /// the first time).
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
    /// Missing (no top-level <c>notify</c> key at all), Installed (matches
    /// <paramref name="ourArgv"/> exactly), or Outdated (a top-level
    /// <c>notify</c> key exists with some other value - a stale launch
    /// descriptor and an unrelated foreign program look identical here by
    /// design, same as <see cref="ClaudeHooksEditor.Status"/>). Never
    /// mutates.
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
    /// Restores <paramref name="recordedPriorForeign"/> verbatim (or removes
    /// the key entirely when it is null - nothing was there before us), but
    /// ONLY when the on-disk value is still exactly <paramref name="ourArgv"/>:
    /// a foreign edit since our install (the value is neither ours nor a
    /// value we ever recorded) is left completely untouched, same
    /// non-clobbering principle as <see cref="ClaudeHooksEditor.Uninstall"/>.
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
    /// Finds the line index of a top-level <c>notify = ...</c> assignment
    /// (searched only among lines before the first section header), or -1
    /// when none exists. The second tuple element is always where a NEW
    /// <c>notify</c> line should be inserted when none is found: immediately
    /// before the first section header, or at the end of the file when there
    /// is none - either way, guaranteed to land before any table and
    /// therefore at the top level.
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
    /// Parses a line already known to match <see cref="NotifyKeyPresence"/>
    /// into its array-of-strings value. Throws <see cref="ExitException"/>
    /// for anything other than a single-line <c>["...", "...", ...]</c> array
    /// of double-quoted basic strings with nothing trailing after the
    /// closing bracket (no inline comment, no multi-line continuation) -
    /// the only form this installer itself ever writes, and the only form it
    /// can confidently interpret.
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
    /// Normalizes CRLF to LF on read. Known, accepted limitation: a
    /// CRLF-authored config.toml round-trips as LF even through a no-op
    /// Status/Unchanged-Install call, which the installer service's
    /// hash-compare guard sees as a real change. Every config this installer
    /// targets lives under a Linux-first tool's own home directory, where
    /// LF is already the norm.
    /// </summary>
    private static List<string> SplitLines(string? text)
        => string.IsNullOrEmpty(text)
            ? []
            : text.Replace("\r\n", "\n").Split('\n').ToList();

    private static string JoinLines(List<string> lines) => string.Join('\n', lines);
}
