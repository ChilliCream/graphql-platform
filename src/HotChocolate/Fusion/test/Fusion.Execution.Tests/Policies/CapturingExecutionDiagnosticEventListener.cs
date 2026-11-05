using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Policies;

internal sealed class CapturingExecutionDiagnosticEventListener : FusionExecutionDiagnosticEventListener
{
    private readonly ConcurrentQueue<Exception> _requestErrors = new();

    private readonly ConcurrentQueue<Exception> _subscriptionEventErrors = new();

    public ImmutableArray<Exception> RequestErrors => [.. _requestErrors];

    public ImmutableArray<Exception> SubscriptionEventErrors => [.. _subscriptionEventErrors];

    public override void RequestError(RequestContext context, Exception error)
        => _requestErrors.Enqueue(error);

    public override void SubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception)
        => _subscriptionEventErrors.Enqueue(exception);
}
