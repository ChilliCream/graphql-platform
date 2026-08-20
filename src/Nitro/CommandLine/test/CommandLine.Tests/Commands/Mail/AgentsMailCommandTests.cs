namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class AgentsMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "agents", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List registered mail agents.

            Usage:
              nitro agent mail agents [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail agents
            """);
    }

    [Fact]
    public async Task NoAgents_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "agents");

        // assert
        result.AssertSuccess(
            """
            No registered agents.
            """);
    }

    [Fact]
    public async Task JsonOutput_NoAgents_ReturnsEmptyArray()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "agents");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task Agents_AreOrderedByName()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "zeta");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alpha");
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "mu");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "agents");

        // assert
        var lines = result.StdOut.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("alpha", lines[0]);
        Assert.StartsWith("mu", lines[1]);
        Assert.StartsWith("zeta", lines[2]);
    }

    [Fact]
    public async Task JsonOutput_ReturnsRegisteredAgents()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register", "--actor", "alpha");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "agents");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var items = root.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("alpha", items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "agents");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
