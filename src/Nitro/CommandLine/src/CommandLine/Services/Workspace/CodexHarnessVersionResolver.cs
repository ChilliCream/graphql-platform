using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class CodexHarnessVersionResolver(
    Func<string, string?>? rolloutVersionReader = null) : ICodexHarnessVersionResolver
{
    private readonly Func<string, string?> _rolloutVersionReader = rolloutVersionReader ?? ReadRolloutVersion;

    public string Resolve(string sessionId) => _rolloutVersionReader(sessionId) ?? "";

    /// <summary>
    /// Finds the session's rollout file under <c>~/.codex/sessions/</c> and
    /// reads <c>payload.cli_version</c> from its first line's
    /// <c>session_meta</c> record.
    /// </summary>
    private static string? ReadRolloutVersion(string sessionId)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");

            return ReadRolloutVersionCore(root, sessionId);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? ReadRolloutVersion(string root, string sessionId)
    {
        try
        {
            return ReadRolloutVersionCore(root, sessionId);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadRolloutVersionCore(string root, string sessionId)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var file = Directory.EnumerateFiles(root, $"rollout-*-{sessionId}.jsonl", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (file is null)
        {
            return null;
        }

        using var reader = new StreamReader(file);
        var firstLine = reader.ReadLine();

        if (firstLine is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(firstLine);
        var root2 = document.RootElement;

        if (root2.ValueKind != JsonValueKind.Object
            || !root2.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || typeElement.GetString() != "session_meta"
            || !root2.TryGetProperty("payload", out var payload)
            || payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("cli_version", out var versionElement)
            || versionElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return versionElement.GetString();
    }
}
