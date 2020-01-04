using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public class GraphQLLanguageService
    {
        private readonly Dictionary<ISyntaxToken, ISyntaxNode> _tokenToNode =
            new Dictionary<ISyntaxToken, ISyntaxNode>();
        private readonly TokenIndexer _tokenIndexer;
        private DocumentNode? _document;

        public GraphQLLanguageService()
        {
            _tokenIndexer = new TokenIndexer(_tokenToNode);
        }

        public void Parse(string sourceText)
        {
            var parser = new StringGraphQLParser(sourceText.AsSpan());
            _document = parser.Parse();
            _tokenIndexer.Visit(_document);
        }

        private sealed class TokenIndexer : SyntaxVisitor
        {
            private readonly Dictionary<ISyntaxToken, ISyntaxNode> _tokenToNode;

            public TokenIndexer(Dictionary<ISyntaxToken, ISyntaxNode> tokenToNode)
                : base(Continue)
            {
                _tokenToNode = tokenToNode;
            }

            protected override ISyntaxVisitorAction Leave(
                ISyntaxNode node,
                ISyntaxVisitorContext context)
            {
                Location location = node.Location;
                ISyntaxToken end = location.EndToken;
                ISyntaxToken? current = location.StartToken;

                TryAddToken(current, node);

                while (current is { } && current != end)
                {
                    current = current.Next;
                    if (current is { })
                    {
                        TryAddToken(current, node);
                    }
                }

                return Continue;
            }

            private void TryAddToken(ISyntaxToken token, ISyntaxNode node)
            {
                if (!_tokenToNode.ContainsKey(token))
                {
                    _tokenToNode.Add(token, node);
                }
            }
        }
    }


}
