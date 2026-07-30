using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal static class CommandLineSchemaExporter
{
    private const int MaxCapturedOutputLength = 32 * 1024;
    private static readonly TimeSpan s_processTerminationTimeout = TimeSpan.FromSeconds(10);

    public static async Task<CommandLineSchemaExportResult> ExportAsync(
        IResource resource,
        GraphQLSourceSchemaAnnotation declaration,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (declaration is not
            {
                Location: SourceSchemaLocationType.CommandLineExport,
                ExportSchemaName: { } schemaName,
                ExportConfiguration: { } configuration,
                ExportTargetFramework: { } targetFramework,
                ExportTimeout: { } timeout
            }
            || string.IsNullOrWhiteSpace(schemaName)
            || string.IsNullOrWhiteSpace(configuration)
            || string.IsNullOrWhiteSpace(targetFramework)
            || timeout <= TimeSpan.Zero
            || declaration.ExportRuntimeIdentifier is { } runtimeIdentifier
                && string.IsNullOrWhiteSpace(runtimeIdentifier))
        {
            throw new InvalidOperationException(
                $"Resource '{resource.Name}' does not have a complete command-line export declaration.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var projectPath = IOPath.GetFullPath(GraphQLResourceModel.GetProjectPath(resource));
        outputDirectory = IOPath.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var schemaPath = IOPath.Combine(outputDirectory, "schema.graphqls");
        var settingsPath = IOPath.Combine(outputDirectory, "schema-settings.json");

        DeleteExistingArtifact(schemaPath);
        DeleteExistingArtifact(settingsPath);

        var startInfo = CreateStartInfo(
            projectPath,
            schemaPath,
            schemaName,
            configuration,
            targetFramework,
            declaration.ExportRuntimeIdentifier);
        var processResult = await RunProcessAsync(
            startInfo,
            timeout,
            resource.Name,
            cancellationToken);

        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resource.Name}' exited with code "
                + $"{processResult.ExitCode}. Child-process output was suppressed because it may "
                + "contain sensitive data.");
        }

        await ValidateArtifactsAsync(
            resource.Name,
            schemaName,
            schemaPath,
            settingsPath,
            cancellationToken);

        return new(
            schemaPath,
            settingsPath,
            projectPath,
            configuration,
            targetFramework,
            declaration.ExportRuntimeIdentifier);
    }

    internal static ProcessStartInfo CreateStartInfo(
        string projectPath,
        string schemaPath,
        string schemaName,
        string configuration,
        string targetFramework,
        string? runtimeIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);

        if (runtimeIdentifier is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(runtimeIdentifier);
        }

        projectPath = IOPath.GetFullPath(projectPath);
        schemaPath = IOPath.GetFullPath(schemaPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = IOPath.GetDirectoryName(projectPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--framework");
        startInfo.ArgumentList.Add(targetFramework);

        if (runtimeIdentifier is not null)
        {
            startInfo.ArgumentList.Add("--runtime");
            startInfo.ArgumentList.Add(runtimeIdentifier);
        }

        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("schema");
        startInfo.ArgumentList.Add("export");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(schemaPath);
        startInfo.ArgumentList.Add("--schema-name");
        startInfo.ArgumentList.Add(schemaName);

        ApplyEnvironmentAllowList(startInfo);
        return startInfo;
    }

    internal static async Task<CommandLineProcessResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        string resourceName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = startInfo };
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        using var outputSource = new CancellationTokenSource();

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Could not start schema export for resource '{resourceName}'.");
        }

        var stdout = CaptureAsync(
            process.StandardOutput,
            retainOutput: true,
            outputSource.Token);
        var stderr = CaptureAsync(
            process.StandardError,
            retainOutput: false,
            outputSource.Token);

        try
        {
            try
            {
                await process.WaitForExitAsync(linkedSource.Token);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested
                    || timeoutSource.IsCancellationRequested)
            {
                await TerminateProcessAsync(
                    process,
                    stdout,
                    stderr,
                    outputSource);

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                throw new TimeoutException(
                    $"Schema export for resource '{resourceName}' exceeded {timeout}.");
            }
            catch
            {
                await TerminateProcessAsync(
                    process,
                    stdout,
                    stderr,
                    outputSource);
                throw;
            }

            var captured = await Task.WhenAll(stdout, stderr);
            return new(
                process.ExitCode,
                captured[0].Output,
                captured[0].WasTruncated,
                captured[1].CharacterCount);
        }
        finally
        {
            if (!HasExited(process))
            {
                await TerminateProcessAsync(
                    process,
                    stdout,
                    stderr,
                    outputSource);
            }
        }
    }

    internal static void ApplyEnvironmentAllowList(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var inheritedEnvironment = startInfo.Environment.ToArray();
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        startInfo.Environment.Clear();

        CopyIfPresent("PATH");
        CopyIfPresent("DOTNET_ROOT");
        CopyIfPresent("DOTNET_ROOT_X64");
        CopyIfPresent("DOTNET_ROOT_X86");
        CopyIfPresent("DOTNET_ROOT_ARM64");
        CopyIfPresent("DOTNET_CLI_HOME");
        CopyIfPresent("NUGET_PACKAGES");

        if (OperatingSystem.IsWindows())
        {
            CopyIfPresent("SystemRoot");
            CopyIfPresent("SystemDrive");
            CopyIfPresent("USERPROFILE");
            CopyIfPresent("LOCALAPPDATA");
        }
        else
        {
            CopyIfPresent("HOME");
        }

        CopyIfPresent("TMPDIR");
        CopyIfPresent("TMP");
        CopyIfPresent("TEMP");

        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        void CopyIfPresent(string name)
        {
            foreach (var pair in inheritedEnvironment)
            {
                if (pair.Key.Equals(name, comparison)
                    && !string.IsNullOrEmpty(pair.Value))
                {
                    startInfo.Environment[name] = pair.Value;
                    return;
                }
            }
        }
    }

    internal static async Task ValidateArtifactsAsync(
        string resourceName,
        string schemaName,
        string schemaPath,
        string settingsPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);

        if (!File.Exists(schemaPath) || !File.Exists(settingsPath))
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resourceName}' did not produce both "
                + "schema.graphqls and schema-settings.json.");
        }

        var schemaText = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        var settingsText = await File.ReadAllTextAsync(settingsPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(settingsText))
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resourceName}' produced empty schema-settings.json.");
        }

        using var settings = JsonDocument.Parse(settingsText);
        var configuration = SchemaComposition.ReadEndpointConfiguration(
            resourceName,
            schemaName,
            settings);
        GraphQLSourceSchemaValidator.Validate(
            resourceName,
            configuration,
            schemaText);
    }

    private static async Task<CapturedOutput> CaptureAsync(
        StreamReader reader,
        bool retainOutput,
        CancellationToken cancellationToken)
    {
        StringBuilder? output = retainOutput
            ? new StringBuilder(MaxCapturedOutputLength)
            : null;
        var buffer = new char[4096];
        var characterCount = 0L;

        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (count == 0)
            {
                break;
            }

            characterCount += count;

            if (output?.Length is < MaxCapturedOutputLength)
            {
                output.Append(
                    buffer,
                    0,
                    Math.Min(count, MaxCapturedOutputLength - output.Length));
            }
        }

        return new(
            output?.ToString() ?? string.Empty,
            characterCount,
            characterCount > MaxCapturedOutputLength);
    }

    private static async Task TerminateProcessAsync(
        Process process,
        Task<CapturedOutput> stdout,
        Task<CapturedOutput> stderr,
        CancellationTokenSource outputSource)
    {
        TryKillProcess(process, entireProcessTree: true);

        if (!HasExited(process))
        {
            TryKillProcess(process, entireProcessTree: false);
        }

        using var cleanupSource = new CancellationTokenSource(s_processTerminationTimeout);

        try
        {
            await process.WaitForExitAsync(cleanupSource.Token);
            await Task.WhenAll(stdout, stderr).WaitAsync(cleanupSource.Token);
        }
        catch (OperationCanceledException) when (cleanupSource.IsCancellationRequested)
        {
            outputSource.Cancel();
        }
        catch (IOException)
        {
            outputSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            outputSource.Cancel();
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static void TryKillProcess(Process process, bool entireProcessTree)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    private static void DeleteExistingArtifact(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private readonly record struct CapturedOutput(
        string Output,
        long CharacterCount,
        bool WasTruncated);
}

internal sealed record CommandLineSchemaExportResult(
    string SchemaPath,
    string SettingsPath,
    string ProjectPath,
    string Configuration,
    string TargetFramework,
    string? RuntimeIdentifier);

internal sealed record CommandLineProcessResult(
    int ExitCode,
    string StandardOutput,
    bool StandardOutputWasTruncated,
    long StandardErrorCharacterCount);
