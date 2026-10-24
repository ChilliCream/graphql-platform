using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;

namespace HotChocolate.Fusion.Policies;

internal sealed class CapturingExecutionDiagnosticEventListener : FusionExecutionDiagnosticEventListener
{
    private readonly ConcurrentQueue<Exception> _requestErrors = new();

    public ImmutableArray<Exception> RequestErrors => [.. _requestErrors];

    public override void RequestError(RequestContext context, Exception error)
        => _requestErrors.Enqueue(error);
}
