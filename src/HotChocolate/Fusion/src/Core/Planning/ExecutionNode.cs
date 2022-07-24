namespace HotChocolate.Fusion.Planning;

internal abstract class ExecutionNode
{
    private readonly List<ExecutionNode> _dependsOn = new();
    private bool _isReadOnly;

    public IReadOnlyList<ExecutionNode> DependsOn => _dependsOn;

    internal void AddDependency(ExecutionNode node)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The execution node is read-only.");
        }

        if (!_dependsOn.Contains(node))
        {
            _dependsOn.Add(node);
        }
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
