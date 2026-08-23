using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ProcessInfoProvider"/>, in particular
/// <see cref="ProcessInfoProvider.IsAlive"/>'s tolerance-based generation
/// match: on Linux, .NET derives <see cref="Process.StartTime"/> from an
/// estimated boot time, so two processes reading the StartTime of the same
/// live pid can observe sub-millisecond jitter, which means exact equality
/// is too strict. The SQLite TEXT round trip that reads a stored
/// <c>proc_start</c> back as a <see cref="DateTimeOffset"/> is lossless with
/// the pinned Microsoft.Data.Sqlite version (it formats with an explicit
/// offset, unlike older versions that dropped it and forced a
/// local-timezone reparse), but that does not remove the OS-side jitter, so
/// a small tolerance is still required. A start time beyond that tolerance -
/// the pid-reuse hazard <c>proc_start</c> exists to catch - must still NOT
/// be treated as the same generation.
/// </summary>
public sealed class ProcessInfoProviderTests
{
    private readonly ProcessInfoProvider _provider = new();

    [Fact]
    public void GetStartTime_Should_ReturnLiveValue_For_CurrentProcess()
    {
        // arrange
        using var self = Process.GetCurrentProcess();

        // act
        var startTime = _provider.GetStartTime(self.Id);

        // assert
        Assert.NotNull(startTime);
        Assert.Equal(self.StartTime.ToUniversalTime(), startTime);
    }

    [Fact]
    public void GetStartTime_Should_ReturnNull_When_NoProcessHasThatPid()
    {
        // act
        var startTime = _provider.GetStartTime(999_999);

        // assert
        Assert.Null(startTime);
    }

    [Fact]
    public void IsAlive_Should_ReturnTrue_When_PidIsRunning_And_StartTimeMatchesExactly()
    {
        // arrange
        using var self = Process.GetCurrentProcess();
        var expectedStart = self.StartTime.ToUniversalTime();

        // act & assert
        Assert.True(_provider.IsAlive(self.Id, expectedStart));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(1999)]
    [InlineData(-1999)]
    public void IsAlive_Should_ReturnTrue_When_StartTimeIsOffBy_LessThanTolerance(
        int millisecondsOffset)
    {
        // arrange: cross-process reads of the same live pid's StartTime can
        // jitter by sub-millisecond amounts on Linux, so offsets within the
        // tolerance must still be treated as the same generation.
        using var self = Process.GetCurrentProcess();
        var jitteredStart = self.StartTime.ToUniversalTime().AddMilliseconds(millisecondsOffset);

        // act & assert
        Assert.True(_provider.IsAlive(self.Id, jitteredStart));
    }

    [Theory]
    [InlineData(2001)]
    [InlineData(-2001)]
    [InlineData(5000)]
    [InlineData(-5000)]
    public void IsAlive_Should_ReturnFalse_When_StartTimeIsOffBy_MoreThanTolerance(
        int millisecondsOffset)
    {
        // arrange: a start time beyond the tolerance is exactly the
        // pid-reuse ambiguity proc_start exists to close - it must NOT be
        // treated as the same generation.
        using var self = Process.GetCurrentProcess();
        var nearMissStart = self.StartTime.ToUniversalTime().AddMilliseconds(millisecondsOffset);

        // act & assert
        Assert.False(_provider.IsAlive(self.Id, nearMissStart));
    }

    [Fact]
    public void IsAlive_Should_ReturnFalse_When_NoProcessHasThatPid()
    {
        // act & assert
        Assert.False(_provider.IsAlive(999_999, DateTimeOffset.UtcNow));
    }
}
