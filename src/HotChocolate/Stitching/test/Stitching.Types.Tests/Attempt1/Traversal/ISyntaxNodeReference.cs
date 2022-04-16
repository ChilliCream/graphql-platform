using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Traversal;

public interface ISyntaxNodeReference
{
    ISyntaxNodeReference? Parent { get; }
    ISyntaxNode Node { get; }
}
