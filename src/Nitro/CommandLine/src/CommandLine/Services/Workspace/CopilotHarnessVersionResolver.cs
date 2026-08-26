using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed partial class CopilotHarnessVersionResolver(
    Func<string, string?>? sessionStateVersionReader = null,
    Func<int, string?>? exeVersionOutputReader = null) : ICopilotHarnessVersionResolver
{
    /// <summary>
    /// Bounds the fallback <c>--version</c> exec: a hung child must never
    /// block the hook that spawned it.
    /// </summary>
    private static readonly TimeSpan s_execTimeout = TimeSpan.FromSeconds(5);

    private readonly Func<string, string?> _sessionStateVersionReader =
        sessionStateVersionReader ?? ReadSessionStateVersion;
    private readonly Func<int, string?> _exeVersionOutputReader = exeVersionOutputReader ?? ReadExeVersionOutput;

    public string Resolve(string sessionId, int ancestorPid)
    {
        var fromSessionState = _sessionStateVersionReader(sessionId);

        if (fromSessionState is { Length: > 0 })
        {
            return fromSessionState;
        }

        var output = _exeVersionOutputReader(ancestorPid);

        if (output is null)
        {
            return "";
        }

        var match = VersionOutputPattern().Match(output);

        return match.Success ? match.Groups[1].Value : "";
    }

    /// <summary>
    /// Reads <c>~/.copilot/session-state/&lt;sessionId&gt;/events.jsonl</c>
    /// looking for the <c>session.start</c> event's <c>data.copilotVersion</c>.
    /// </summary>
    private static string? ReadSessionStateVersion(string sessionId)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".copilot",
                "session-state",
                sessionId,
                "events.jsonl");

            if (!File.Exists(path))
            {
                return null;
            }

            foreach (var line in File.ReadLines(path))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                if (root.TryGetProperty("type", out var typeElement)
                    && typeElement.GetString() == "session.start"
                    && root.TryGetProperty("data", out var data)
                    && data.TryGetProperty("copilotVersion", out var versionElement))
                {
                    return versionElement.GetString();
                }
            }

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
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs <paramref name="pid"/>'s own executable with <c>--version</c>
    /// and returns its first output line, or null on any failure, timeout,
    /// or when the pid's executable path cannot be resolved.
    /// </summary>
    private static string? ReadExeVersionOutput(int pid)
    {
        try
        {
            var exePath = File.ResolveLinkTarget($"/proc/{pid}/exe", returnFinalTarget: true)?.FullName;

            if (exePath is null)
            {
                return null;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(exePath, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            process.Start();

            if (!process.WaitForExit((int)s_execTimeout.TotalMilliseconds))
            {
                process.Kill();
                return null;
            }

            return process.StandardOutput.ReadLine();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    // The trailing period is part of the grammar; a second advisory line
    // (if any) is ignored since only the first line is read.
    [GeneratedRegex(@"^GitHub Copilot CLI (\d+\.\d+\.\d+)\.$")]
    private static partial Regex VersionOutputPattern();
}
