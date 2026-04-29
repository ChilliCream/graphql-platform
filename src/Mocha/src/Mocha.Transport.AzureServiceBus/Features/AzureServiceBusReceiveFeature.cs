using Azure.Messaging.ServiceBus;
using Mocha.Features;

namespace Mocha.Transport.AzureServiceBus.Features;

/// <summary>
/// Pooled feature carrying the Azure Service Bus delivery context through the receive middleware
/// pipeline. The endpoint dispatcher calls <see cref="SetNonSession"/> or <see cref="SetSession"/>
/// per message; subsequent middleware accesses the current message via <see cref="Message"/> and
/// settles it via <see cref="Actions"/>. Handlers that need session-only primitives reach the SDK
/// args directly via <c>context.GetAzureServiceBusSessionEventArgs()</c>.
/// </summary>
internal sealed class AzureServiceBusReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets the received message for the current dispatch.
    /// </summary>
    public ServiceBusReceivedMessage Message
        => ProcessMessageEventArgs?.Message
        ?? ProcessSessionMessageEventArgs?.Message
        ?? throw new InvalidOperationException("Receive feature has no args set");

    /// <summary>
    /// Gets the settlement actions for the current message.
    /// </summary>
    internal IAzureServiceBusMessageActions Actions { get; private set; } = default!;

    /// <summary>
    /// Gets the underlying non-session event args, or <see langword="null"/> on a session dispatch.
    /// </summary>
    public ProcessMessageEventArgs? ProcessMessageEventArgs { get; private set; }

    /// <summary>
    /// Gets the underlying session event args, or <see langword="null"/> on a non-session dispatch.
    /// </summary>
    public ProcessSessionMessageEventArgs? ProcessSessionMessageEventArgs { get; private set; }

    internal void SetNonSession(ProcessMessageEventArgs args)
    {
        ProcessMessageEventArgs = args;
        ProcessSessionMessageEventArgs = null;
        Actions = new AzureServiceBusMessageActions(args);
    }

    internal void SetSession(ProcessSessionMessageEventArgs args)
    {
        ProcessMessageEventArgs = null;
        ProcessSessionMessageEventArgs = args;
        Actions = new AzureServiceBusSessionMessageActions(args);
    }

    // IPooledFeature contract: Initialize is a no-op here.
    // The dispatcher's static lambda calls SetNonSession/SetSession to populate state.
    // (Existing pattern — Initialize(state) populating the field directly — is replaced
    //  by the typed setters so the exclusive-disjunction invariant is enforced in code.)
    /// <inheritdoc />
    public void Initialize(object _) { }

    /// <inheritdoc />
    public void Reset()
    {
        ProcessMessageEventArgs = null;
        ProcessSessionMessageEventArgs = null;
        Actions = default!;
    }
}
