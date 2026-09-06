using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises the <see cref="ProcessStartInfo"/> built for the
/// <c>codex queue</c> subprocess directly: it must strip the actor identity a real
/// invocation would otherwise inherit and suppress the child's own hook
/// re-entry, so a queued message's own notify firing is never misattributed
/// to the actor that queued it and never re-triggers a ping of its own.
/// </summary>
public sealed class CodexQueueClientTests
{
    [Fact]
    public void BuildStartInfo_Should_SuppressHooks()
    {
        // act
        var startInfo = CodexQueueClient.BuildStartInfo("thread-1", "digest");

        // assert
        Assert.Equal("1", startInfo.Environment["NITRO_HOOK_SUPPRESS"]);
    }

    [Fact]
    public void MapResult_Should_ReturnOk_When_ExitCodeIsZero()
    {
        // act
        var result = CodexQueueClient.MapResult(0, stderr: "");

        // assert
        Assert.Equal(CodexQueueResult.Ok, result);
    }

    /// <summary>
    /// Signature for a well-formed but nonexistent Codex thread id.
    /// </summary>
    [Fact]
    public void MapResult_Should_ReturnEndpointGone_When_StderrIsTheNoRolloutSignature()
    {
        // arrange
        var stderr = CodexHookFixtures.Read("stderr.queue-gone-thread.txt");

        // act
        var result = CodexQueueClient.MapResult(1, stderr);

        // assert
        Assert.Equal(CodexQueueResult.EndpointGone, result);
    }

    /// <summary>
    /// Signature for a malformed (non-UUID) thread id. Not expected to fire
    /// in production because the stored
    /// thread id always comes from Codex's own notify payload - but matched
    /// defensively since it is an equally unambiguous "no such session"
    /// report.
    /// </summary>
    [Fact]
    public void MapResult_Should_ReturnEndpointGone_When_StderrIsTheNoActiveSessionSignature()
    {
        // arrange
        var stderr = CodexHookFixtures.Read("stderr.queue-malformed-thread.txt");

        // act
        var result = CodexQueueClient.MapResult(1, stderr);

        // assert
        Assert.Equal(CodexQueueResult.EndpointGone, result);
    }

    /// <summary>
    /// Signature for an unrelated queue failure: a nonzero exit that must not
    /// be misclassified as a gone thread.
    /// </summary>
    [Fact]
    public void MapResult_Should_ReturnError_When_StderrIsAnUnrelatedFailure()
    {
        // arrange
        var stderr = CodexHookFixtures.Read("stderr.queue-unrelated-failure.txt");

        // act
        var result = CodexQueueClient.MapResult(1, stderr);

        // assert
        Assert.Equal(CodexQueueResult.Error, result);
    }

    [Fact]
    public void MapResult_Should_ReturnError_When_ExitCodeIsNonzeroAndStderrIsEmpty()
    {
        // act
        var result = CodexQueueClient.MapResult(1, stderr: "");

        // assert
        Assert.Equal(CodexQueueResult.Error, result);
    }
}
