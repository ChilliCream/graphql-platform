using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface ISyntaxNode
    {
        NodeKind Kind { get; }

        Location? Location { get; }

        IEnumerable<ISyntaxNode> GetNodes();

        string ToString(bool indented);
    }
}
