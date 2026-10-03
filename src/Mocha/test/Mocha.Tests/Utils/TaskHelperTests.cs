using CookieCrumble;

namespace Mocha.Tests.Utils;

public sealed class TaskHelperTests
{
    [Fact]
    public async Task WhenAllAsync_Should_ThrowEveryFailure_When_SeveralTasksFail()
    {
        // arrange
        Task[] tasks =
        [
            Task.CompletedTask,
            FailAsync(new InvalidOperationException("first stop failed")),
            FailAsync(new InvalidOperationException("second stop failed")),
            CancelAsync(new OperationCanceledException("stop cancelled"))
        ];

        // act
        var exception = await Assert.ThrowsAsync<AggregateException>(() => TaskHelper.WhenAllAsync(tasks));

        // assert
        exception.InnerExceptions.Select(e => $"{e.GetType().Name}: {e.Message}").MatchInlineSnapshot(
            """
            [
              "InvalidOperationException: first stop failed",
              "InvalidOperationException: second stop failed"
            ]
            """);
    }

    [Fact]
    public async Task WhenAllAsync_Should_RethrowOriginalException_When_OneTaskFails()
    {
        // arrange
        Task[] tasks =
        [
            Task.CompletedTask,
            FailAsync(new InvalidOperationException("stop failed")),
            CancelAsync(new OperationCanceledException("stop cancelled"))
        ];

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => TaskHelper.WhenAllAsync(tasks));

        // assert
        Assert.Equal("stop failed", exception.Message);
    }

    private static async Task FailAsync(Exception exception)
    {
        await Task.Yield();
        throw exception;
    }

    private static async Task CancelAsync(OperationCanceledException exception)
    {
        await Task.Yield();
        throw exception;
    }
}
