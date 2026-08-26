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
    /// Reads one session file, returning it only when it carries
    /// <paramref name="sessionId"/>. A file being rewritten as it is read is
    /// skipped, not fatal.
    /// </summary>
    private static ClaudeSessionFile? TryRead(string path, string sessionId)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            if (!root.TryGetProperty("sessionId", out var id)
                || id.ValueKind != JsonValueKind.String
                || id.GetString() != sessionId)
            {
                return null;
            }

            return new ClaudeSessionFile(
                sessionId,
                root.TryGetProperty("cwd", out var cwd) && cwd.ValueKind == JsonValueKind.String
                    ? cwd.GetString() ?? ""
                    : "",
                root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString() ?? ""
                    : "",
                root.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.String
                    ? version.GetString() ?? ""
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
