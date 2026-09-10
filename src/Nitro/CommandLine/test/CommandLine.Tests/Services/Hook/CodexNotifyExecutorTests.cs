using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexNotifyExecutor"/>'s fail-open envelope and its
/// distinguishing feature versus every other adapter in this namespace: the
/// exit code carries the foreign-wrapping contract instead of always being
/// success.
/// </summary>
public sealed class CodexNotifyExecutorTests
{
    private const string PayloadJson = """{"type":"agent-turn-complete","thread-id":"t-1","cwd":"/work"}""";

    [Fact]
    public async Task RunAsync_Should_ExecForeignAndReturnItsExitCode_When_OneIsConfigured()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const int foreignExitCode = 0;
        var foreignInvocations = 0;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => Task.FromResult(new CodexNotifyOutcome { Queued = true }),
            _ => { foreignInvocations++; return Task.FromResult<int?>(foreignExitCode); },
            PayloadJson,
            cancellationToken);

        Assert.Equal(foreignExitCode, exitCode);
        Assert.Equal(1, foreignInvocations);
    }

    [Fact]
    public async Task RunAsync_Should_PropagateANonzeroForeignExitCode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => Task.FromResult(CodexNotifyOutcome.Neutral),
            _ => Task.FromResult<int?>(17),
            PayloadJson,
            cancellationToken);

        Assert.Equal(17, exitCode);
    }

    [Fact]
    public async Task RunAsync_Should_ReturnZero_When_NoForeignProgramIsConfigured()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => Task.FromResult(CodexNotifyOutcome.Neutral),
            _ => Task.FromResult<int?>(null),
            PayloadJson,
            cancellationToken);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_Should_StillExecForeign_When_OurOwnPayloadIsMalformed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var foreignInvocations = 0;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => throw new InvalidOperationException("must not be reached"),
            _ => { foreignInvocations++; return Task.FromResult<int?>(0); },
            "{ not valid json",
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(1, foreignInvocations);
    }

    [Fact]
    public async Task RunAsync_Should_StillExecForeign_When_OurOwnHandlerThrows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var foreignInvocations = 0;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            _ => { foreignInvocations++; return Task.FromResult<int?>(3); },
            PayloadJson,
            cancellationToken);

        Assert.Equal(3, exitCode);
        Assert.Equal(1, foreignInvocations);
    }

    [Fact]
    public async Task RunAsync_Should_StillExecForeign_When_OurOwnHandlerHangsPastTheEntryTimeout()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var foreignInvocations = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            async (_, _) => { await Task.Delay(Timeout.InfiniteTimeSpan); return CodexNotifyOutcome.Neutral; },
            _ => { foreignInvocations++; return Task.FromResult<int?>(0); },
            PayloadJson,
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        stopwatch.Stop();

        Assert.Equal(0, exitCode);
        Assert.Equal(1, foreignInvocations);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"expected RunAsync to return near the 50ms timeout, took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task RunAsync_Should_StillRunOurOwnWork_When_ExecForeignItselfThrows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ourWorkInvoked = false;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            (_, _) => { ourWorkInvoked = true; return Task.FromResult(CodexNotifyOutcome.Neutral); },
            _ => throw new InvalidOperationException("simulated spawn failure"),
            PayloadJson,
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.True(ourWorkInvoked);
    }

    [Fact]
    public async Task RunAsync_Should_StillExecForeign_When_Suppressed()
    {
        // The NITRO_HOOK_SUPPRESS reentrancy guard is about OUR OWN mail
        // work re-entering through a spawned relay; it must never suppress
        // the operator's originally-configured foreign notify program.
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var ourWorkInvoked = false;
        var foreignInvocations = 0;

        var exitCode = await CodexNotifyExecutor.RunAsync(
            environmentVariables,
            (_, _) => { ourWorkInvoked = true; return Task.FromResult(CodexNotifyOutcome.Neutral); },
            _ => { foreignInvocations++; return Task.FromResult<int?>(0); },
            PayloadJson,
            cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.False(ourWorkInvoked);
        Assert.Equal(1, foreignInvocations);
    }
}
