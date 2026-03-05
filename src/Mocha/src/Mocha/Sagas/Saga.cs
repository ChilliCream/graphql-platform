using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Features;

namespace Mocha.Sagas;

/// <summary>
/// Base class for all sagas, orchestrating long-running business processes as a state machine
/// driven by incoming messages.
/// </summary>
/// <remarks>
/// A saga correlates multiple messages across time, maintaining persistent state and transitioning
/// through defined states in response to events. Derive from <see cref="Saga{TState}"/> to define
/// concrete state types and transition logic.
/// </remarks>
public abstract partial class Saga : IFeatureProvider
{
    /// <summary>
    /// Creates a new saga instance and initializes its internal consumer.
    /// </summary>
    protected Saga()
    {
        Consumer = new SagaConsumer(this);
    }

    /// <summary>
    /// Gets the consumer that receives and dispatches messages to this saga.
    /// </summary>
    public Consumer Consumer { get; }

    /// <summary>
    /// Gets the feature collection associated with this saga, used to attach cross-cutting capabilities.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Gets the serializer used to persist and restore saga state.
    /// </summary>
    public ISagaStateSerializer StateSerializer { get; protected set; } = null!;

    /// <summary>
    /// Gets the logical name of this saga, used for logging, diagnostics, and state store identification.
    /// </summary>
    public string Name { get; protected set; } = "__Unnamed";

    /// <summary>
    /// Gets the dispatch endpoint used to send response messages when the saga completes a request-reply flow.
    /// </summary>
    public IDispatchEndpoint ResponseEndpoint { get; protected set; } = null!;

    /// <summary>
    /// Gets the CLR type of the state object managed by this saga.
    /// </summary>
    public abstract Type StateType { get; }

    /// <summary>
    /// Gets the dictionary of all configured states in this saga, keyed by state name.
    /// </summary>
    public abstract IReadOnlyDictionary<string, SagaState> States { get; }

    /// <summary>
    /// Processes an incoming message by loading or creating saga state, executing transitions, and persisting the result.
    /// </summary>
    /// <param name="context">The consume context providing the incoming message, headers, and runtime services.</param>
    /// <returns>
    /// <see langword="true"/> if the event was handled by this saga;
    /// <see langword="false"/> if no matching saga instance was found for the correlation identifier.
    /// </returns>
    public abstract Task<bool> HandleEvent(IConsumeContext context);

    /// <summary>
    /// Builds a structural description of this saga, including all states, transitions, published and sent events.
    /// </summary>
    /// <remarks>
    /// This is used for visualization, diagnostics, and tooling support to introspect the saga topology at runtime.
    /// </remarks>
    /// <returns>A <see cref="SagaDescription"/> containing the full state machine definition.</returns>
    public SagaDescription Describe()
    {
        var states = new List<SagaStateDescription>();

        foreach (var (stateName, state) in States)
        {
            var transitions = new List<SagaTransitionDescription>();

            foreach (var (eventType, transition) in state.Transitions)
            {
                transitions.Add(
                    new SagaTransitionDescription(
                        DescriptionHelpers.GetTypeName(eventType),
                        eventType.FullName,
                        transition.TransitionTo,
                        transition.TransitionKind,
                        transition.AutoProvision,
                        transition.Publish.IsEmpty
                            ? null
                            : transition
                                .Publish.Select(p => new SagaEventDescription(
                                    DescriptionHelpers.GetTypeName(p.MessageType),
                                    p.MessageType.FullName))
                                .ToList(),
                        transition.Send.IsEmpty
                            ? null
                            : transition
                                .Send.Select(s => new SagaEventDescription(
                                    DescriptionHelpers.GetTypeName(s.MessageType),
                                    s.MessageType.FullName))
                                .ToList()));
            }

            states.Add(
                new SagaStateDescription(
                    stateName,
                    state.IsInitial,
                    state.IsFinal,
                    state.OnEntry is not null
                        ? new SagaLifeCycleDescription(
                            state.OnEntry.Publish.IsEmpty
                                ? null
                                : state
                                    .OnEntry.Publish.Select(p => new SagaEventDescription(
                                        DescriptionHelpers.GetTypeName(p.MessageType),
                                        p.MessageType.FullName))
                                    .ToList(),
                            state.OnEntry.Send.IsEmpty
                                ? null
                                : state
                                    .OnEntry.Send.Select(s => new SagaEventDescription(
                                        DescriptionHelpers.GetTypeName(s.MessageType),
                                        s.MessageType.FullName))
                                    .ToList())
                        : null,
                    state.Response is not null
                        ? new SagaResponseDescription(
                            DescriptionHelpers.GetTypeName(state.Response.EventType),
                            state.Response.EventType.FullName)
                        : null,
                    transitions));
        }

        return new SagaDescription(
            Name,
            DescriptionHelpers.GetTypeName(StateType),
            StateType.FullName,
            Consumer.Name,
            states);
    }

