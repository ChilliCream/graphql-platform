namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class ShowMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a curated memory's details.

            Usage:
              nitro agent memory show <id> [options]

            Arguments:
              <id>  The memory ID

            Options:
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory show "01hqzxk8xdtd3fk3f0z7c5g8vm"
            """);
    }

    [Fact]
    public async Task ExistingMemory_PrintsDetails()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Use pnpm, not npm.", type: "preference", tags: ["tooling"]);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"{record.Id} (preference)", result.StdOut);
        Assert.Contains("Tags: tooling", result.StdOut);
        Assert.Contains("Use pnpm, not npm.", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_ReturnsDetailResult()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Use pnpm, not npm.", type: "preference", tags: ["tooling"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(record.Id, root.GetProperty("id").GetString());
        Assert.Equal("project", root.GetProperty("scope").GetString());
        Assert.Equal("preference", root.GetProperty("type").GetString());
        Assert.Equal("Use pnpm, not npm.", root.GetProperty("text").GetString());
        Assert.Equal(["tooling"], root.GetProperty("tags").EnumerateArray().Select(e => e.GetString()!).ToArray());
    }

    [Fact]
    public async Task MemoryNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", "01hqzxk8xdtd3fk3f0z7c5g8vm");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }

    [Fact]
    public async Task NoWorkspace_DefaultScope_ReturnsDoesNotExist()
    {
        // The default scope is "all": with no project workspace and no
        // matching global memory either, this is a genuine not-found, not a
        // missing-workspace condition.

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", "01hqzxk8xdtd3fk3f0z7c5g8vm");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }

    [Fact]
    public async Task NoWorkspace_ExplicitProjectScope_ReturnsMissingWorkspaceError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", "01hqzxk8xdtd3fk3f0z7c5g8vm", "--scope", "project");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }

    [Fact]
    public async Task ExplicitGlobalScope_ShowsGlobalMemory()
    {
        // arrange
        var record = await SeedMemoryAsync("Global text.", scope: "global");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id, "--scope", "global");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scope: global", result.StdOut);
    }

    [Fact]
    public async Task DefaultScope_FindsGlobalMemory_When_NoProjectWorkspaceExists()
    {
        // arrange
        var record = await SeedMemoryAsync("Global text.", scope: "global");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scope: global", result.StdOut);
    }

    [Fact]
    public async Task CrossScopeDuplicateId_HumanMode_ReturnsErrorWithNoPartialOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Project text.", scope: "project");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", projectRecord.Id);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Contains("Cross-scope duplicate memory ids found", result.StdErr);
        Assert.Contains(projectRecord.Id, result.StdErr);
    }

    [Fact]
    public async Task CrossScopeDuplicateId_JsonMode_ReturnsStructuredDiagnostic()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Project text.", scope: "project");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", projectRecord.Id);

        // assert
        Assert.Equal(1, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var conflicts = document.RootElement.GetProperty("conflicts").EnumerateArray().ToArray();
        var conflict = Assert.Single(conflicts);
        Assert.Equal(projectRecord.Id, conflict.GetProperty("id").GetString());
        Assert.Equal(
            ["project", "global"],
            conflict.GetProperty("scopes").EnumerateArray().Select(e => e.GetString()!).ToArray());
    }

    [Fact]
    public async Task CrossScopeDuplicateId_ExplicitScope_StillWorks()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Project text.", scope: "project");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", projectRecord.Id, "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scope: project", result.StdOut);
    }
}
