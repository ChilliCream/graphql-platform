using System.Collections.Generic;

namespace HotChocolate.Language;

public interface IHasWithInterfaces<out TNode>
    where TNode : class, ISyntaxNode
{
    TNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces);
}
