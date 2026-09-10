using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexForeignNotifyRunner : ICodexForeignNotifyRunner
{
    public async Task<int?> RunAsync(
        IReadOnlyList<string> foreignArgv, string payloadJson, CancellationToken cancellationToken)
    {
        if (foreignArgv.Count == 0)
        {
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo(foreignArgv[0])
            {
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            for (var i = 1; i < foreignArgv.Count; i++)
            {
                startInfo.ArgumentList.Add(foreignArgv[i]);
            }

            startInfo.ArgumentList.Add(payloadJson);

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return null;
            }

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode;
        }
        catch
        {
            // Fail-open: the foreign program no longer exists, is no longer
            // executable, or any other spawn-time failure.
            return null;
        }
    }
}