    /// <summary>
    /// Creates a saga instance using a fluent configuration delegate instead of subclassing.
    /// </summary>
    /// <typeparam name="T">The saga state type, which must derive from <see cref="SagaStateBase"/>.</typeparam>
    /// <param name="configure">A delegate that defines states, transitions, and events for the saga.</param>
    /// <returns>A fully configured <see cref="Saga{TState}"/> instance.</returns>
    public static Saga<T> Create<T>(Action<ISagaDescriptor<T>> configure) where T : SagaStateBase
    {
        return new FluentSaga<T>(configure);
    }

    internal class FluentSaga<TState>(Action<ISagaDescriptor<TState>> configure) : Saga<TState>
        where TState : SagaStateBase
    {
        protected override void Configure(ISagaDescriptor<TState> descriptor)
        {
            configure(descriptor);
        }
    }
}

/// <summary>
/// Strongly-typed saga base class that manages state of type <typeparamref name="TState"/> and routes incoming
/// messages through a configured state machine.
/// </summary>
/// <typeparam name="TState">The saga state type, which must derive from <see cref="SagaStateBase"/>.</typeparam>
/// <remarks>
/// Subclass this type and override <see cref="Configure"/> to define states, transitions, and side-effects.
/// The saga runtime loads or creates <typeparamref name="TState"/>, applies transitions, and persists the
/// state after each message is processed. When a final state is reached, the saga state is deleted and an
/// optional response is sent back to the originator.
/// </remarks>
public abstract partial class Saga<TState> : Saga where TState : SagaStateBase
{
    // TODO -> this should go into a diagnostic listener
    private ILogger<Saga<TState>>? _logger;

    private readonly Action<ISagaDescriptor<TState>> _configure;

