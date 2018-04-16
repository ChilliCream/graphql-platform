using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public partial class Parser
    {
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
            Token start = context.Current;
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
        private DirectiveDefinitionNode ParseDirectiveDefinition(ParserContext context)
        {
            Token start = context.Current;
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

        /**
         * DirectiveLocations :
         *   - `|`? DirectiveLocation
         *   - DirectiveLocations | DirectiveLocation
         */
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

        /*
         * DirectiveLocation :
         *   - ExecutableDirectiveLocation
         *   - TypeSystemDirectiveLocation
         *
         * ExecutableDirectiveLocation : one of
         *   `QUERY`
         *   `MUTATION`
         *   `SUBSCRIPTION`
         *   `FIELD`
         *   `FRAGMENT_DEFINITION`
         *   `FRAGMENT_SPREAD`
         *   `INLINE_FRAGMENT`
         *
         * TypeSystemDirectiveLocation : one of
         *   `SCHEMA`
         *   `SCALAR`
         *   `OBJECT`
         *   `FIELD_DEFINITION`
         *   `ARGUMENT_DEFINITION`
         *   `INTERFACE`
         *   `UNION`
         *   `ENUM`
         *   `ENUM_VALUE`
         *   `INPUT_OBJECT`
         *   `INPUT_FIELD_DEFINITION`
         */
        private NameNode ParseDirectiveLocation(ParserContext context)
        {
            Token start = context.Current;
            NameNode name = ParseName(context);
            if (DirectiveLocation.IsValidName(name.Value))
            {
                return name;
            }
            throw context.Unexpected(start);
        }
    }
}