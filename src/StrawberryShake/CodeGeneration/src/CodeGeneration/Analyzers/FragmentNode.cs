using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FragmentNode
{
    public FragmentNode(
        Fragment fragment,
        IReadOnlyList<FragmentNode>? nodes = null,
        DirectiveNode? defer = null)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        Nodes = nodes ?? Array.Empty<FragmentNode>();
        Defer = defer;
    }

    public Fragment Fragment { get; }

    public DirectiveNode? Defer { get; }

    public IReadOnlyList<FragmentNode> Nodes { get; }
}
