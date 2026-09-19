using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ClaudeSessionActivityReader(Func<string, string?>? sessionFileReader = null)
    : IClaudeSessionActivityReader
{
    private readonly Func<string, string?> _sessionFileReader = sessionFileReader ?? ReadSessionFile;

    public string? GetStatus(string sessionId)
    {
        var json = _sessionFileReader(sessionId);

        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.TryGetProperty("status", out var status)
                ? status.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the contents of a session file with the matching session id, or null
    /// when no readable matching file is found.
    /// </summary>
    private static string? ReadSessionFile(string sessionId)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "sessions");

        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
            {
                var json = File.ReadAllText(path);

                try
                {
                    using var document = JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty("sessionId", out var id)
                        && id.ValueKind == JsonValueKind.String
                        && id.GetString() == sessionId)
                    {
                        return json;
                    }
                }
                catch (JsonException)
                {
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
}
