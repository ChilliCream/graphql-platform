namespace HotChocolate.Fusion;

internal abstract class ExecutionNode
{
    private readonly HashSet<ExecutionNode> _dependsOn = new();
    private bool _isReadOnly = false;

    public IReadOnlySet<ExecutionNode> DependsOn => _dependsOn;

    internal void AddDependency(ExecutionNode node)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The execution node is read-only.");
        }

        _dependsOn.Add(node);
    }

    internal void Seal()
    {
        if (!_isReadOnly)
        {
            OnSeal();
            _isReadOnly = true;
        }
    }

    protected virtual void OnSeal() { }
}
