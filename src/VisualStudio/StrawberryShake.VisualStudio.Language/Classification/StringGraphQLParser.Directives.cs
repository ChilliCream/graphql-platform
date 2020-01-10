using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLClassifier
    {
        private static readonly List<DirectiveNode> _emptyDirectives =
            new List<DirectiveNode>();

        private DirectiveDefinitionNode ParseDirectiveDefinition()
        {
            ISyntaxToken start = _reader.Token;

            StringValueNode? description = ParseDescription();

            ExpectDirectiveKeyword();
            ExpectAt();

            NameNode name = ParseName();
            List<InputValueDefinitionNode> arguments = ParseArgumentDefinitions();

            bool isRepeatable = SkipRepeatableKeyword();
            ExpectOnKeyword();

            List<NameNode> locations = ParseDirectiveLocations();

             var location = new Location(start, _reader.Token);

            return new DirectiveDefinitionNode
            (
                location,
                name,
                description,
                isRepeatable,
                arguments,
                locations
            );
        }

        private List<NameNode> ParseDirectiveLocations()
        {
            var list = new List<NameNode>();

            // skip optional leading pipe.
            SkipPipe();

            do
            {
                list.Add(ParseDirectiveLocation());
            }
            while (SkipPipe());

            return list;
        }

        private NameNode ParseDirectiveLocation()
        {
            TokenKind kind = _reader.Kind;
            NameNode name = ParseName();

            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }

            throw Unexpected(kind);
        }

        private void ParseDirectives(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            if (_reader.Kind == TokenKind.At)
            {
                while (_reader.Kind == TokenKind.At)
                {
                    ParseDirective(isConstant);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DirectiveNode ParseDirective(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            ISyntaxToken start = _reader.Token;

            ExpectAt();
            ExpectName();

            classifications.AddClassification(
                SyntaxClassificationKind.DirectiveIdentifier,
                new Location(start, _reader.Token));

            List<ArgumentNode> arguments = ParseArguments(isConstant);

             var location = new Location(start, _reader.Token);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }
    }
}
