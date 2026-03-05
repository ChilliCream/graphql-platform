using Mocha;
using Xunit;

namespace Mocha.Sagas.Tests;

/// <summary>
/// A "plan" for executing a series of saga tests in a fluent manner.
/// </summary>
public sealed class SagaTestPlan<T> where T : SagaStateBase
{
    private readonly List<Func<SagaTester<T>, Task>> _steps = new();

    public SagaTester<T> Tester { get; }

    private T? _lastState;

    public object? LastMessage { get; private set; }

    public SagaTestPlan(SagaTester<T> tester)
    {
        Tester = tester;
    }

    public SagaTestPlan<T> WithState(T state)
    {
        Tester.SetState(state);
        return this;
    }

    /// <summary>
    /// Add a new step (function) to the plan. Each step is passed the <see cref="SagaTester{T}"/>.
    /// </summary>
    public SagaTestPlan<T> AddStep(Func<SagaTester<T>, Task> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Runs all planned steps in sequence, awaiting each one before moving on.
    /// </summary>
    public async Task RunAll()
    {
        foreach (var step in _steps)
        {
            _lastState = Tester.State ?? _lastState;
            await step(Tester);
        }
    }

    public SagaTestPlan<T> SetState(string state)
    {
        return AddStep(async tester =>
        {
            tester.State!.State = state;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Queues a step that sends an event to the saga.
    /// </summary>
    public SagaTestPlan<T> On(object @event)
    {
        return AddStep(tester => tester.ExecuteAsync(@event));
    }

    // Some people prefer a "ThenOn(...)" for clarity:
    public SagaTestPlan<T> ThenOn(object @event)
    {
        return On(@event);
    }

    /// <summary>
    /// Queues a step that asserts the Saga's internal state matches `expected`.
    /// </summary>
    public SagaTestPlan<T> ExpectState(string expected)
    {
        return AddStep(async tester =>
        {
            Assert.NotNull(_lastState);
            Assert.Equal(expected, _lastState.State);
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Queues a step that checks we sent exactly one `TMessage`
    /// and that it satisfies the given predicate.
    /// </summary>
    public SagaTestPlan<T> ExpectSendMessage<TMessage>(Action<T, TMessage> assert)
    {
        return AddStep(async tester =>
        {
            var message = tester.ExpectSentMessage<TMessage>();
            LastMessage = message;
            Assert.NotNull(_lastState);
            assert(_lastState, message);

            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Simpler version without predicate.
    /// </summary>
    public SagaTestPlan<T> ExpectSendMessage<TMessage>()
    {
        return AddStep(async tester =>
        {
            LastMessage = tester.ExpectSentMessage<TMessage>();
            await Task.CompletedTask;
        });
    }

    public SagaTestPlan<T> ExpectReplyMessage<TMessage>(Action<T, TMessage> assert)
    {
        return AddStep(async tester =>
        {
            var message = tester.ExpectReplyMessage<TMessage>();
            LastMessage = message;
            assert(_lastState!, message);

            await Task.CompletedTask;
        });
    }

    public SagaTestPlan<T> ExpectReplyMessage<TMessage>()
    {
        return AddStep(async tester =>
        {
            LastMessage = tester.ExpectReplyMessage<TMessage>();
            await Task.CompletedTask;
        });
    }

    public SagaTestPlan<T> ExpectCompletion()
    {
        return AddStep(async tester =>
        {
            tester.ExpectCompleted();
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Queues a step that checks exactly one message of type TMessage was published.
    /// </summary>
    public SagaTestPlan<T> ExpectPublishedMessage<TMessage>()
    {
        return AddStep(async tester =>
        {
            LastMessage = tester.ExpectPublishedMessage<TMessage>();
            await Task.CompletedTask;
        });
    }

    public SagaTestPlan<T> ExpectPublishedMessage<TMessage>(Action<T, TMessage> assert)
    {
        return AddStep(async tester =>
        {
            var message = tester.ExpectPublishedMessage<TMessage>();
            LastMessage = message;
            assert(_lastState!, message);
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Queues a step that checks the <see cref="SendOptions"/> for the given message.
    /// </summary>
    public SagaTestPlan<T> ExpectSendOptions(Action<SendOptions> assert)
    {
        return AddStep(async tester =>
        {
            Assert.NotNull(LastMessage);
            var options = tester.ExpectSendOptions(LastMessage);
            assert(options);
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Queues a step that checks there are no options for the given message.
    /// </summary>
    public SagaTestPlan<T> ExpectNoOptions<TMessage>(TMessage message) where TMessage : class
    {
        return AddStep(async tester =>
        {
            tester.ExpectNoOptions(message);
            await Task.CompletedTask;
        });
    }

    public SagaTestPlan<T> WithDefaultMetadata()
    {
        return AddStep(async tester =>
        {
            var state = tester.State!;
            state.Metadata.Set(SagaContextData.ReplyAddress, $"queue://test/{SagaTester.Defaults.ReplyEndpoint}");
            state.Metadata.Set(SagaContextData.CorrelationId, SagaTester.Defaults.CorrelationId.ToString());
            await Task.CompletedTask;
        });
    }
}
