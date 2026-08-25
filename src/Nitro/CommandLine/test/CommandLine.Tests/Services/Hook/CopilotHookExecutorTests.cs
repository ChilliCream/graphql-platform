using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotHookExecutor"/>'s fail-open envelope directly
/// against <see cref="StringReader"/>/<see cref="StringWriter"/>, mirroring
/// <c>CodexHookExecutorTests</c>: the suppression short-circuit, every
/// failure path resolving to the neutral <c>{}</c> response, the flat (not
/// <c>hookSpecificOutput</c>-nested) response shape, and successful parsing
/// of Copilot payload fixtures.
/// </summary>
public sealed class CopilotHookExecutorTests
{
    [Fact]
    public async Task RunAsync_Should_WriteNeutralWithoutInvokingTheHandler_When_SuppressEnvVarIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var input = new StringReader(CopilotHookFixtures.Read("payload.session-start.json"));
        var output = new StringWriter();
        var handlerInvoked = false;

        var exitCode = await CopilotHookExecutor.RunAsync(
            environmentVariables,
            input,
            output,
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(CopilotHookOutcome.Neutral);
            },
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

        var exitCode = await CopilotHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("must not be reached"),
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_HandlerThrows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(
            CopilotHookFixtures.Read("payload.session-end.json"));
        var output = new StringWriter();

        var exitCode = await CopilotHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheEntryTimeoutElapses()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(
            CopilotHookFixtures.Read("payload.session-end.json"));
        var output = new StringWriter();

        var exitCode = await CopilotHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return CopilotHookOutcome.Neutral; },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteAFlatAdditionalContext_When_HandlerReturnsOne()
    {
        // The response envelope is flat, not nested under hookSpecificOutput
        // like Claude and Codex. See CopilotHookResponse.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(
            CopilotHookFixtures.Read("payload.session-start.json"));
        var output = new StringWriter();

        var exitCode = await CopilotHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => Task.FromResult(new CopilotHookOutcome { AdditionalContext = "nitro mail: 1 unread message." }),
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            """{"additionalContext":"nitro mail: 1 unread message."}""",
            output.ToString().Trim());
    }

    [Theory]
    [InlineData(
        "payload.session-start.json",
        "session-1")]
    [InlineData(
        "payload.user-prompt-submitted.json",
        "session-1")]
    [InlineData(
        "payload.session-end.json",
        "session-2")]
    public async Task RunAsync_Should_ParseTheFixture_Into_TheExpectedPayload(
        string fixtureFile, string expectedSessionId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(CopilotHookFixtures.Read(fixtureFile));
        var output = new StringWriter();
        CopilotHookPayload? captured = null;

        await CopilotHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(CopilotHookOutcome.Neutral);
            },
            cancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(expectedSessionId, captured.SessionId);
        Assert.NotNull(captured.Cwd);
    }
}
