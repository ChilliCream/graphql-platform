using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private DirectiveDefinitionNode ParseDirectiveDefinition(ParserContext context)
        {
            SyntaxToken start = context.Current;
            StringValueNode description = ParseDescription(context);
            context.ExpectDirectiveKeyword();
            context.ExpectAt();
            NameNode name = context.ParseName();
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

        private List<NameNode> ParseDirectiveLocations(ParserContext context)
        {
            List<NameNode> list = new List<NameNode>();

            // skip optional leading pipe.
            context.Skip(TokenKind.Pipe);

            do
            {
                list.Add(ParseDirectiveLocation(context));
            }
            while (context.Skip(TokenKind.Pipe));

            return list;
        }

        private NameNode ParseDirectiveLocation(ParserContext context)
        {
            SyntaxToken start = context.Current;
            NameNode name = context.ParseName();
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw context.Unexpected(start);
        }

        private List<DirectiveNode> ParseDirectives(
            ParserContext context, bool isConstant)
        {
            List<DirectiveNode> list = new List<DirectiveNode>();

            while (context.Current.IsAt())
            {
                list.Add(ParseDirective(context, isConstant));
            }

            return list;
        }

        private DirectiveNode ParseDirective(
            ParserContext context, bool isConstant)
        {
            SyntaxToken start = context.Current;
            context.ExpectAt();
            NameNode name = context.ParseName();
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
