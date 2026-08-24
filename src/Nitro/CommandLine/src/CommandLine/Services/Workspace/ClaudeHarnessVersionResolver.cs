using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ClaudeHarnessVersionResolver(
    Func<int, string?>? sessionFileReader = null,
    Func<int, string?>? startTicksReader = null) : IClaudeHarnessVersionResolver
{
    private readonly Func<int, string?> _sessionFileReader = sessionFileReader ?? ReadSessionFile;
    private readonly Func<int, string?> _startTicksReader = startTicksReader ?? ProcStat.ReadStartTicks;

    public string Resolve(int pid)
    {
        var json = _sessionFileReader(pid);

        if (json is null || !TryParse(json, out var version, out var recordedStartTicks))
        {
            return "";
        }

        var actualStartTicks = _startTicksReader(pid);

        // A pid whose CURRENT start ticks disagree with what the session
        // file recorded belongs to a different process than the one that
        // wrote the file (the OS reused the pid); the file is stale.
        return actualStartTicks is not null && actualStartTicks == recordedStartTicks ? version : "";
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

    private static bool TryParse(string json, out string version, out string startTicks)
    {
        version = "";
        startTicks = "";

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("version", out var versionElement)
                || !root.TryGetProperty("procStart", out var procStartElement))
            {
                return false;
            }

            var versionValue = versionElement.GetString();
            var procStartValue = procStartElement.GetString();

            if (string.IsNullOrEmpty(versionValue) || string.IsNullOrEmpty(procStartValue))
            {
                return false;
            }

            version = versionValue;
            startTicks = procStartValue;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
