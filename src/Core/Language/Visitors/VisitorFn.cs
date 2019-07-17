using System.Collections.Generic;

namespace HotChocolate.Language
{
    public delegate VisitorAction VisitorFn<T>(
        T node,
        ISyntaxNode parent,
        IReadOnlyList<object> path,
        IReadOnlyList<ISyntaxNode> ancestors)
        where T : ISyntaxNode;
}
