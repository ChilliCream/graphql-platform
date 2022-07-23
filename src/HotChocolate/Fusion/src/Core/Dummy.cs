using System.Text;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

internal class QueryPlan : ExecutionNode
{
}

internal  sealed class RequestNode : ExecutionNode
{
    private RequestHandler? _handler;

    public RequestHandler Handler
    {
        // todo : exception
        get => _handler ?? throw new InvalidOperationException("Not initialized!");
        internal set => _handler = value;
    }

    protected override void OnSeal()
    {
        if (_handler is null)
        {
            // todo : exception
            throw new InvalidOperationException(
                "The handler must be set before sealing a request node.");
        }
    }
}

internal  abstract class ExecutionNode
{
    private readonly List<ExecutionNode> _nodes = new();
    private bool _isReadOnly = false;

    public IReadOnlyList<ExecutionNode> Nodes => _nodes;

    internal void AppendNode(ExecutionNode node)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The execution node is read-only.");
        }

        _nodes.Add(node);
    }

    internal void Seal()
    {
        if (!_isReadOnly)
        {
            OnSeal();

            foreach (var node in _nodes)
            {
                node.Seal();
            }

            _isReadOnly = true;
        }
    }

    protected virtual void OnSeal() { }
}

internal sealed class RequestHandler
{
    private readonly DocumentNode _document;

    internal RequestHandler(DocumentNode document)
    {
        _document = document;
    }

    public Request CreateRequest(IReadOnlyList<IValueNode>? variables)
        => throw new NotImplementedException();

    public IReadOnlyList<IValueNode> ExtractState(JsonElement response)
        => throw new NotImplementedException();

    public void ExtractResult(JsonElement response, ObjectResult parent)
        => throw new NotImplementedException();
}

public readonly struct Request
{
    public DocumentNode Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
}

public interface IType // should be called named type
{
    string Name { get; }
}
