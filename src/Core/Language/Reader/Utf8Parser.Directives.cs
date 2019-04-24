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
            context.Skip(TokenKind.Pipe);

            do
            {
                list.Add(ParseDirectiveLocation(context));
            }
            while (context.Skip(TokenKind.Pipe));

            return list;
        }

        private static NameNode ParseDirectiveLocation(ParserContext context)
        {
            SyntaxToken start = context.Current;
            NameNode name = ParseName(context);
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw context.Unexpected(start);
        }

        private static List<DirectiveNode> ParseDirectives(
            ParserContext context, bool isConstant)
        {
            var list = new List<DirectiveNode>();

            while (context.Current.IsAt())
            {
                list.Add(ParseDirective(context, isConstant));
            }

            return list;
        }

        private static DirectiveNode ParseDirective(
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;
            context.ExpectAt();
            NameNode name = ParseName(context);
            List<ArgumentNode> arguments =
                ParseArguments(context, isConstant);
            Location location = context.CreateLocation(start);

            return new DirectiveNode
            (
                location,
                name,
                arguments
            );
        }
    }
}
