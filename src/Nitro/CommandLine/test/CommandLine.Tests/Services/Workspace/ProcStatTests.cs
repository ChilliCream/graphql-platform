using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ProcStat.ReadStartTicks"/> against the real
/// <c>/proc</c> filesystem. The command <c>sed 's/.*) //'
/// /proc/&lt;pid&gt;/stat | awk '{print $20}'</c> against this process's own
/// pid matches the value this method returns.
/// </summary>
public sealed class ProcStatTests
{
    [Fact]
    public void ReadStartTicks_Should_ReturnADigitString_For_CurrentProcess()
    {
        // arrange
        using var self = Process.GetCurrentProcess();

        // act
        var startTicks = ProcStat.ReadStartTicks(self.Id);

        // assert
        Assert.NotNull(startTicks);
        Assert.Matches("^[0-9]+$", startTicks);
    }

    [Fact]
    public void ReadStartTicks_Should_ReturnTheSameValue_When_CalledTwice()
    {
        // arrange
        using var self = Process.GetCurrentProcess();

        // act
        var first = ProcStat.ReadStartTicks(self.Id);
        var second = ProcStat.ReadStartTicks(self.Id);

        // assert: this process's own start ticks do not change mid-run.
        Assert.Equal(first, second);
    }

    [Fact]
    public void ReadStartTicks_Should_ReturnNull_When_NoProcessHasThatPid()
    {
        // act
        var startTicks = ProcStat.ReadStartTicks(999_999);

        // assert
        Assert.Null(startTicks);
    }
}
