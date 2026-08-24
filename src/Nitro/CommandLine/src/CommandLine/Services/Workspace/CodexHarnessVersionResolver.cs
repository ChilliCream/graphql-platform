using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed partial class CodexHarnessVersionResolver(
    Func<string, string?>? rolloutVersionReader = null,
    Func<int, string?>? exePathReader = null) : ICodexHarnessVersionResolver
{
    private readonly Func<string, string?> _rolloutVersionReader = rolloutVersionReader ?? ReadRolloutVersion;
    private readonly Func<int, string?> _exePathReader = exePathReader ?? ReadExePath;

    public string Resolve(string sessionId, int ancestorPid)
    {
        var fromRollout = _rolloutVersionReader(sessionId);

        if (fromRollout is { Length: > 0 })
        {
            return fromRollout;
        }

        var exePath = _exePathReader(ancestorPid);

        if (exePath is null)
        {
            return "";
        }

        var match = ReleaseVersionPattern().Match(exePath);

        return match.Success ? match.Groups[1].Value : "";
    }

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

    private static string? ReadExePath(int pid)
    {
        try
        {
            return File.ResolveLinkTarget($"/proc/{pid}/exe", returnFinalTarget: true)?.FullName;
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

    // Standalone installs live under
    // ~/.codex/packages/standalone/releases/<version>-<target-triple>/bin/codex.
    // The version can itself contain dots and hyphens (a prerelease suffix),
    // so the target triple's architecture prefix anchors where it ends;
    // matching only x86_64/aarch64/arm64/i686 avoids over-capturing into the
    // triple on architectures whose name has no underscore of its own.
    [GeneratedRegex(@"/releases/(\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)*?)-(?:x86_64|aarch64|arm64|i686)-")]
    private static partial Regex ReleaseVersionPattern();
}
