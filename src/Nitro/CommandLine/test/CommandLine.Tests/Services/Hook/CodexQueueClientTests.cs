using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises the <see cref="ProcessStartInfo"/> built for the <c>codex
/// queue</c> subprocess directly: it must strip the actor identity a real
/// invocation would otherwise inherit and suppress the child's own hook
/// re-entry, so a queued message's own notify firing is never misattributed
/// to the actor that queued it and never re-triggers a ping of its own.
/// </summary>
public sealed class CodexQueueClientTests
{
    [Fact]
    public void BuildStartInfo_Should_StripActorEnvAndSuppressHooks()
    {
        // act
        var startInfo = CodexQueueClient.BuildStartInfo("thread-1", "digest");

        // assert
        Assert.False(startInfo.Environment.ContainsKey("NITRO_MAIL_ACTOR"));
        Assert.False(startInfo.Environment.ContainsKey("NITRO_TASK_ACTOR"));
        Assert.Equal("1", startInfo.Environment["NITRO_HOOK_SUPPRESS"]);
    }
}
