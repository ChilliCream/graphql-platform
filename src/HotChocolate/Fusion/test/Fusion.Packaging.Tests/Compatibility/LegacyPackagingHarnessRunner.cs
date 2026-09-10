using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Builds and drives tools/LegacyPackagingHarness, an out-of-process reader/writer pinned
/// to the released 16.6.4 HotChocolate.Fusion.Packaging package, for repo-7i1.2. Every
/// invocation runs the harness as a separate OS process so the pinned old package never
/// shares an assembly-load context, or an output directory, with the renamed
/// HotChocolate.Fusion.Packaging assembly this test project references directly.
/// </summary>
internal static class LegacyPackagingHarnessRunner
{
    private static readonly SemaphoreSlim s_buildLock = new(1, 1);
    private static string? s_harnessDllPath;

    /// <summary>
    /// Runs the harness with the given command (<c>write</c> or <c>read</c>) against
    /// <paramref name="archivePath"/> and <paramref name="certPath"/>, building it first
    /// if this is the first invocation in the test run. Returns the harness's captured
    /// standard output.
    /// </summary>
    public static async Task<string> RunAsync(
        string command,
        string archivePath,
        string certPath,
        CancellationToken cancellationToken)
    {
        var dllPath = await EnsureHarnessBuiltAsync(cancellationToken);
        return await RunProcessAsync(
            "dotnet",
            [dllPath, command, archivePath, certPath],
            cancellationToken);
    }

    private static async Task<string> EnsureHarnessBuiltAsync(CancellationToken cancellationToken)
    {
        if (s_harnessDllPath is { } cached)
        {
            return cached;
        }

        await s_buildLock.WaitAsync(cancellationToken);
        try
        {
            if (s_harnessDllPath is { } cachedAfterWait)
            {
                return cachedAfterWait;
            }

            var csprojPath = GetHarnessProjectPath();
            var harnessDir = System.IO.Path.GetDirectoryName(csprojPath)
                ?? throw new InvalidOperationException("Could not resolve the harness project directory.");

            await RunProcessAsync(
                "dotnet",
                ["build", csprojPath, "-c", "Debug", "-v", "quiet"],
                cancellationToken);

            var dllPath = System.IO.Path.Combine(harnessDir, "bin", "Debug", "net10.0", "LegacyPackagingHarness.dll");

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException(
                    "Building tools/LegacyPackagingHarness did not produce the expected assembly.",
                    dllPath);
            }

            s_harnessDllPath = dllPath;
            return dllPath;
        }
        finally
        {
            s_buildLock.Release();
        }
    }

    private static async Task<string> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"""
                Process '{fileName} {string.Join(' ', arguments)}' exited with code {process.ExitCode}.
                --- stdout ---
                {standardOutput}
                --- stderr ---
                {standardError}
                """);
        }

        return standardOutput;
    }

    private static string GetHarnessProjectPath([CallerFilePath] string sourceFile = "")
    {
        // sourceFile: .../Fusion.Packaging.Tests/Compatibility/LegacyPackagingHarnessRunner.cs
        var compatibilityDir = System.IO.Path.GetDirectoryName(sourceFile)
            ?? throw new InvalidOperationException("Could not resolve the calling source directory.");
        var testProjectDir = System.IO.Path.GetDirectoryName(compatibilityDir)
            ?? throw new InvalidOperationException("Could not resolve the test project directory.");

        return System.IO.Path.Combine(testProjectDir, "tools", "LegacyPackagingHarness", "LegacyPackagingHarness.csproj");
    }
}
