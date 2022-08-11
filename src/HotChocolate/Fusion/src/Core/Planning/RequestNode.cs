using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal sealed class RequestNode : ExecutionNode
{
    public RequestNode(RequestHandler handler)
    {
        Handler = handler;
    }

    public RequestHandler Handler { get; }
}
