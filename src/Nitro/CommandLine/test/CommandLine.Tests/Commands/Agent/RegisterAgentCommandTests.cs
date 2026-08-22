using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class RegisterAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "register", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Register the resolved actor as an agent, with an optional role. --actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.

            Usage:
              nitro agent register [options]

            Options:
              --actor <actor>    The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --role <role>      The agent's role, free text, normalized lowercase (defaults to empty)
              --client <client>  The client program the agent runs as, e.g. "claude-code" or "codex", free text, normalized lowercase (defaults to auto-detected, or empty)
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent register
              nitro agent register --role "backend"
            """);
    }

    [Fact]
    public async Task DefaultActor_RegistersAgent()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register");

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
    public async Task RoleOption_RegistersAgentWithRole()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "Backend");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent' as 'backend'.
            """);
        Assert.Equal(
            "backend",
            await QueryScalarAsync("SELECT role FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task ClientOption_RegistersAgentWithClient()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--client", "Claude-Code");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "claude-code",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task NoClientOptionAndNoMarker_RegistersAgentWithEmptyClient()
    {
        // arrange: no --client given, and the mocked environment carries no
        // CLAUDECODE (or any other) marker, so DetectClient finds nothing.
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task ActorOption_OverridesEnvironmentVariable()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "register", "--actor", "Explicit-Agent");

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
        var result = await ExecuteCommandAsync(
            "agent", "register", "--role", "backend", "--client", "codex");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("name").GetString());
        Assert.Equal("backend", root.GetProperty("role").GetString());
        Assert.Equal("codex", root.GetProperty("client").GetString());
        Assert.True(root.TryGetProperty("registeredAt", out _));
        Assert.True(root.TryGetProperty("lastSeenAt", out _));
    }

    [Fact]
    public async Task Reregister_IsIdempotent_AndUpdatesLastSeenAt()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");
        var registeredAt = await QueryScalarAsync(
            "SELECT registered_at FROM agents WHERE name = 'test-agent'");
        FakeTime.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await ExecuteCommandAsync("agent", "register");

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
    public async Task Reregister_WithoutRole_ClearsPreviousRole()
    {
        // arrange: register sets role on every call, defaulting to empty
        // when --role is omitted, the same way last_seen_at is always
        // bumped; only the mail-send auto-registration path (TouchAsync)
        // preserves an existing role.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--role", "backend");

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT role FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task Reregister_WithoutClient_ClearsPreviousClient()
    {
        // arrange: register sets client on every call, defaulting to
        // auto-detected-or-empty when --client is omitted, the same way
        // role and last_seen_at are always overwritten.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--client", "claude-code");

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
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
        var result = await ExecuteCommandAsync("agent", "register", "--actor", actor);

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
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "");

        // assert
        result.AssertError(
            """
            An agent name must not be empty.
            """);
    }
}

/// <summary>
/// Exercises <see cref="RegisterAgentCommand.DetectClient"/> directly,
/// against a hand-rolled <see cref="IEnvironmentVariableProvider"/> rather
/// than through the CLI: the mocked provider the other command tests share
/// only stubs names under a "NITRO_" prefix
/// (<see cref="ChilliCream.Nitro.CommandLine.Tests.Commands.CommandTestBase.SetupEnvironmentVariable"/>),
/// which cannot represent an unprefixed marker like CLAUDECODE.
/// </summary>
public sealed class RegisterAgentCommandDetectClientTests
{
    [Fact]
    public void DetectClient_Should_ReturnClaudeCode_When_CLAUDECODEIsSet()
    {
        // arrange
        var environmentVariables = new FakeEnvironmentVariableProvider(
            new Dictionary<string, string> { ["CLAUDECODE"] = "1" });

        // act
        var client = RegisterAgentCommand.DetectClient(environmentVariables);

        // assert
        Assert.Equal("claude-code", client);
    }

    [Fact]
    public void DetectClient_Should_ReturnNull_When_NoKnownMarkerIsSet()
    {
        // arrange
        var environmentVariables = new FakeEnvironmentVariableProvider(new Dictionary<string, string>());

        // act
        var client = RegisterAgentCommand.DetectClient(environmentVariables);

        // assert
        Assert.Null(client);
    }

    private sealed class FakeEnvironmentVariableProvider(IReadOnlyDictionary<string, string> variables)
        : IEnvironmentVariableProvider
    {
        public string? GetEnvironmentVariable(string name)
            => variables.GetValueOrDefault(name);
    }
}
