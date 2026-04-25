using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// <see cref="MochaResourceSource"/> that projects a live <see cref="MessagingRuntime"/> into a
/// flat list of <see cref="MochaResource"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// The source rebuilds its snapshot lazily on first read using
/// <see cref="MessageBusDescriptionVisitor"/> to walk the runtime exactly once — the visitor
/// remains the single intermediate, avoiding duplicate traversal logic. Each transport's
/// <see cref="MessagingTransport.ContributeMochaResources"/> override appends transport-specific
/// resources (queues, exchanges, topics, bindings) onto the snapshot.
/// </para>
/// <para>
/// To keep dispatch endpoints created lazily (post-startup) observable, the source subscribes to
/// <see cref="IEndpointRouter.DispatchEndpointAdded"/> and fires its change token after each new
/// endpoint is observed. Idiomatic ordering — swap the token before cancelling, the same pattern
/// used by <c>CompositeMochaResourceSource</c> — keeps consumer callbacks safe under re-entrancy.
/// </para>
/// </remarks>
public sealed class MochaMessageBusResourceSource : MochaResourceSource, IDisposable
{
    private readonly object _lock = new();
    private readonly MessagingRuntime _runtime;
    private readonly EventHandler<DispatchEndpointAddedEventArgs> _dispatchEndpointAddedHandler;

    private IReadOnlyList<MochaResource>? _resources;
    private CancellationTokenSource? _cts;
    private IChangeToken? _consumerChangeToken;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="MochaMessageBusResourceSource"/> over the supplied runtime.
    /// </summary>
    /// <param name="runtime">The messaging runtime to project as resources.</param>
    public MochaMessageBusResourceSource(MessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _dispatchEndpointAddedHandler = OnDispatchEndpointAdded;
        _runtime.Endpoints.DispatchEndpointAdded += _dispatchEndpointAddedHandler;
    }

    /// <inheritdoc />
    public override IReadOnlyList<MochaResource> Resources
    {
        get
        {
            EnsureResourcesInitialized();
            return _resources;
        }
    }

    /// <inheritdoc />
    public override IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return _consumerChangeToken;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancellationTokenSource? oldCts;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _runtime.Endpoints.DispatchEndpointAdded -= _dispatchEndpointAddedHandler;

            oldCts = _cts;
            _cts = null;
        }

        oldCts?.Dispose();
    }

    [MemberNotNull(nameof(_resources))]
    private void EnsureResourcesInitialized()
    {
        if (_resources is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_resources is not null)
            {
                return;
            }

            EnsureChangeTokenInitialized();
            CreateResourcesUnsynchronized();
        }
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (_consumerChangeToken is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_consumerChangeToken is not null)
            {
                return;
            }

            CreateChangeTokenUnsynchronized();
        }
    }

    private void OnDispatchEndpointAdded(object? sender, DispatchEndpointAddedEventArgs args)
    {
        CancellationTokenSource? oldTokenSource;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            oldTokenSource = _cts;

            // Don't create a new change token if no one is listening yet.
            if (oldTokenSource is not null)
            {
                CreateChangeTokenUnsynchronized();
            }

            // Don't refresh the snapshot if no one has read it yet.
            if (_resources is not null)
            {
                CreateResourcesUnsynchronized();
            }
        }

        oldTokenSource?.Cancel();
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void CreateChangeTokenUnsynchronized()
    {
        var cts = new CancellationTokenSource();
        _cts = cts;
        _consumerChangeToken = new CancellationChangeToken(cts.Token);
    }

    [MemberNotNull(nameof(_resources))]
    private void CreateResourcesUnsynchronized()
    {
        var description = MessageBusDescriptionVisitor.Visit(_runtime);
        var resources = new List<MochaResource>();

        var instanceId = description.Host.InstanceId;
        resources.Add(new MochaServiceResource(description.Host));

        foreach (var messageType in description.MessageTypes)
        {
            resources.Add(new MochaMessageTypeResource(instanceId, messageType));
        }

        foreach (var consumer in description.Consumers)
        {
            resources.Add(new MochaHandlerResource(instanceId, consumer));
        }

        var inboundIndex = 0;
        foreach (var route in description.Routes.Inbound)
        {
            resources.Add(new MochaInboundRouteResource(instanceId, inboundIndex, route));
            inboundIndex++;
        }

        var outboundIndex = 0;
        foreach (var route in description.Routes.Outbound)
        {
            resources.Add(new MochaOutboundRouteResource(instanceId, outboundIndex, route));
            outboundIndex++;
        }

        if (description.Sagas is { } sagas)
        {
            foreach (var saga in sagas)
            {
                AddSagaResources(resources, instanceId, saga);
            }
        }

        foreach (var transport in _runtime.Transports)
        {
            transport.ContributeMochaResources(resources);
        }

        _resources = resources;
    }

    private static void AddSagaResources(
        List<MochaResource> resources,
        string instanceId,
        SagaDescription saga)
    {
        var sagaResource = new MochaSagaResource(instanceId, saga);
        resources.Add(sagaResource);

        foreach (var state in saga.States)
        {
            var stateResource = new MochaSagaStateResource(sagaResource.Id, instanceId, saga.Name, state);
            resources.Add(stateResource);
        }

        foreach (var fromState in saga.States)
        {
            var fromStateResource = new MochaSagaStateResource(sagaResource.Id, instanceId, saga.Name, fromState);
            var transitionIndex = 0;
            foreach (var transition in fromState.Transitions)
            {
                var toState = FindState(saga, transition.TransitionTo);
                var toStateResource = toState is not null
                    ? new MochaSagaStateResource(sagaResource.Id, instanceId, saga.Name, toState)
                    : null;
                var toStateId = toStateResource?.Id ?? MochaUrn.Create("core", "saga_state", instanceId, saga.Name, transition.TransitionTo);

                resources.Add(
                    new MochaSagaTransitionResource(
                        sagaResource.Id,
                        fromStateResource.Id,
                        toStateId,
                        instanceId,
                        saga.Name,
                        fromState.Name,
                        transitionIndex,
                        transition));
                transitionIndex++;
            }
        }
    }

    private static SagaStateDescription? FindState(SagaDescription saga, string name)
    {
        foreach (var state in saga.States)
        {
            if (string.Equals(state.Name, name, StringComparison.Ordinal))
            {
                return state;
            }
        }

        return null;
    }
}
