using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ClaudeAncestorSessionResolver(
    Func<int, int?>? parentPidReader = null,
    Func<int, string?>? sessionFileReader = null) : IClaudeAncestorSessionResolver
{
    /// <summary>
    /// Bounds the ancestor walk so a pathological /proc (a cycle, or a very
    /// deep process tree) cannot loop forever.
    /// </summary>
    private const int MaxAncestorHops = 64;

    private readonly Func<int, int?> _parentPidReader = parentPidReader ?? ProcessAncestry.GetParentPid;
    private readonly Func<int, string?> _sessionFileReader = sessionFileReader ?? ReadSessionFile;

    public ClaudeAncestorSession? Resolve()
    {
        var pid = Environment.ProcessId;

        for (var hop = 0; hop < MaxAncestorHops; hop++)
        {
            var parentPid = _parentPidReader(pid);

            if (parentPid is null or <= 1)
            {
                return null;
            }

            var json = _sessionFileReader(parentPid.Value);

            if (json is not null && TryParseSession(parentPid.Value, json, out var session))
            {
                return session;
            }

            pid = parentPid.Value;
        }

        return null;
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

    private static bool TryParseSession(
        int pid, string json, [NotNullWhen(true)] out ClaudeAncestorSession? session)
    {
        session = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("sessionId", out var sessionIdElement)
                || !root.TryGetProperty("cwd", out var cwdElement)
                || !root.TryGetProperty("name", out var nameElement))
            {
                return false;
            }

            var sessionId = sessionIdElement.GetString();
            var cwd = cwdElement.GetString();
            var name = nameElement.GetString();

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(cwd) || string.IsNullOrEmpty(name))
            {
                return false;
            }

            session = new ClaudeAncestorSession(pid, sessionId, cwd, name);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
