using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private static readonly List<DirectiveNode> _emptyDirectives =
            new List<DirectiveNode>();

        private void ParseDirectiveDefinition()
        {
            ParseDirectiveKeyword();

            ParseDirectiveName();

            ParseArgumentDefinitions();

            ParseRepeatableKeyword();

            ParseOnKeyword();

            ParseDirectiveLocations();
        }

        private void ParseRepeatableKeyword() =>
            SkipKeyword(SyntaxClassificationKind.RepeatableKeyword, GraphQLKeywords.Repeatable);

        private void ParseDirectiveLocations()
        {
            // skip optional leading pipe.
            SkipPipe();

            do
            {
                ParseDirectiveLocation();
            }
            while (SkipPipe());
        }

        private unsafe void ParseDirectiveLocation()
        {
            ISyntaxToken token = _reader.Token;

            if (token.Kind == TokenKind.Name)
            {
                fixed (char* c = _reader.Value)
                {
                    string name = new string(c, 0, _reader.Value.Length);
                    if (DirectiveLocation.IsValidName(name))
                    {
                        _classifications.AddClassification(
                            SyntaxClassificationKind.DirectiveLocation,
                            _reader.Token);
                        MoveNext();
                        return;
                    }
                }
            }

            _classifications.AddClassification(
                SyntaxClassificationKind.Error,
                _reader.Token);
            MoveNext();
        }

        private void ParseDirectives(bool isConstant)
        {
            if (_reader.Kind == TokenKind.At)
            {
                while (_reader.Kind == TokenKind.At)
                {
                    ParseDirective(isConstant);
                }
            }
        }

        private void ParseDirective(bool isConstant)
        {
            ParseDirectiveName();
            ParseArguments(isConstant);
        }

        private void ParseDirectiveName()
        {
            ISyntaxToken start = _reader.Token;

            if (_reader.Kind == TokenKind.At)
            {
                MoveNext();
                _classifications.AddClassification(
                    _reader.Kind == TokenKind.Name
                        ? SyntaxClassificationKind.DirectiveIdentifier
                        : SyntaxClassificationKind.Error,
                    new Location(start, _reader.Token));
            }
            else
            {
                _classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }

            MoveNext();
        }
    }
}
