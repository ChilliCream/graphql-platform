namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class RegisterMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "register", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Register the resolved actor as a mail agent. --actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.

            Usage:
              nitro agent mail register [options]

            Options:
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail register
              nitro agent mail register --actor "claude-1"
            """);
    }

    [Fact]
    public async Task DefaultActor_RegistersAgent()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync("SELECT name FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task ActorOption_OverridesEnvironmentVariable()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "register", "--actor", "Explicit-Agent");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'explicit-agent'.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsRegisteredAgent()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("name").GetString());
        Assert.True(root.TryGetProperty("registeredAt", out _));
        Assert.True(root.TryGetProperty("lastSeenAt", out _));
    }

    [Fact]
    public async Task Reregister_IsIdempotent_AndUpdatesLastSeenAt()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "mail", "register");
        var registeredAt = await QueryScalarAsync(
            "SELECT registered_at FROM agents WHERE name = 'test-agent'");
        FakeTime.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            1L,
            long.Parse(
                (await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE name = 'test-agent'"))!));
        Assert.Equal(
            registeredAt,
            await QueryScalarAsync("SELECT registered_at FROM agents WHERE name = 'test-agent'"));
        Assert.NotEqual(
            registeredAt,
            await QueryScalarAsync("SELECT last_seen_at FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register");

        // assert
        result.AssertError(
            """
            No mail workspace found. Run `nitro agent mail init` first.
            """);
    }

    [Theory]
    [InlineData("agent.mail")]
    [InlineData("agent name")]
    public async Task InvalidActor_ReturnsError(string actor)
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register", "--actor", actor);

        // assert
        Assert.Empty(result.StdOut);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(actor, result.StdErr);
    }

    [Fact]
    public async Task EmptyActor_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "register", "--actor", "");

        // assert
        result.AssertError(
            """
            An agent name must not be empty.
            """);
    }
}
