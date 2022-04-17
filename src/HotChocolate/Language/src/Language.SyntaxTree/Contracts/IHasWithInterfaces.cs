using System.Collections.Generic;

namespace HotChocolate.Language;

public interface IHasWithInterfaces<out TNode>
{
    TNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces);
}
