using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IHasSyntaxNode
    {
        ISyntaxNode SyntaxNode { get; }
    }
}