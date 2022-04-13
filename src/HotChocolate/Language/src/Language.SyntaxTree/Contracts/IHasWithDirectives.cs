using System.Collections.Generic;

namespace HotChocolate.Language;

public interface IHasWithDirectives<out TNode>
{
    TNode WithDirectives(IReadOnlyList<DirectiveNode> directives);
}
