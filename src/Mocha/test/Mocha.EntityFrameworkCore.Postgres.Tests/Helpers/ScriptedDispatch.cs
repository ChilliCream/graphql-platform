using System.Collections.Concurrent;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

/// <summary>
/// A dispatch endpoint in front of the transport that lets a test decide what each dispatch attempt does.
/// It sees the dispatches of the outbox relay and the scheduler, which do not run the dispatch middleware.
/// </summary>
internal sealed class ScriptedDispatch(Func<DispatchAttempt, ValueTask> onDispatch)
{
    private readonly ConcurrentDictionary<string, int> _attempts = new();

    /// <summary>
    /// Gets the ids of the messages the transport accepted, one entry per accepted dispatch.
    /// </summary>
    public ConcurrentQueue<string> Accepted { get; } = new();

    public int AttemptsOf(string messageId) => _attempts.GetValueOrDefault(messageId);

    /// <summary>
    /// Replaces the endpoint of the started <paramref name="runtime"/> that publishes
    /// <typeparamref name="TMessage"/> with an endpoint that runs the script for each dispatch to its address.
    /// </summary>
    public void InterceptPublishEndpoint<TMessage>(MessagingRuntime runtime)
    {
        var endpoint = runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(TMessage)));
        var scripted = new ScriptedDispatchEndpoint(this, endpoint);
        scripted.Initialize(runtime, new DispatchEndpointConfiguration { Name = endpoint.Name });
        scripted.Complete(runtime);
        runtime.Endpoints.Remove(endpoint);
    }

    private ValueTask DispatchAsync(IDispatchContext context, DispatchDelegate next)
    {
        var messageId = context.Envelope!.MessageId!;
        var number = _attempts.AddOrUpdate(messageId, 1, static (_, attempts) => attempts + 1);

        return onDispatch(new DispatchAttempt(this, context, next, messageId, number));
    }

    // Takes over the address of the replaced endpoint and passes each dispatch to the script, which can hand it
    // to the replaced endpoint's transport.
    private sealed class ScriptedDispatchEndpoint(ScriptedDispatch dispatch, DispatchEndpoint endpoint)
        : DispatchEndpoint(endpoint.Transport)
    {
        protected override void OnInitialize(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        {
            Address = endpoint.Address;
            Destination = endpoint.Destination;
        }

        protected override void OnComplete(
            IMessagingConfigurationContext context,
            DispatchEndpointConfiguration configuration)
        {
        }

        protected override ValueTask DispatchAsync(IDispatchContext context)
            => dispatch.DispatchAsync(context, endpoint.DispatchInternalAsync);
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

    /// <summary>
    /// Hands the message to the transport.
    /// </summary>
    public async ValueTask SendAsync()
    {
        await next(context);
        dispatch.Accepted.Enqueue(messageId);
    }
}
