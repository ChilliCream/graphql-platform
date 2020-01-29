using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public interface ISyntaxNode
    {
        NodeKind Kind { get; }

        Location Location { get; }

        IEnumerable<ISyntaxNode> GetNodes();
    }
}
