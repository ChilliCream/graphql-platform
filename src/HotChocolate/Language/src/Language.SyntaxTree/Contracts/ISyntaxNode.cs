using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface ISyntaxNode
    {
        NodeKind Kind { get; }

        Location? Location { get; }

        //int NodeCount { get; }

        IEnumerable<ISyntaxNode> GetNodes();

        // void CopyTo(Span<ISyntaxNode> nodes);

        string ToString(bool indented);
    }
}
