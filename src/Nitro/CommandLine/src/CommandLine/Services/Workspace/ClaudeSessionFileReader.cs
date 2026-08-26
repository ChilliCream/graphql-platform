using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ClaudeSessionFileReader : IClaudeSessionFileReader
{
    public ClaudeSessionFile? Find(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "sessions");

        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
            {
                if (TryRead(path, sessionId) is { } session)
                {
                    return session;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    /// <summary>
    /// The pid, which the session file writes as a number but older files
    /// wrote as a string.
    /// </summary>
    private static int? TryReadPid(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(element.GetString(), out var value) => value,
            _ => null
        };

    /// <summary>
    /// Reads one session file, returning it only when it carries
    /// <paramref name="sessionId"/> and every field a session row needs. A
    /// file being rewritten as it is read is skipped, not fatal.
    /// </summary>
    private static ClaudeSessionFile? TryRead(string path, string sessionId)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            if (!root.TryGetProperty("sessionId", out var id)
                || id.ValueKind != JsonValueKind.String
                || id.GetString() != sessionId
                || !root.TryGetProperty("pid", out var pidElement)
                || TryReadPid(pidElement) is not { } pid
                || pid <= 0)
            {
                return null;
            }

            return new ClaudeSessionFile(
                pid,
                sessionId,
                root.TryGetProperty("cwd", out var cwd) && cwd.ValueKind == JsonValueKind.String
                    ? cwd.GetString() ?? ""
                    : "",
                root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString() ?? ""
                    : "");
        }
        catch (JsonException)
        {
            return null;
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
