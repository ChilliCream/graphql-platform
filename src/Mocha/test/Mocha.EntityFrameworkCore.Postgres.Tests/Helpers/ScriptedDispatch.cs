using System.Collections.Concurrent;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

/// <summary>
/// A dispatch middleware in front of the transport that lets a test decide what each dispatch attempt does.
/// </summary>
internal sealed class ScriptedDispatch(Func<DispatchAttempt, ValueTask> onDispatch)
{
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    /// <summary>
    /// Gets the ids of the messages the transport accepted, one entry per accepted dispatch.
    /// </summary>
    public ConcurrentQueue<string> Accepted { get; } = new();

    public DispatchMiddlewareConfiguration Middleware
        => new((_, next) => context => DispatchAsync(context, next), "ScriptedDispatch");

    public int AttemptsOf(string messageId) => _attempts.GetValueOrDefault(messageId);

    private ValueTask DispatchAsync(IDispatchContext context, DispatchDelegate next)
    {
        var messageId = context.Envelope!.MessageId!;
        var number = _attempts.AddOrUpdate(messageId, 1, static (_, attempts) => attempts + 1);

        return onDispatch(new DispatchAttempt(this, context, next, messageId, number));
    }
}

/// <summary>
/// One dispatch of a message as the <see cref="ScriptedDispatch"/> sees it.
/// </summary>
internal sealed class DispatchAttempt(
    ScriptedDispatch dispatch,
    IDispatchContext context,
    DispatchDelegate next,
    string messageId,
    int number)
{
    public string MessageId => messageId;

    /// <summary>
    /// Gets the number of this dispatch of the message, starting at one.
    /// </summary>
    public int Number => number;

    public CancellationToken CancellationToken => context.CancellationToken;

    /// <summary>
    /// Hands the message to the transport.
    /// </summary>
    public async ValueTask SendAsync()
    {
        await next(context);
        dispatch.Accepted.Enqueue(messageId);
    }

    /// <summary>
    /// Waits until the dispatch is cancelled or <paramref name="release"/> completes, and returns without an
    /// error in both cases, as a transport does that has already handed the message to its broker.
    /// </summary>
    public async Task WaitForCancellationAsync(Task release)
    {
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = CancellationToken.Register(() => cancelled.TrySetResult());
        await Task.WhenAny(cancelled.Task, release);
    }
}
