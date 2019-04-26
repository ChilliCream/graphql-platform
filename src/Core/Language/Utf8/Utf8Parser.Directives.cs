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
            TokenInfo start = TokenInfo.FromReader(in _reader);

            StringValueNode description = ParseDescription();

            ExpectDirectiveKeyword();
            ExpectAt();

            NameNode name = ParseName();
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions();

            bool isRepeatable = SkipRepeatableKeyword();
            ExpectOnKeyword();

            List<NameNode> locations = ParseDirectiveLocations();

            Location location = CreateLocation(in start);

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

        private static List<NameNode> ParseDirectiveLocations(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            var list = new List<NameNode>();

            // skip optional leading pipe.
            ParserHelper.Skip(ref reader, TokenKind.Pipe);

            do
            {
                list.Add(ParseDirectiveLocation(context, ref reader));
            }
            while (ParserHelper.Skip(ref reader, TokenKind.Pipe));

            return list;
        }

        private static NameNode ParseDirectiveLocation(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            TokenKind kind = reader.Kind;
            NameNode name = ParseName(context, ref reader);
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw ParserHelper.Unexpected(ref reader, kind);
        }

        private static List<DirectiveNode> ParseDirectives(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            if (reader.Kind == TokenKind.At)
            {
                var list = new List<DirectiveNode>();

                while (reader.Kind == TokenKind.At)
                {
                    list.Add(ParseDirective(context, ref reader, isConstant));
                }

                return list;
            }

            return _emptyDirectives;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DirectiveNode ParseDirective(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            TokenInfo start = TokenInfo.FromReader(in _reader);

            ParserHelper.ExpectAt(ref reader);
            NameNode name = ParseName(context, ref reader);
            List<ArgumentNode> arguments =
                ParseArguments(context, ref reader, isConstant);

            Location location = CreateLocation(in start);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }
    }
}
