using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLParser
    {
        private static readonly List<DirectiveNode> _emptyDirectives =
            new List<DirectiveNode>();

        private DirectiveDefinitionNode ParseDirectiveDefinition()
        {
            TokenInfo start = Start();

            StringValueNode? description = ParseDescription();

            ExpectDirectiveKeyword();
            ExpectAt();

            NameNode name = ParseName();
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions();

            bool isRepeatable = SkipRepeatableKeyword();
            ExpectOnKeyword();

            List<NameNode> locations = ParseDirectiveLocations();

            Location? location = CreateLocation(in start);

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

        private List<DirectiveNode> ParseDirectives(bool isConstant)
        {
            if (_reader.Kind == TokenKind.At)
            {
                var list = new List<DirectiveNode>();

                while (_reader.Kind == TokenKind.At)
                {
                    list.Add(ParseDirective(isConstant));
                }

                return list;
            }

            return _emptyDirectives;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DirectiveNode ParseDirective(bool isConstant)
        {
            TokenInfo start = Start();

            ExpectAt();
            NameNode name = ParseName();
            List<ArgumentNode> arguments = ParseArguments(isConstant);

            Location? location = CreateLocation(in start);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }
    }
}
