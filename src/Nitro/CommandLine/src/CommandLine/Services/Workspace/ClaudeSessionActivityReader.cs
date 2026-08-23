using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a live Claude Code harness session's idle/busy activity straight
/// from <c>~/.claude/sessions/&lt;pid&gt;.json</c> at display time. Matches
/// the plan's rule that Claude activity is a read-through, never stored:
/// there is no column for it in <c>agent_sessions</c>, and every call
/// re-reads the file fresh instead of caching. Used by <c>agent list</c> and
/// the TUI Agents tab, both of which only ask for activity on sessions this
/// Nitro instance can actually see the process for (an
/// <see cref="AgentSessionState.Online"/> claude-code row).
/// </summary>
internal interface IClaudeSessionActivityReader
{
    /// <summary>
    /// Returns the session file's <c>status</c> field ("idle" or "busy",
    /// whatever Claude Code currently writes) for <paramref name="pid"/>,
    /// but only when the file's own <c>sessionId</c> still matches
    /// <paramref name="sessionId"/> - a pid can be reused by an unrelated
    /// process, or the file can already belong to a newer generation than
    /// the one the caller is asking about. Returns null on any mismatch,
    /// missing file, or parse failure: this is a best-effort display
    /// enrichment, never a source of truth.
    /// </summary>
    string? GetStatus(int pid, string sessionId);
}

internal sealed class ClaudeSessionActivityReader(Func<int, string?>? sessionFileReader = null)
    : IClaudeSessionActivityReader
{
    private readonly Func<int, string?> _sessionFileReader = sessionFileReader ?? ReadSessionFile;

    public string? GetStatus(int pid, string sessionId)
    {
        var json = _sessionFileReader(pid);

        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("sessionId", out var sessionIdElement)
                || sessionIdElement.GetString() != sessionId)
            {
                return null;
            }

            return root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadSessionFile(int pid)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude",
                "sessions",
                $"{pid}.json");

            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
