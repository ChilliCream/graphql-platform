using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;

namespace Mocha.Sagas;

public abstract partial class Saga
{
    /// <summary>
    /// Initializes the saga by building its state machine from the descriptor configuration and validating the result.
    /// </summary>
    /// <param name="context">The messaging setup context providing naming conventions and services.</param>
    public abstract void Initialize(IMessagingSetupContext context);
}

public abstract partial class Saga<TState> where TState : SagaStateBase
{
    /// <inheritdoc />
    public override void Initialize(IMessagingSetupContext context)
    {
        var descriptor = new SagaDescriptor<TState>(context);

        Configuration = CreateConfiguration(context);
        descriptor.Name(context.Naming.GetSagaName(GetType()));

        Configure(descriptor);

        var definition = descriptor.CreateConfiguration();

        _logger = context.Services.GetRequiredService<ILogger<Saga<TState>>>();
        Name = definition.Name ?? throw new SagaInitializationException(this, "Saga name is not defined.");
        Urn = MochaUrn.Saga(context.Host.EffectiveServiceName, Name);
        StateSerializer ??=
            definition.StateSerializer?.Invoke(context.Services)
            ?? context.Services.GetRequiredService<ISagaStateSerializerFactory>().GetSerializer(typeof(TState));

        var duringAnyTransitions = definition.DuringAny!.Transitions;

        var states = new Dictionary<string, SagaState>();

        foreach (var state in definition.States)
        {
            if (state.Name is null)
            {
                throw new SagaInitializationException(this, "State name is not defined.");
            }

            var stateName = state.Name;
            var transitions = InitializeTransitions(
                state,
                // we don't want DuringAny transitions to be added to initial and final states
                state.IsFinal || state.IsInitial
                    ? []
                    : duringAnyTransitions);

            var response = state.Response is { EventType: not null, Factory: not null }
                ? new SagaResponse(state.Response.EventType, state.Response.Factory)
                : null;

            var onEntry = InitializeLifeCycle(state.OnEntry);

            var sagaState = new SagaState(
                MochaUrn.SagaState(Urn, stateName),
                stateName,
                state.IsInitial,
                state.IsFinal,
                onEntry,
                response,
                transitions);

            if (state.IsInitial)
            {
                foreach (var transition in transitions)
                {
                    if (transition.StateFactory is null)
                    {
                        throw new SagaInitializationException(
                            this,
                            $"When '{transition.EventType.Name}' is triggered, no state factory is defined.");
                    }
                }

                _initialState = sagaState;
            }

            states.Add(state.Name, sagaState);
        }

        _states = states;

        // TODO this is probably the wrong place for this and should be togglable!
        SagaValidator.ValidateStateMachine(this);
    }

    private SagaLifeCycle? InitializeLifeCycle(SagaLifeCycleConfiguration? definition)
    {
        if (definition is null)
        {
            return null;
        }

        var publish = InitializeEventPublish(definition.Publish);
        var send = InitializeEventSend(definition.Send);

        var lifeCycle = new SagaLifeCycle(publish, send);

        return lifeCycle;
    }

    private ImmutableArray<SagaTransition> InitializeTransitions(
        SagaStateConfiguration state,
        IReadOnlyList<SagaTransitionConfiguration>? additionalTransitions = null)
    {
        additionalTransitions ??= [];

        var stateName = state.Name ?? throw new SagaInitializationException(this, "State name is not defined.");

        var transitions = ImmutableArray.CreateBuilder<SagaTransition>(
            state.Transitions.Count + additionalTransitions.Count);

        foreach (var transition in state.Transitions)
        {
            transitions.Add(InitializeTransition(stateName, transition));
        }

        foreach (var transition in additionalTransitions)
        {
            transitions.Add(InitializeTransition(stateName, transition));
        }

        return transitions.MoveToImmutable();
    }

    private SagaTransition InitializeTransition(
        string stateName,
        SagaTransitionConfiguration transition)
    {
        if (transition.EventType is not { } eventType)
        {
            throw new SagaInitializationException(this, "Transition event type is not defined.");
        }

        if (transition.TransitionTo is null)
        {
            throw new SagaInitializationException(this, "Transition target state is not defined.");
        }

        var transitionKind =
            transition.TransitionKind
            ?? throw new SagaInitializationException(this, "Transition has no kind defined");

        return new SagaTransition(
            MochaUrn.SagaTransition(Urn, stateName, DescriptionHelpers.GetTypeName(eventType)),
            eventType,
            transition.TransitionTo,
            transitionKind,
            transition.Action ?? DefaultAction,
            InitializeEventPublish(transition.Publish),
            InitializeEventSend(transition.Send),
            transition.StateFactory);
    }

    private static void DefaultAction(object _, object __)
    { }

    private static ImmutableArray<SagaEventPublish> InitializeEventPublish(
        List<SagaEventPublishConfiguration> definitions)
    {
        var publish = ImmutableArray.CreateBuilder<SagaEventPublish>(definitions.Count);

        foreach (var publishConfiguration in definitions)
        {
            publish.Add(
                new SagaEventPublish(
                    publishConfiguration.MessageType,
                    publishConfiguration.Factory,
                    publishConfiguration.Options));
        }

        return publish.MoveToImmutable();
    }

    private static ImmutableArray<SagaEventSend> InitializeEventSend(List<SagaEventSendConfiguration> definitions)
    {
        var send = ImmutableArray.CreateBuilder<SagaEventSend>(definitions.Count);

        foreach (var sendConfiguration in definitions)
        {
            send.Add(
                new SagaEventSend(sendConfiguration.MessageType, sendConfiguration.Factory, sendConfiguration.Options));
        }

        return send.MoveToImmutable();
    }

    private SagaConfiguration CreateConfiguration(IMessagingSetupContext discoveryContext)
    {
        var descriptor = new SagaDescriptor<TState>(discoveryContext);
        _configure(descriptor);
        return descriptor.CreateConfiguration();
    }
}
