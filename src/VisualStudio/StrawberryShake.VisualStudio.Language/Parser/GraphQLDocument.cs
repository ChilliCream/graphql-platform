using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.VisualStudio.Language
{
    public class GraphQLDocument
    {
        private static readonly List<ISyntaxClassifier> _syntaxClassifiers = new List<ISyntaxClassifier>
        {
            new TypeNameClassifier(),
            new TypeKeywordClassifier(),
            new OperationDefinitionClassifier(),
            new LiteralClassifier()
        };

        private readonly Dictionary<ISyntaxToken, ISyntaxNode> _tokenToNode =
            new Dictionary<ISyntaxToken, ISyntaxNode>();
        private readonly Dictionary<ISyntaxToken, SyntaxClassification> _tokenToClassification =
            new Dictionary<ISyntaxToken, SyntaxClassification>();

        private readonly TokenIndexer _tokenIndexer;
        private DocumentNode? _document;

        public GraphQLDocument()
        {
            _tokenIndexer = new TokenIndexer(_tokenToNode);
        }

        public void Parse(string sourceText)
        {
            try
            {
                var parser = new StringGraphQLParser(sourceText.AsSpan());
                _document = parser.Parse();

                _tokenToNode.Clear();
                _tokenToClassification.Clear();

                _tokenIndexer.Visit(_document);

                ISyntaxToken? current = _document.Location.StartToken;
                while (current is { } && current.Kind != TokenKind.EndOfFile)
                {
                    if (current.Kind == TokenKind.Comment)
                    {
                        _tokenToClassification.Add(
                            current,
                            new SyntaxClassification(
                                SyntaxClassificationKind.Comment,
                                current.Start,
                                current.End - current.Start));
                    }
                    else if (_tokenToNode.TryGetValue(current, out ISyntaxNode? node))
                    {
                        for (int i = 0; i < _syntaxClassifiers.Count; i++)
                        {
                            if (_syntaxClassifiers[i].TryClassify(
                                current, node,
                                out SyntaxClassification? classification))
                            {
                                _tokenToClassification.Add(current, classification!.Value);
                            }
                        }
                    }
                    else
                    {
                        _tokenToClassification.Add(
                            current,
                            new SyntaxClassification(
                                SyntaxClassificationKind.Other,
                                current.Start,
                                current.End - current.Start));
                    }

                    current = current.Next;
                }
            }
            catch (SyntaxException)
            {

            }
        }

        public IEnumerable<SyntaxClassification> GetSyntaxClassifications(int start, int length)
        {
            int end = start + length;

            foreach (ISyntaxToken token in _tokenToNode.Keys
                .Where(t => t.Start >= start && t.Start < end)
                .OrderBy(t => t.Start))
            {
                if (_tokenToClassification.TryGetValue(token, out SyntaxClassification classification))
                {
                    yield return classification;
                }
            }

            yield break;
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

                if (node.Kind != NodeKind.Name)
                {
                    TryAddToken(current, node);

                    while (current is { } && current != end)
                    {
                        current = current.Next;
                        if (current is { })
                        {
                            TryAddToken(current, node);
                        }
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

    internal interface ISyntaxClassifier
    {
        bool TryClassify(
            ISyntaxToken token,
            ISyntaxNode node,
            out SyntaxClassification? classification);
    }

    public class TypeNameClassifier : ISyntaxClassifier
    {
        public bool TryClassify(
            ISyntaxToken token,
            ISyntaxNode node,
            out SyntaxClassification? classification)
        {
            if (token.Kind == TokenKind.Name
                && node is ITypeDefinitionNode
                && node is INamedSyntaxNode named
                && named.Name.Location.StartToken == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.SymbolDefinition,
                    token.Start,
                    token.Length);
                return true;
            }

            classification = null;
            return false;
        }
    }

    public class TypeKeywordClassifier : ISyntaxClassifier
    {
        public bool TryClassify(
            ISyntaxToken token,
            ISyntaxNode node,
            out SyntaxClassification? classification)
        {
            if (token.Kind == TokenKind.Name
                && node is ITypeDefinitionNode type
                && GetKeywordToken(type.Location.StartToken) == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.Keyword,
                    token.Start,
                    token.Length);
                return true;
            }

            classification = null;
            return false;
        }

        private static ISyntaxToken GetKeywordToken(ISyntaxToken token)
        {
            ISyntaxToken current = token;
            while (current.Kind != TokenKind.Name)
            {
                current = current.Next!;
            }
            return current;
        }
    }

    public class OperationDefinitionClassifier : ISyntaxClassifier
    {
        public bool TryClassify(
            ISyntaxToken token,
            ISyntaxNode node,
            out SyntaxClassification? classification)
        {
            if (token.Kind == TokenKind.Name
                && node is OperationDefinitionNode op)
            {
                if (op.Location.StartToken == token)
                {
                    classification = new SyntaxClassification(
                        SyntaxClassificationKind.Keyword,
                        token.Start,
                        token.Length);
                    return true;
                }
                else if (op.Name is { }
                    && op.Name.Location.StartToken == token)
                {
                    classification = new SyntaxClassification(
                        SyntaxClassificationKind.SymbolDefinition,
                        token.Start,
                        token.Length);
                    return true;
                }
            }

            classification = null;
            return false;
        }
    }

    public class LiteralClassifier : ISyntaxClassifier
    {
        public bool TryClassify(
            ISyntaxToken token,
            ISyntaxNode node,
            out SyntaxClassification? classification)
        {
            if (node is StringValueNode
                && node.Location.StartToken == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.StringLiteral,
                    token.Start,
                    token.Length);
                return true;
            }

            if ((node is IntValueNode || node is FloatValueNode)
                && node.Location.StartToken == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.NumberLiteral,
                    token.Start,
                    token.Length);
                return true;
            }

            if (node is BooleanValueNode
                && node.Location.StartToken == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.BooleanLiteral,
                    token.Start,
                    token.Length);
                return true;
            }

            if (node is EnumValueNode
               && node.Location.StartToken == token)
            {
                classification = new SyntaxClassification(
                    SyntaxClassificationKind.EnumLiteral,
                    token.Start,
                    token.Length);
                return true;
            }

            classification = null;
            return false;
        }
    }


}
