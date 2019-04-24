using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class Utf8Parser
    {
        private static DirectiveDefinitionNode ParseDirectiveDefinition(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            StringValueNode description = ParseDescription(context, in reader);
            ParserHelper.ExpectDirectiveKeyword(in reader);
            ParserHelper.ExpectAt(in reader);
            NameNode name = ParseName(context, in reader);
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions(context, in reader);
            bool isRepeatable = ParserHelper.SkipRepeatableKeyword(in reader);
            ParserHelper.ExpectOnKeyword(in reader);
            List<NameNode> locations =
                ParseDirectiveLocations(context, in reader);

            Location location = context.CreateLocation(in reader);

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
            in Utf8GraphQLReader reader)
        {
            var list = new List<NameNode>();

            // skip optional leading pipe.
            ParserHelper.Skip(in reader, TokenKind.Pipe);

            do
            {
                list.Add(ParseDirectiveLocation(context, in reader));
            }
            while (ParserHelper.Skip(in reader, TokenKind.Pipe));

            return list;
        }

        private static NameNode ParseDirectiveLocation(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            TokenKind kind = reader.Kind;
            NameNode name = ParseName(context, in reader);
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw ParserHelper.Unexpected(in reader, kind);
        }

        private static List<DirectiveNode> ParseDirectives(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            var list = new List<DirectiveNode>();

            while (TokenHelper.IsAt(in reader))
            {
                list.Add(ParseDirective(context, in reader, isConstant));
            }

            return list;
        }

        private static DirectiveNode ParseDirective(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(in reader);

            ParserHelper.ExpectAt(in reader);
            NameNode name = ParseName(context, in reader);
            List<ArgumentNode> arguments =
                ParseArguments(context, in reader, isConstant);

            Location location = context.CreateLocation(in reader);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }
    }
}
