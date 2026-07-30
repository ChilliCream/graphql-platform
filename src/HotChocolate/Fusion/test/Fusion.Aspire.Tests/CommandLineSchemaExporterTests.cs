using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Aspire;

public sealed class CommandLineSchemaExporterTests
{
    [Fact]
    public void CreateStartInfo_Should_UseExplicitArgumentSafeBuildInputs()
    {
        var projectPath = GetTestProjectFile();
        var schemaPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "schema export",
            "schema.graphqls");

        var startInfo = CommandLineSchemaExporter.CreateStartInfo(
            projectPath,
            schemaPath,
            "Products Schema",
            "Release",
            "net9.0",
            "linux-x64");

        string.Join(Environment.NewLine, startInfo.ArgumentList).MatchInlineSnapshot(
            $$"""
            run
            --project
            {{projectPath}}
            --configuration
            Release
            --framework
            net9.0
            --runtime
            linux-x64
            --no-launch-profile
            --
            schema
            export
            --output
            {{schemaPath}}
            --schema-name
            Products Schema
            """);
    }

    [Fact]
    public void CreateStartInfo_Should_OmitRuntimeArgument_When_ExportIsExplicitlyPortable()
    {
        var startInfo = CommandLineSchemaExporter.CreateStartInfo(
            GetTestProjectFile(),
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "schema.graphqls"),
            "Products",
            "Release",
            "net9.0",
            runtimeIdentifier: null);

        Assert.Equal(
            [
                "run",
                "--project",
                GetTestProjectFile(),
                "--configuration",
                "Release",
                "--framework",
                "net9.0",
                "--no-launch-profile",
                "--",
                "schema",
                "export",
                "--output",
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "schema.graphqls"),
                "--schema-name",
                "Products"
            ],
            startInfo.ArgumentList);
    }

    [Fact]
    public void ApplyEnvironmentAllowList_Should_RemoveSecretsAndKeepOsBootstrapVariables()
    {
        var startInfo = new ProcessStartInfo();
        startInfo.Environment.Clear();
        startInfo.Environment["PATH"] = "dotnet-path";
        startInfo.Environment["DOTNET_ROOT"] = "dotnet-root";
        startInfo.Environment["NUGET_PACKAGES"] = "packages";
        startInfo.Environment["NITRO_API_KEY"] = "secret";

        if (OperatingSystem.IsWindows())
        {
            startInfo.Environment["SystemRoot"] = "system-root";
            startInfo.Environment["USERPROFILE"] = "user-profile";
        }
        else
        {
            startInfo.Environment["HOME"] = "user-home";
            startInfo.Environment["path"] = "wrong-case-path";
        }

        CommandLineSchemaExporter.ApplyEnvironmentAllowList(startInfo);

        var expected = OperatingSystem.IsWindows()
            ? new Dictionary<string, string?>
            {
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_ROOT"] = "dotnet-root",
                ["NUGET_PACKAGES"] = "packages",
                ["PATH"] = "dotnet-path",
                ["SystemRoot"] = "system-root",
                ["USERPROFILE"] = "user-profile"
            }
            : new Dictionary<string, string?>
            {
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_ROOT"] = "dotnet-root",
                ["HOME"] = "user-home",
                ["NUGET_PACKAGES"] = "packages",
                ["PATH"] = "dotnet-path"
            };

        Assert.Equal(
            expected.OrderBy(t => t.Key),
            startInfo.Environment.OrderBy(t => t.Key));
    }

    [Fact]
    public async Task RunProcessAsync_Should_DrainPipesAndBoundRetainedOutput_When_OutputIsLarge()
    {
        var startInfo = CreateLargeOutputProcess();

        var result = await CommandLineSchemaExporter.RunProcessAsync(
            startInfo,
            TimeSpan.FromSeconds(30),
            "products",
            TestContext.Current.CancellationToken);

        $$"""
        Exit code: {{result.ExitCode}}
        Standard output length: {{result.StandardOutput.Length}}
        Standard output truncated: {{result.StandardOutputWasTruncated}}
        Standard error characters: {{result.StandardErrorCharacterCount > 32 * 1024}}
        Secret retained: {{result.ToString().Contains("stderr-secret", StringComparison.Ordinal)}}
        """.MatchInlineSnapshot(
            """
            Exit code: 7
            Standard output length: 32768
            Standard output truncated: True
            Standard error characters: True
            Secret retained: False
            """);
    }

    [Fact]
    public async Task RunProcessAsync_Should_KillProcessTreeAndPreserveToken_When_Canceled()
    {
        var startInfo = CreateLongRunningProcess();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var timer = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CommandLineSchemaExporter.RunProcessAsync(
                startInfo,
                TimeSpan.FromMinutes(1),
                "products",
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(
            timer.Elapsed < TimeSpan.FromSeconds(10),
            $"Cancellation cleanup took {timer.Elapsed}.");
    }

    [Fact]
    public async Task ValidateArtifactsAsync_Should_AcceptExactNameAndValidSchema()
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile(
            "schema.graphqls",
            "type Query { product: String }");
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Products" }""");

        await CommandLineSchemaExporter.ValidateArtifactsAsync(
            "products",
            "Products",
            schemaPath,
            settingsPath,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ValidateArtifactsAsync_Should_RejectNameMismatch()
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile(
            "schema.graphqls",
            "type Query { product: String }");
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Inventory" }""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CommandLineSchemaExporter.ValidateArtifactsAsync(
                "products",
                "Products",
                schemaPath,
                settingsPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The configured source schema name 'Products' for resource 'products' "
            + "does not match schema-settings.json name 'Inventory'.",
            exception.Message);
    }

    [Theory]
    [InlineData("  ", "Schema export for resource 'products' produced empty GraphQL SDL.")]
    [InlineData(
        "type Query {",
        "Schema for resource 'products' is not valid GraphQL SDL.")]
    public async Task ValidateArtifactsAsync_Should_RejectInvalidSchema(
        string schema,
        string expectedMessage)
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile("schema.graphqls", schema);
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Products" }""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CommandLineSchemaExporter.ValidateArtifactsAsync(
                "products",
                "Products",
                schemaPath,
                settingsPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, exception.Message);
    }

    private static ProcessStartInfo CreateLargeOutputProcess()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateProcess(
                "cmd.exe",
                "/D",
                "/S",
                "/C",
                "(for /L %i in (1,1,4000) do @echo 01234567890123456789)"
                + "&(for /L %i in (1,1,4000) do @echo stderr-secret-0123456789 1>&2)"
                + "&exit /b 7");
        }

        return CreateProcess(
            "/bin/sh",
            "-c",
            "i=0; while [ $i -lt 4000 ]; do "
            + "printf '01234567890123456789\\n'; "
            + "printf 'stderr-secret-0123456789\\n' >&2; "
            + "i=$((i + 1)); done; exit 7");
    }

    private static ProcessStartInfo CreateLongRunningProcess()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateProcess(
                "cmd.exe",
                "/D",
                "/S",
                "/C",
                "ping -t 127.0.0.1 >nul");
        }

        return CreateProcess(
            "/bin/sh",
            "-c",
            "while true; do sleep 1; done");
    }

    private static ProcessStartInfo CreateProcess(
        string fileName,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(sourceFile)!,
                "HotChocolate.Fusion.Aspire.Tests.csproj"));

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "fusion-aspire-tests",
            Guid.NewGuid().ToString("N"));

        public TemporaryDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        public string WriteFile(string fileName, string content)
        {
            var path = System.IO.Path.Combine(_path, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(_path, recursive: true);
        }
    }
}
