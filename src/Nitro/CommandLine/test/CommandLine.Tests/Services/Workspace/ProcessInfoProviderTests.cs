using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ProcessInfoProvider"/>: <see
/// cref="ProcessInfoProvider.IsAlive"/> and the non-legacy branch of <see
/// cref="ProcessInfoProvider.Observe"/> compare raw kernel start ticks by
/// exact string equality, no tolerance - a pid-reuse hazard whose new
/// process happens to start within the same wall-clock second (or even
/// millisecond) as the row it replaced is still classified dead, because its
/// ticks differ, and no amount of boot-time-estimate drift can misclassify a
/// live process as dead the way the old DateTimeOffset-with-tolerance
/// comparison could. The legacy branch (<c>legacy: true</c>) preserves that
/// old wall-clock-with-tolerance rule verbatim, for a row a v5-to-v6
/// migration marked <c>proc_start_legacy</c> until its own next SessionStart
/// rewrites it with fresh ticks.
/// </summary>
public sealed class ProcessInfoProviderTests
{
    private readonly ProcessInfoProvider _provider = new();

    [Fact]
    public void GetStartTicks_Should_ReturnLiveValue_For_CurrentProcess()
    {
        // arrange
        using var self = Process.GetCurrentProcess();

        // act
        var startTicks = _provider.GetStartTicks(self.Id);

        // assert
        Assert.Equal(ProcStat.ReadStartTicks(self.Id), startTicks);
    }

    [Fact]
    public void GetStartTicks_Should_ReturnNull_When_NoProcessHasThatPid()
    {
        // act
        var startTicks = _provider.GetStartTicks(999_999);

        // assert
        Assert.Null(startTicks);
    }

    [Fact]
    public void IsAlive_Should_ReturnTrue_When_PidIsRunning_And_TicksMatchExactly()
    {
        // arrange
        using var self = Process.GetCurrentProcess();
        var expectedTicks = ProcStat.ReadStartTicks(self.Id);

        // act & assert
        Assert.True(_provider.IsAlive(self.Id, expectedTicks!));
    }

    [Fact]
    public void IsAlive_Should_ReturnFalse_When_NoProcessHasThatPid()
    {
        // act & assert
        Assert.False(_provider.IsAlive(999_999, "1"));
    }

    [Fact]
    public void IsAlive_Should_ReturnFalse_When_PidReused_WithDifferentTicks_DespiteIdenticalWallClockStart()
    {
        // arrange: the exact hazard raw ticks exist to close - a reused pid
        // whose new process happens to start within the same wall-clock
        // second (or even millisecond) as the generation it replaced must
        // still be classified as a different generation, because ticks
        // compare by exact string equality with no tolerance window at all.
        var provider = new ProcessInfoProvider(startTicksReader: _ => "39270330");

        // act & assert
        Assert.False(provider.IsAlive(107466, "39270331"));
    }

    [Fact]
    public void GetProcessScope_Should_ReturnAPidNamespaceValue_When_RunningOnLinux()
    {
        // arrange & act
        var scope = _provider.GetProcessScope();

        // assert
        if (OperatingSystem.IsLinux())
        {
            Assert.Matches("^pidns:[0-9]+$", scope);
        }
        else
        {
            Assert.Equal("", scope);
        }
    }

    [Fact]
    public void GetProcessScope_Should_ReturnTheSameValue_When_CalledTwice()
    {
        // arrange & act
        var first = _provider.GetProcessScope();
        var second = _provider.GetProcessScope();

        // assert: this process's own PID namespace does not change mid-run.
        Assert.Equal(first, second);
    }

    [Fact]
    public void CanObserveScope_Should_ReturnTrue_When_RecordedScopeMatchesReaderScope()
    {
        // arrange
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.True(_provider.CanObserveScope(recordedScope));
    }

    [Fact]
    public void CanObserveScope_Should_ReturnTrue_When_RecordedScopeIsBlank()
    {
        // act & assert: an unknown recorded scope (a row from before
        // process_scope was captured, or a platform with no signal) is not
        // treated as a mismatch.
        Assert.True(_provider.CanObserveScope(""));
    }

    [Fact]
    public void CanObserveScope_Should_ReturnFalse_When_BothScopesAreKnown_AndDiffer()
    {
        // arrange: a known reader scope, so the comparison is exercised on
        // platforms that report none of their own.
        var provider = new ProcessInfoProvider(processScopeReader: () => "pidns:1");

        // act & assert: a positive mismatch between two KNOWN scopes -
        // definite proof this reader cannot trust its own view.
        Assert.False(provider.CanObserveScope("pidns:2"));
    }

