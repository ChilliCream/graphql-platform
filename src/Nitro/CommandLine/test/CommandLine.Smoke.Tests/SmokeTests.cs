using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CliWrap;
using CliWrap.Buffered;

namespace ChilliCream.Nitro.CommandLine.Smoke.Tests;

public class SmokeTests
{
    private static readonly string ProjectPath = ResolveProjectPath();
    private static readonly string TargetFramework = ResolveTargetFramework();
    private static readonly string Configuration = ResolveConfiguration();

    [Fact]
    public async Task Version_Flag_Prints_Version()
    {
        // act
        var result = await RunNitroAsync("--version");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.StandardOutput));
    }

    [Fact]
    public async Task Help_Flag_Mentions_Nitro()
    {
        // act
        var result = await RunNitroAsync("--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("nitro", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Task_Init_Creates_Workspace_In_Fresh_Directory()
    {
        // arrange
        var tempDir = CreateTempDirectory();
        try
        {
            // act
            var result = await RunNitroAsync("agent init", workingDirectory: tempDir);

            // assert
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Initialized agent workspace", result.StandardOutput);
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task Task_List_Succeeds_After_Init()
    {
        // arrange
        var tempDir = CreateTempDirectory();
        try
        {
            var initResult = await RunNitroAsync("agent init", workingDirectory: tempDir);
            Assert.Equal(0, initResult.ExitCode);

            // act
            var result = await RunNitroAsync("agent tasks list", workingDirectory: tempDir);

            // assert
            Assert.Equal(0, result.ExitCode);
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task Mail_Init_Register_Send_Inbox_Round_Trips()
    {
        // arrange: a fresh mail workspace, one actor (self-addressed, so the
        // round trip needs no second registered agent), all against the
        // real published-DI binary in a fresh temp directory (CliWrap
        // pattern of Task_Init_Creates_Workspace_In_Fresh_Directory).
        var tempDir = CreateTempDirectory();
        try
        {
            var initResult = await RunNitroAsync("agent init", workingDirectory: tempDir);
            Assert.Equal(0, initResult.ExitCode);
            Assert.Contains("Initialized agent workspace", initResult.StandardOutput);

            var registerResult = await RunNitroAsync("agent register", workingDirectory: tempDir);
            Assert.Equal(0, registerResult.ExitCode);
            Assert.Contains("Registered 'smoke-test'", registerResult.StandardOutput);

            var sendResult = await RunNitroAsync(
                "agent mail send smoke-test --subject Smoke-round-trip --body Round-trip-ok",
                workingDirectory: tempDir);
            Assert.Equal(0, sendResult.ExitCode);
            Assert.Contains("Sent '", sendResult.StandardOutput);

            // act
            var inboxResult = await RunNitroAsync("agent mail inbox", workingDirectory: tempDir);

            // assert
            Assert.Equal(0, inboxResult.ExitCode);
            Assert.Contains("Smoke-round-trip", inboxResult.StandardOutput);
            Assert.Contains("1 message(s)", inboxResult.StandardOutput);
        }
        finally
        {
            DeleteDirectory(tempDir);
        }
    }

    private static async Task<BufferedCommandResult> RunNitroAsync(string arguments, string? workingDirectory = null)
    {
        // Run the already-built CLI in the SAME configuration these tests were
        // built in. 'dotnet run --no-build' defaults to Debug, so without this
        // a Release test run (e.g. CI) would point at a Debug build that does
        // not exist and every smoke test would fail with exit code 1.
        var args = new[]
        {
            "run", "--project", ProjectPath, "-c", Configuration, "--framework", TargetFramework, "--no-build", "--"
        }.Concat(SplitArguments(arguments));

        var command = Cli.Wrap("dotnet")
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithEnvironmentVariables(env => env.Set("NITRO_TASK_ACTOR", "smoke-test"));

        if (workingDirectory is not null)
        {
            command = command.WithWorkingDirectory(workingDirectory);
        }

        return await command.ExecuteBufferedAsync(TestContext.Current.CancellationToken);
    }

    private static string ResolveConfiguration()
    {
        var configured = Environment.GetEnvironmentVariable("NITRO_SMOKE_TEST_CONFIGURATION");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static string ResolveTargetFramework()
    {
        var configuredFramework = Environment.GetEnvironmentVariable("NITRO_SMOKE_TEST_TFM");
        if (!string.IsNullOrWhiteSpace(configuredFramework))
        {
            return configuredFramework.Trim();
        }

        var frameworkName =
            typeof(SmokeTests).Assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), inherit: false)
                .OfType<TargetFrameworkAttribute>()
                .Single()
                .FrameworkName;

        const string versionPrefix = ".NETCoreApp,Version=v";
        if (!frameworkName.StartsWith(versionPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported target framework '{frameworkName}'.");
        }

        return "net" + frameworkName[versionPrefix.Length..];
    }

    private static IEnumerable<string> SplitArguments(string arguments)
    {
        return arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nitro-smoke-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static string ResolveProjectPath([CallerFilePath] string? sourceFilePath = null)
    {
        var dir =
            Path.GetDirectoryName(sourceFilePath)
            ?? throw new InvalidOperationException("Unable to resolve smoke test source directory.");
        return Path.GetFullPath(Path.Combine(dir, "..", "..", "src", "CommandLine", "Nitro.CommandLine.csproj"));
    }
}
