using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHookExecutor"/>'s fail-open envelope directly
/// against <see cref="StringReader"/>/<see cref="StringWriter"/>, without
/// System.CommandLine or DI: the suppression short-circuit, every failure
/// path resolving to the neutral <c>{}</c> response, and successful
/// translation of the captured Claude payload fixtures into the harness's
/// wire shape.
/// </summary>
public sealed class ClaudeHookExecutorTests
{
    [Fact]
    public async Task RunAsync_Should_WriteNeutralWithoutInvokingTheHandler_When_SuppressEnvVarIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var input = new StringReader(HookFixtures.Read("session-start.json"));
        var output = new StringWriter();
        var handlerInvoked = false;

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            environmentVariables,
            input,
            output,
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(ClaudeHookOutcome.Neutral);
            },
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_PayloadIsMalformedJson()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("malformed.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("must not be reached"),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_HandlerThrows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheEntryTimeoutElapses()
    {
        // arrange: an explicit short timeout stands in for the real 10s
        // entry ceiling so this test does not have to wait it out.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return ClaudeHookOutcome.Neutral; },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteHookSpecificOutput_When_HandlerReturnsAdditionalContext()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("user-prompt-submit.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => Task.FromResult(new ClaudeHookOutcome { AdditionalContext = "nitro mail: 1 unread message." }),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(
            """{"hookSpecificOutput":{"additionalContext":"nitro mail: 1 unread message."}}""",
            output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteBlockDecision_When_HandlerReturnsBlock()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => Task.FromResult(new ClaudeHookOutcome { Block = true, BlockReason = "unread mail" }),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("""{"decision":"block","reason":"unread mail"}""", output.ToString().Trim());
    }

    [Theory]
    [InlineData("session-start.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("user-prompt-submit.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("stop.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("stop-reentrant.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", true)]
    [InlineData("session-end.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    public async Task RunAsync_Should_ParseTheCapturedFixture_Into_TheExpectedPayload(
        string fixtureFile, string expectedSessionId, string expectedCwd, bool expectedStopHookActive)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read(fixtureFile));
        var output = new StringWriter();
        ClaudeHookPayload? captured = null;

        // act
        await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(ClaudeHookOutcome.Neutral);
            },
            cancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.Equal(expectedSessionId, captured.SessionId);
        Assert.Equal(expectedCwd, captured.Cwd);
        Assert.Equal(expectedStopHookActive, captured.StopHookActive);
    }
}
