using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

public abstract class QueryPlanNode
{
    private readonly List<QueryPlanNode> _dependsOn = new();
    private bool _isReadOnly;

    public IReadOnlyList<QueryPlanNode> DependsOn => _dependsOn;

    public abstract bool AppliesTo(ISelectionSet selectionSet);

    internal abstract Task ExecuteAsync(
        IFederationContext context,
        IReadOnlyList<WorkItem> workItems,
        CancellationToken cancellationToken);

    internal void AddDependency(QueryPlanNode node)
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