    [Fact]
    public void Observe_Should_ReturnAlive_When_PidIsRunning_And_TicksMatch_And_ScopeMatches()
    {
        // arrange
        using var self = Process.GetCurrentProcess();
        var expectedTicks = ProcStat.ReadStartTicks(self.Id)!;
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Alive,
            _provider.Observe(self.Id, expectedTicks, legacy: false, recordedScope));
    }

    [Fact]
    public void Observe_Should_ReturnDead_When_NoProcessHasThatPid_And_RecordedScopeMatchesReaderScope()
    {
        // arrange
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Dead,
            _provider.Observe(999_999, "1", legacy: false, recordedScope));
    }

    [Fact]
    public void Observe_Should_ReturnDead_When_TicksDiffer_DespiteIdenticalWallClockStart()
    {
        // arrange: with a stubbed reader that always returns the same
        // ticks, an expected value one digit off is still an exact-string
        // mismatch, and the non-legacy branch drives Dead purely from that
        // mismatch, with no wall-clock reasoning involved.
        var provider = new ProcessInfoProvider(startTicksReader: _ => "39270330");

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Dead,
            provider.Observe(107466, "39270331", legacy: false, recordedProcessScope: ""));
    }

    [Fact]
    public void Observe_Should_ReturnUnobservable_When_TicksReadIsPermissionDenied()
    {
        // arrange: an unreadable-but-live pid (e.g. a process running as
        // another user) must not be classified Dead and reaped - the
        // permission failure has to surface as Unobservable, the same as
        // the legacy branch already does.
        var provider = new ProcessInfoProvider(
            observeReader: (int _, out bool permissionDenied) =>
            {
                permissionDenied = true;
                return null;
            });

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Unobservable,
            provider.Observe(107466, "1", legacy: false, recordedProcessScope: ""));
    }

    [Fact]
    public void Observe_Should_ReturnUnobservable_When_RecordedScopeDiffersFromReaderScope()
    {
        // arrange: the process is genuinely alive, but a positive scope
        // mismatch (a different PID namespace than the row's writer
        // recorded) means this reader cannot trust that fact.
        using var self = Process.GetCurrentProcess();
        var expectedTicks = ProcStat.ReadStartTicks(self.Id)!;
        var provider = new ProcessInfoProvider(processScopeReader: () => "pidns:1");

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Unobservable,
            provider.Observe(self.Id, expectedTicks, legacy: false, "pidns:2"));
    }

    [Fact]
    public void Observe_Should_FallBackToALivenessCheck_When_RecordedScopeIsBlank()
    {
        // arrange: a row from before process_scope was captured (or a
        // platform with no scope signal) carries no writer-side scope to
        // compare against, so Observe falls back to a direct liveness check
        // exactly like the pre-scope-aware behavior.
        using var self = Process.GetCurrentProcess();
        var expectedTicks = ProcStat.ReadStartTicks(self.Id)!;

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Alive,
            _provider.Observe(self.Id, expectedTicks, legacy: false, recordedProcessScope: ""));
    }

    [Fact]
    public void Observe_Should_ReturnAlive_When_Legacy_And_WallClockStartMatchesExactly()
    {
        // arrange: a row a v5-to-v6 migration marked legacy still carries
        // its pre-v6 DateTimeOffset proc_start, read with the old wall-clock
        // rule until its own next SessionStart rewrites it.
        using var self = Process.GetCurrentProcess();
        var expectedStart = self.StartTime.ToUniversalTime().ToString("O");
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Alive,
            _provider.Observe(self.Id, expectedStart, legacy: true, recordedScope));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(1999)]
    [InlineData(-1999)]
    public void Observe_Should_ReturnAlive_When_Legacy_And_WallClockStartIsOffBy_LessThanTolerance(
        int millisecondsOffset)
    {
        // arrange: cross-process reads of the same live pid's StartTime can
        // jitter by sub-millisecond amounts on Linux, so offsets within the
        // legacy tolerance must still be treated as the same generation.
        using var self = Process.GetCurrentProcess();
        var jitteredStart = self.StartTime.ToUniversalTime().AddMilliseconds(millisecondsOffset).ToString("O");
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Alive,
            _provider.Observe(self.Id, jitteredStart, legacy: true, recordedScope));
    }

    [Theory]
    [InlineData(2001)]
    [InlineData(-2001)]
    [InlineData(5000)]
    [InlineData(-5000)]
    public void Observe_Should_ReturnDead_When_Legacy_And_WallClockStartIsOffBy_MoreThanTolerance(
        int millisecondsOffset)
    {
        // arrange: a start time beyond the legacy tolerance is exactly the
        // pid-reuse ambiguity proc_start exists to close - it must NOT be
        // treated as the same generation.
        using var self = Process.GetCurrentProcess();
        var nearMissStart = self.StartTime.ToUniversalTime().AddMilliseconds(millisecondsOffset).ToString("O");
        var recordedScope = _provider.GetProcessScope();

        // act & assert
        Assert.Equal(
            ProcessObservationResult.Dead,
            _provider.Observe(self.Id, nearMissStart, legacy: true, recordedScope));
    }
}
