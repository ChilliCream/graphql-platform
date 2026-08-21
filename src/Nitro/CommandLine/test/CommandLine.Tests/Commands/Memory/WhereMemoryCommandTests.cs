namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class WhereMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "where", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Print the resolved project and global memory store paths.

            Usage:
              nitro agent memory where [options]

            Options:
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory where
              nitro agent memory where --scope global
            """);
    }

    [Fact]
    public async Task WithWorkspace_PrintsBothPaths()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"project: {MemoryDirectory}", result.StdOut);
        Assert.Contains($"global: {GlobalMemoryDirectory}", result.StdOut);
    }

    [Fact]
    public async Task NoWorkspace_PrintsNotFoundForProject_AndStillPrintsGlobal()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("project: (not found)", result.StdOut);
        Assert.Contains($"global: {GlobalMemoryDirectory}", result.StdOut);
    }

    [Fact]
    public async Task ScopeFilter_Project_OnlyPrintsProjectPath()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where", "--scope", "project");

        // assert
        result.AssertSuccess($"project: {MemoryDirectory}");
    }

    [Fact]
    public async Task ScopeFilter_Global_OnlyPrintsGlobalPath()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where", "--scope", "global");

        // assert
        result.AssertSuccess($"global: {GlobalMemoryDirectory}");
    }

    [Fact]
    public async Task JsonOutput_ReturnsBothLocations()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.Length);
        Assert.Equal("project", items[0].GetProperty("scope").GetString());
        Assert.Equal(MemoryDirectory, items[0].GetProperty("path").GetString());
        Assert.Equal("global", items[1].GetProperty("scope").GetString());
        Assert.Equal(GlobalMemoryDirectory, items[1].GetProperty("path").GetString());
    }

    [Fact]
    public async Task JsonOutput_NoWorkspace_ReturnsNullProjectPath()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "where");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("project", items[0].GetProperty("scope").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, items[0].GetProperty("path").ValueKind);
    }
}
