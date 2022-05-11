using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public interface ISyntaxReference
{
    ISyntaxReference? Parent { get; }
    ISyntaxNode Node { get; }
}