using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// The settlement's M10 test (perles-net-k3j.16): repeated sends across
/// watcher restart boundaries all produce at least one <c>session.send</c>,
/// including mail accumulated before the extension ever started. The
/// watcher's state machine ships as plain JS inside the embedded
/// <c>extension.mjs</c> asset (see <c>CopilotExtensionAsset</c>), not C#, so
/// this test shells out to <c>node --test</c> against
/// <c>test/fixtures/copilot-extension/state-machine.m10.test.mjs</c>, which
/// imports and drives that asset's exported pure functions directly. This
/// class only verifies the node process actually ran and passed; the
/// assertions themselves live in the .mjs fixture.
/// </summary>
public sealed class CopilotExtensionStateMachineM10Tests
{
    [Fact]
    public async Task NodeTest_StateMachineM10Fixture_Passes()
    {
        if (!IsNodeAvailable())
        {
            Assert.Skip("'node' is not available on this machine.");
        }

        var fixturePath = ResolveFixturePath();

        if (!File.Exists(fixturePath))
        {
            Assert.Skip($"M10 fixture not found at '{fixturePath}'.");
        }

        var startInfo = new ProcessStartInfo("node")
        {
            ArgumentList = { "--test", fixturePath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the 'node' process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        // Full output on failure, never truncated: a `node --test` TAP
        // report names exactly which restart-boundary scenario failed.
        var diagnostics = new StringBuilder()
            .AppendLine($"node --test exited {process.ExitCode}.")
            .AppendLine("--- stdout ---")
            .AppendLine(stdout)
            .AppendLine("--- stderr ---")
            .AppendLine(stderr)
            .ToString();

        Assert.True(process.ExitCode == 0, diagnostics);
    }

    private static bool IsNodeAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("node")
            {
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            process?.WaitForExit();
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    private static string ResolveFixturePath([CallerFilePath] string thisFilePath = "")
    {
        // thisFilePath: .../test/CommandLine.Tests/Services/Hook/CopilotExtensionStateMachineM10Tests.cs
        var directory = Path.GetDirectoryName(thisFilePath)!; // .../Services/Hook

        for (var i = 0; i < 3; i++)
        {
            directory = Path.GetDirectoryName(directory)
                ?? throw new InvalidOperationException($"Could not walk up from '{thisFilePath}'.");
        }

        // directory is now .../test
        return Path.Combine(directory, "fixtures", "copilot-extension", "state-machine.m10.test.mjs");
    }
}
