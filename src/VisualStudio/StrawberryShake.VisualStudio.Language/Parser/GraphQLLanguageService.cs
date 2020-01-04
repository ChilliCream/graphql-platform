using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public class GraphQLLanguageService
    {
        private readonly Dictionary<ISyntaxToken, ISyntaxNode> _tokenToNode =
            new Dictionary<ISyntaxToken, ISyntaxNode>();

        private DocumentNode _currentDocument;

        public void Parse(string sourceText)
        {

        }

        private void Foo(ISyntaxNode )
    }

    public interface ISyntaxToken
    {
        TokenKind Kind { get; }
        int Start { get; }
        int End { get; }
        int Line { get; }
        int Column { get; }
        string Value { get; }
        SyntaxToken Previous { get; }
        SyntaxToken Next { get; }
    }
}
