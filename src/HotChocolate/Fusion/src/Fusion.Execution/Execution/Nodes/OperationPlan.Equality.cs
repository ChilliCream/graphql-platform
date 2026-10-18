namespace HotChocolate.Fusion.Execution.Nodes;

public sealed partial record OperationPlan
{
    /// <inheritdoc />
    public bool Equals(OperationPlan? other)
        => ReferenceEquals(this, other)
            || other is not null
            && ReferenceEquals(_nodesById, other._nodesById)
            && Id == other.Id
            && Operation == other.Operation
            && RootNodes.Equals(other.RootNodes)
            && AllNodes.Equals(other.AllNodes)
            && SearchSpace == other.SearchSpace
            && ExpandedNodes == other.ExpandedNodes
            && DeliveryGroups.Equals(other.DeliveryGroups)
            && IncrementalPlans.Equals(other.IncrementalPlans)
            && MaxNodeId == other.MaxNodeId
            && UsesDynamicSchemaNames == other.UsesDynamicSchemaNames
            && UsesBatchNodes == other.UsesBatchNodes;

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_nodesById);
        hash.Add(Id);
        hash.Add(Operation);
        hash.Add(RootNodes);
        hash.Add(AllNodes);
        hash.Add(SearchSpace);
        hash.Add(ExpandedNodes);
        hash.Add(DeliveryGroups);
        hash.Add(IncrementalPlans);
        hash.Add(MaxNodeId);
        hash.Add(UsesDynamicSchemaNames);
        hash.Add(UsesBatchNodes);
        return hash.ToHashCode();
    }
}