    /// <summary>
    /// Creates a new saga instance using the provided configuration delegate to define the state machine.
    /// </summary>
    /// <param name="configure">A delegate that defines states, transitions, and events for the saga.</param>
    protected Saga(Action<ISagaDescriptor<TState>> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Creates a new saga instance that uses the <see cref="Configure"/> method to define the state machine.
    /// </summary>
    protected Saga()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Gets the compiled saga configuration containing the resolved state machine definition.
    /// </summary>
    protected internal SagaConfiguration Configuration { get; private set; } = null!;

    private Dictionary<string, SagaState>? _states;

    private SagaState _initialState = null!;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the saga has not been initialized.</exception>
    public override IReadOnlyDictionary<string, SagaState> States
        => _states ?? throw new InvalidOperationException("Saga is not initialized.");

    /// <inheritdoc />
    public override Type StateType => typeof(TState);

    /// <summary>
    /// Defines the saga state machine by configuring states, transitions, and side-effects on the provided descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor used to declare states and transitions.</param>
    protected abstract void Configure(ISagaDescriptor<TState> descriptor);

    /// <inheritdoc />
    public override async Task<bool> HandleEvent(IConsumeContext context)
    {
        if (_states is null)
        {
            throw new InvalidOperationException("Saga is not initialized.");
        }

        var ct = context.CancellationToken;

        var @event = context.GetMessage();

        using var _ = OpenTelemetry.Source.StartActivity($"Processing {Name}: {@event!.GetType().Name}");

        TState? state;
        if (context.TryGetSagaId(@event, out var correlationId))
        {
            state = await LoadStateAsync(correlationId, context, ct);
            if (state is null)
            {
                return false;
            }

            _logger!.LoadedSagaStateForCorrelationId(Name, correlationId);
        }
        else
        {
            state = CreateState(@event, context);

            _logger!.CreatedSagaState(Name, state.Id);

            await OnEnterStateAsync(state, _initialState, context);
        }

        await OnHandleTransitionAsync(state, @event, context);

        if (_states.TryGetValue(state.State, out var nextState))
        {
            await OnEnterStateAsync(state, nextState, context);

            if (nextState.IsFinal)
            {
                // we need to save the state after the initial state is entered
                return true;
            }
        }

        using var __ = OpenTelemetry.Source.StartActivity("persist saga state");

        await context.GetSagaFeature().Store.SaveAsync(this, state, ct);

        return true;
    }

    /// <summary>
    /// Creates a new saga state instance from the initiating event, setting the initial state and capturing
    /// the reply address and correlation identifier for request-reply flows.
    /// </summary>
    /// <param name="event">The event that initiates the saga.</param>
    /// <param name="context">The consume context providing headers, reply address, and correlation data.</param>
    /// <returns>A new <typeparamref name="TState"/> instance positioned at the initial state.</returns>
    /// <exception cref="SagaExecutionException">
    /// Thrown when no transition is defined for the event type in the initial state, or when the transition
    /// lacks a state factory.
    /// </exception>
    protected virtual TState CreateState(object @event, IConsumeContext context)
    {
        var eventType = @event.GetType();

        using var _ = OpenTelemetry.Source.StartActivity($"Creating Saga by event {eventType.Name}");

        if (!_initialState.Transitions.TryGetValue(eventType, out var transition))
        {
            throw new SagaExecutionException(
                this,
                $"No transition defined for event '{eventType.Name}' in state '{_initialState.State}'.");
        }

        if (transition.StateFactory is null)
        {
            throw new SagaExecutionException(
                this,
                $"No state factory defined for event '{eventType.Name}' in state '{_initialState.State}'.");
        }

        var state = (TState)transition.StateFactory(@event);
        state.State = _initialState.State;

        // we set the reply endpoint of the message into the saga context data. This way a user
        // can request a saga like a normal request reply and will get a response once the saga is
        // completed.
        state.Metadata.Set(SagaContextData.ReplyAddress, context.ResponseAddress?.ToString());
        state.Metadata.Set(SagaContextData.CorrelationId, context.CorrelationId);

        return state;
    }

    /// <summary>
    /// Executes the transition logic for the given event in the saga's current state, updating the state
    /// and dispatching any configured publish or send side-effects.
    /// </summary>
    /// <remarks>
    /// The method walks the event type hierarchy to find a matching transition, allowing base-type transitions
    /// to serve as catch-all handlers. After the transition action mutates the state, configured publish
    /// and send events are dispatched.
    /// </remarks>
    /// <typeparam name="TEvent">The type of the incoming event.</typeparam>
    /// <param name="state">The current saga state instance.</param>
    /// <param name="event">The event triggering the transition.</param>
    /// <param name="context">The consume context providing runtime services and cancellation.</param>
    /// <exception cref="SagaExecutionException">Thrown when no transition is defined for the event type in the current state.</exception>
    protected virtual async Task OnHandleTransitionAsync<TEvent>(TState state, TEvent @event, IConsumeContext context)
        where TEvent : notnull
    {
        var ct = context.CancellationToken;

        var eventType = @event.GetType();

        var currentState = GetCurrentState(state);

        using var _ = OpenTelemetry.Source.StartActivity($"Handle {eventType.Name} in {currentState.State}");

        var firstEvent = eventType;
        SagaTransition? transition;
        while (!currentState.Transitions.TryGetValue(eventType, out transition) && eventType.BaseType is not null)
        {
            eventType = eventType.BaseType;
        }

        if (transition is null)
        {
            throw new SagaExecutionException(
                this,
                $"No transition defined for event '{firstEvent.Name}' in state '{currentState.State}'.");
        }

        _logger!.TransitioningState(Name, currentState.State, eventType.Name);

        transition.Action(state, @event);
        state.State = transition.TransitionTo;

        await PublishEventsAsync(context, transition.Publish, state, ct);
        await SendEventsAsync(context, transition.Send, state, ct);
    }

    /// <summary>
    /// Performs state-entry logic when the saga transitions into a new state, including dispatching
    /// on-entry events and handling final-state completion (response and state deletion).
    /// </summary>
    /// <remarks>
    /// When entering a final state, this method sends the configured response back to the originator
    /// (if a reply address was captured) and deletes the persisted saga state from the store.
    /// </remarks>
    /// <param name="state">The current saga state instance.</param>
    /// <param name="nextState">The state definition being entered.</param>
    /// <param name="context">The consume context providing runtime services and cancellation.</param>
    protected virtual async Task OnEnterStateAsync(TState state, SagaState nextState, IConsumeContext context)
    {
        using var _ = OpenTelemetry.Source.StartActivity($"Enter {nextState.State}");

        var ct = context.CancellationToken;

        _logger!.EnteringState(Name, nextState.State);

        if (nextState.OnEntry is { } onEntry)
        {
            await PublishEventsAsync(context, onEntry.Publish, state, ct);
            await SendEventsAsync(context, onEntry.Send, state, ct);
        }

        if (nextState.IsFinal)
        {
            if (nextState.Response is not null
                && state.Metadata.TryGet(SagaContextData.ReplyAddress, out var replyTo)
                && state.Metadata.TryGet(SagaContextData.CorrelationId, out var correlationId)
                && Uri.TryCreate(replyTo, UriKind.Absolute, out var replyAddress))
            {
                using var __ = OpenTelemetry.Source.StartActivity($"Reply to {replyTo}");

                var response = nextState.Response.Factory(state);

                var options = new ReplyOptions
                {
                    Headers = [],
                    ReplyAddress = replyAddress,
                    CorrelationId = correlationId
                };

                _logger!.ReplyingToSaga(Name, correlationId, replyTo, response.GetType().Name);

                // we do not add the saga-id header to the response, as the saga is already
                // finished
                await context.GetBus().ReplyAsync(response, options, ct);
            }

            await context.GetSagaFeature().Store.DeleteAsync(this, state.Id, ct);

            // TODO this needs to be done differently
            // var cleanup = context.Services.GetRequiredService<ISagaCleanup>();
            // await cleanup.CleanupAsync(context.Saga, state, ct);

            _logger!.SagaCompleted(Name, state.Id);
        }
    }

    /// <summary>
    /// Resolves the <see cref="SagaState"/> definition that corresponds to the saga instance's current state name.
    /// </summary>
    /// <param name="state">The saga state instance whose <see cref="SagaStateBase.State"/> name is looked up.</param>
    /// <returns>The matching <see cref="SagaState"/> definition.</returns>
    /// <exception cref="SagaExecutionException">Thrown when no state definition is found for the current state name.</exception>
    protected virtual SagaState GetCurrentState(TState state)
    {
        if (!_states!.TryGetValue(state.State, out var currentState))
        {
            throw new SagaExecutionException(this, $"No state found for state '{state.State}'.");
        }

        return currentState;
    }

    /// <summary>
    /// Loads a previously persisted saga state from the saga store using the correlation identifier.
    /// </summary>
    /// <param name="correlationId">The unique identifier correlating the incoming message to an existing saga instance.</param>
    /// <param name="context">The consume context providing access to the saga store and runtime services.</param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// The loaded <typeparamref name="TState"/> instance, or <see langword="null"/> if no persisted state
    /// exists for the given correlation identifier.
    /// </returns>
    protected virtual async Task<TState?> LoadStateAsync(
        Guid correlationId,
        IConsumeContext context,
        CancellationToken ct)
    {
        return await context.GetSagaFeature().Store.LoadAsync<TState>(this, correlationId, ct);
    }

    private async Task PublishEventsAsync(
        IConsumeContext context,
        ImmutableArray<SagaEventPublish> publish,
        TState state,
        CancellationToken ct)
    {
        if (publish.Length == 0)
        {
            return;
        }

        using var _ = OpenTelemetry.Source.StartActivity();

        foreach (var trigger in publish)
        {
            var message = trigger.Factory(context, state);

            if (message is null)
            {
                continue;
            }

            var options = trigger.Options.ConfigureOptions is { } configureOptions
                ? configureOptions(context, state)
                : PublishOptions.Default;

            if (options.Headers == null)
            {
                options = options with { Headers = [] };
            }

            options.Headers.Set(SagaContextData.SagaId, state.Id.ToString("D"));

            _logger!.PublishingEvent(Name, message.GetType().Name);

            await context.GetBus().PublishAsync(message, options, ct);
        }
    }

    private async Task SendEventsAsync(
        IConsumeContext context,
        ImmutableArray<SagaEventSend> sends,
        TState state,
        CancellationToken ct)
    {
        if (sends.Length == 0)
        {
            return;
        }

        using var _ = OpenTelemetry.Source.StartActivity();

        foreach (var trigger in sends)
        {
            var message = trigger.Factory(context, state);

            if (message is null)
            {
                continue;
            }

            var options = trigger.Options.ConfigureOptions is { } configureOptions
                ? configureOptions(context, state)
                : SendOptions.Default;

            if (options.Headers == null)
            {
                options = options with { Headers = [] };
            }

            var requestType = context.Runtime.GetMessageType(message.GetType());
            var endpoint = context.Runtime.GetSendEndpoint(requestType);

            options = options with { ReplyEndpoint = endpoint.Transport.ReplyReceiveEndpoint?.Source.Address };

            options.Headers.Set(SagaContextData.SagaId, state.Id.ToString("D"));

            _logger!.SendingEvent(Name, message.GetType().Name);

            await context.GetBus().SendAsync(message, options, ct);
        }
    }
}

file static class Extensions
{
    public static bool TryGetSagaId(this IConsumeContext context, object? message, out Guid correlationId)
    {
        if (message is ICorrelatable { CorrelationId: not null } correlatable)
        {
            correlationId = correlatable.CorrelationId.Value;
            return true;
        }

        if (context.Headers.TryGet(SagaContextData.SagaId, out var headerId)
            && Guid.TryParse(headerId, out correlationId))
        {
            return true;
        }

        correlationId = Guid.Empty;
        return false;
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Created saga state {SagaName} {SagaId}")]
    public static partial void CreatedSagaState(this ILogger logger, string sagaName, Guid sagaId);

    [LoggerMessage(LogLevel.Information, "Loaded saga state for correlation id {SagaName} {CorrelationId}")]
    public static partial void LoadedSagaStateForCorrelationId(
        this ILogger logger,
        string sagaName,
        Guid correlationId);

    [LoggerMessage(LogLevel.Information, "Entering state {SagaName} {State}")]
    public static partial void EnteringState(this ILogger logger, string sagaName, string state);

    [LoggerMessage(LogLevel.Information, "Transitioning state {SagaName} {State} by event {Event}")]
    public static partial void TransitioningState(this ILogger logger, string sagaName, string state, string @event);

    [LoggerMessage(LogLevel.Information, "Publishing event {SagaName} {Event}")]
    public static partial void PublishingEvent(this ILogger logger, string sagaName, string @event);

    [LoggerMessage(LogLevel.Information, "Sending event {SagaName} {Event}")]
    public static partial void SendingEvent(this ILogger logger, string sagaName, string @event);

    [LoggerMessage(LogLevel.Information, "Replying to saga {SagaName} {CorrelationId} {ReplyTo} {Response}")]
    public static partial void ReplyingToSaga(
        this ILogger logger,
        string sagaName,
        string correlationId,
        string replyTo,
        string response);

    [LoggerMessage(LogLevel.Information, "Saga completed {SagaName} {SagaId}")]
    public static partial void SagaCompleted(this ILogger logger, string sagaName, Guid sagaId);
}
