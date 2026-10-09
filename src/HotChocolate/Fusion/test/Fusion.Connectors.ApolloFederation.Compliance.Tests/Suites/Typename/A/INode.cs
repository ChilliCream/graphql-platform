namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// The <c>Node</c> interface as projected by the <c>a</c> subgraph: a
/// single <c>id</c> field shared by every implementer (<see cref="Oven"/>
/// and <see cref="Toaster"/>).
/// </summary>
public interface INode
{
    string Id { get; }
}
