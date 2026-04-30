namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// The federated <c>Node</c> interface as projected by the <c>node</c>
/// subgraph: a single <c>id</c> field shared by every implementer.
/// </summary>
public interface INode
{
    string Id { get; }
}
