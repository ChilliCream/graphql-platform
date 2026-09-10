using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHookExecutor"/>'s fail-open envelope directly
/// against <see cref="StringReader"/>/<see cref="StringWriter"/>, mirroring
/// <c>ClaudeHookExecutorTests</c>: the suppression short-circuit, every
/// failure path resolving to the neutral <c>{}</c> response, and successful
/// translation into the wire shape, using the S1 spike's captured Codex
/// payload fixtures.
/// </summary>
public sealed class CodexHookExecutorTests
{
    [Fact]
    public async Task RunAsync_Should_WriteNeutralWithoutInvokingTheHandler_When_SuppressEnvVarIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var input = new StringReader(CodexHookFixtures.Read("payload.session-start.json"));
        var output = new StringWriter();
        var error = new StringWriter();
        var handlerInvoked = false;

        var exitCode = await CodexHookExecutor.RunAsync(
            environmentVariables,
            input,
            output,
            error,
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(CodexHookOutcome.Neutral);
            },
            "SessionStart",
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_PayloadIsMalformedJson()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader("{ this is not valid json");
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await CodexHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => throw new InvalidOperationException("must not be reached"),
            "SessionStart",
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_HandlerThrows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(CodexHookFixtures.Read("payload.session-end.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await CodexHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            "SessionEnd",
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheEntryTimeoutElapses()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(CodexHookFixtures.Read("payload.session-end.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await CodexHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return CodexHookOutcome.Neutral; },
            "SessionEnd",
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteHookSpecificOutput_When_HandlerReturnsAdditionalContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(CodexHookFixtures.Read("payload.user-prompt-submit.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await CodexHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => Task.FromResult(new CodexHookOutcome { AdditionalContext = "nitro mail: 1 unread message." }),
            "UserPromptSubmit",
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            """{"hookSpecificOutput":{"hookEventName":"UserPromptSubmit","additionalContext":"nitro mail: 1 unread message."}}""",
            output.ToString().Trim());
    }

    [Theory]
    [InlineData("payload.session-start.json", "session-1", "/workspace/project")]
    [InlineData("payload.user-prompt-submit.json", "session-1", "/workspace/project")]
    [InlineData("payload.session-end.json", "session-1", "/workspace/project")]
    public async Task RunAsync_Should_ParseTheFixture_Into_TheExpectedPayload(
        string fixtureFile, string expectedSessionId, string expectedCwd)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(CodexHookFixtures.Read(fixtureFile));
        var output = new StringWriter();
        var error = new StringWriter();
        CodexHookPayload? captured = null;

        await CodexHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(CodexHookOutcome.Neutral);
            },
            "SessionStart",
            cancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(expectedSessionId, captured.SessionId);
        Assert.Equal(expectedCwd, captured.Cwd);
    }
}
