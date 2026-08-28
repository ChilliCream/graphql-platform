using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class CodexHarnessVersionResolver(
    Func<string, string?>? rolloutVersionReader = null) : ICodexHarnessVersionResolver
{
    private readonly Func<string, string?> _rolloutVersionReader = rolloutVersionReader ?? ReadRolloutVersion;

    public string Resolve(string sessionId) => _rolloutVersionReader(sessionId) ?? "";

    /// <summary>
    /// Finds the session's rollout file under <c>~/.codex/sessions/</c> (the
    /// date-bucketed directory is not otherwise known, so every subdirectory
    /// is searched by filename) and reads <c>payload.cli_version</c> from
    /// its first line's <c>session_meta</c> record.
    /// </summary>
    private static string? ReadRolloutVersion(string sessionId)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");

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

            if (!root2.TryGetProperty("type", out var typeElement)
                || typeElement.GetString() != "session_meta"
                || !root2.TryGetProperty("payload", out var payload)
                || !payload.TryGetProperty("cli_version", out var versionElement))
            {
                return null;
            }

            return versionElement.GetString();
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
}
