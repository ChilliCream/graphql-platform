using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private static DirectiveDefinitionNode ParseDirectiveDefinition(
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectDirectiveKeyword();
            context.ExpectAt();
            NameNode name = ParseName(context);
            List<InputValueDefinitionNode> arguments =
                ParseArgumentDefinitions(context);
            context.ExpectOnKeyword();
            List<NameNode> locations =
                ParseDirectiveLocations(context);
            Location location = context.CreateLocation(start);

            return new DirectiveDefinitionNode
            (
                location,
                name,
                description,
                arguments,
                locations
            );
        }

        private static List<NameNode> ParseDirectiveLocations(
            ParserContext context)
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
