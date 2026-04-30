using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Carries the context needed by the transport-level request hooks
/// (<see cref="HttpSourceSchemaClientConfiguration.OnBeforeSend"/> and
/// <see cref="HttpSourceSchemaClientConfiguration.OnAfterReceive"/>).
/// Stored on <see cref="Transport.Http.GraphQLHttpRequest.State"/>
/// so the static hook delegates can access it without capturing.
/// </summary>
public readonly struct RequestCallbackState
{
    public RequestCallbackState(
        OperationPlanContext context,
        ExecutionNode node,
        HttpSourceSchemaClientConfiguration configuration)
    {
        Context = context;
        Node = node;
        Configuration = configuration;
    }

    public OperationPlanContext Context { get; }

    public ExecutionNode Node { get; }

    public HttpSourceSchemaClientConfiguration Configuration { get; }
}
