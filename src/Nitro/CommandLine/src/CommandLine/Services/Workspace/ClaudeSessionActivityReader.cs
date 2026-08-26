using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

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
