using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises the <see cref="ProcessStartInfo"/> built for the detached
/// <c>ping-worker</c> child directly: both the Linux (<c>setsid nohup</c>)
/// and non-Linux fallback shapes must strip the actor identity a real
/// invocation would otherwise inherit and suppress the child's own hook
/// re-entry, so a worker's own mail/hook activity is never misattributed to
/// the actor that spawned it and never re-triggers a ping of its own.
/// </summary>
public sealed class PingWorkerLauncherTests
{
    private static readonly LaunchDescriptor Descriptor = new("nitro", []);
    private static readonly string[] WorkerArgs = ["agent", "ping-worker", "--attempt", "attempt-1"];

    [Fact]
    public void BuildLinuxDetachedStartInfo_Should_StripActorEnvAndSuppressHooks()
    {
        // act
        var startInfo = PingWorkerLauncher.BuildLinuxDetachedStartInfo(Descriptor, WorkerArgs);

        // assert
        Assert.False(startInfo.Environment.ContainsKey("NITRO_MAIL_ACTOR"));
        Assert.False(startInfo.Environment.ContainsKey("NITRO_TASK_ACTOR"));
        Assert.Equal("1", startInfo.Environment["NITRO_HOOK_SUPPRESS"]);
    }

    [Fact]
    public void BuildFallbackStartInfo_Should_StripActorEnvAndSuppressHooks()
    {
        // act
        var startInfo = PingWorkerLauncher.BuildFallbackStartInfo(Descriptor, WorkerArgs);

        // assert
        Assert.False(startInfo.Environment.ContainsKey("NITRO_MAIL_ACTOR"));
        Assert.False(startInfo.Environment.ContainsKey("NITRO_TASK_ACTOR"));
        Assert.Equal("1", startInfo.Environment["NITRO_HOOK_SUPPRESS"]);
    }
}
