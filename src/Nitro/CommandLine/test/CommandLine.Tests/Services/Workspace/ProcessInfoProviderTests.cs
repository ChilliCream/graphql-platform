using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ProcessInfoProvider"/>, in particular
/// <see cref="ProcessInfoProvider.IsAlive"/>'s exact-equality generation
/// match: the SQLite TEXT round trip that reads a stored <c>proc_start</c>
/// back as a <see cref="DateTimeOffset"/> is lossless with the pinned
/// Microsoft.Data.Sqlite version (it formats with an explicit offset, unlike
/// older versions that dropped it and forced a local-timezone reparse), so
/// no fuzzy tolerance is needed, and a near-miss start time - the exact
/// hazard <c>proc_start</c> exists to catch - must NOT be treated as the
/// same generation.
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
    [InlineData(2000)]
    [InlineData(-2000)]
    public void IsAlive_Should_ReturnFalse_When_StartTimeIsOffByAnyAmount_IncludingWithinTheOldTolerance(
        int millisecondsOffset)
    {
        // arrange: a near-miss start time is exactly the pid-reuse ambiguity
        // proc_start exists to close - a fuzzy tolerance here would silently
        // treat a different process generation as the one this row expects.
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
