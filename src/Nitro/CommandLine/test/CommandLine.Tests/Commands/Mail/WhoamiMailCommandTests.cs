namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class WhoamiMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Print the resolved actor identity and whether it is registered in this workspace.

            Usage:
              nitro agent mail whoami [options]

            Options:
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail whoami
            """);
    }

    [Fact]
    public async Task Unregistered_PrintsActorAndNotRegistered()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami");

        // assert
        result.AssertSuccess(
            """
            test-agent
            Not registered in this workspace. Run `nitro agent mail register` to register.
            """);
    }

    [Fact]
    public async Task Registered_PrintsActorAndRegistered()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register");

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami");

        // assert
        result.AssertSuccess(
            """
            test-agent
            Registered in this workspace.
            """);
    }

    [Fact]
    public async Task JsonOutput_Unregistered_ReturnsNameAndFalse()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("name").GetString());
        Assert.False(root.GetProperty("registered").GetBoolean());
    }

    [Fact]
    public async Task JsonOutput_Registered_ReturnsNameAndTrue()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal("test-agent", root.GetProperty("name").GetString());
        Assert.True(root.GetProperty("registered").GetBoolean());
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "whoami");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }
}
