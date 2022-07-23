namespace HotChocolate.Fusion;

internal sealed class RequestNode : ExecutionNode
{
    public RequestNode(RequestHandler handler)
    {
        Handler = handler;
    }

    public RequestHandler Handler { get; }
}
