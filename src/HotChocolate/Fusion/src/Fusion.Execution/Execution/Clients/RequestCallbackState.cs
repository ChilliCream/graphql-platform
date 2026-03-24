using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Carries the context needed by the transport-level request hooks
/// (<see cref="SourceSchemaHttpClientConfiguration.OnBeforeSend"/> and
/// <see cref="SourceSchemaHttpClientConfiguration.OnAfterReceive"/>).
/// Stored on <see cref="Transport.Http.GraphQLHttpRequest.State"/>
/// so the static hook delegates can access it without capturing.
/// </summary>
public readonly struct RequestCallbackState
{
    public RequestCallbackState(
        OperationPlanContext context,
        ExecutionNode node,
        SourceSchemaHttpClientConfiguration configuration)
    {
        Context = context;
        Node = node;
        Configuration = configuration;
    }

    public OperationPlanContext Context { get; }

    public ExecutionNode Node { get; }

    public SourceSchemaHttpClientConfiguration Configuration { get; }
}
