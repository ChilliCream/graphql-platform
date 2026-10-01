using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal sealed partial class OpencodeVersionResolver(
    Func<CancellationToken, Task<string?>>? versionReader = null) : IOpencodeVersionResolver
{
    private readonly Func<CancellationToken, Task<string?>> _versionReader = versionReader ?? ReadVersionAsync;

    public async Task<Version?> ResolveAsync(CancellationToken cancellationToken)
    {
        var text = await _versionReader(cancellationToken);

        if (text is null)
        {
            return null;
        }

        var match = VersionPattern().Match(text);

        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
    }

    private static async Task<string?> ReadVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("opencode")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--version");
            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await error;

            return process.ExitCode == 0 ? await output : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    [GeneratedRegex(@"\d+\.\d+(?:\.\d+)?")]
    private static partial Regex VersionPattern();
}
