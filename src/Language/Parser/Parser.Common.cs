using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private NameNode ParseName(ParserContext context)
        {
            Token token = context.ExpectName();
            Location location = context.CreateLocation(token);

            return new NameNode
            (
                location,
                token.Value
            );
        }

        private List<T> ParseMany<T>(
            ParserContext context,
            TokenKind openKind,
            Func<ParserContext, T> parser,
            TokenKind closeKind)
        {
            if (context.Current.Kind != openKind)
            {
                throw new SyntaxException(context,
                    $"Expected a name token: {context.Current}.");
            }

            List<T> list = new List<T>();

            // skip opening token
            context.MoveNext();

            while (context.Current.Kind != closeKind) // todo : fix this
            {
                list.Add(parser(context));
            }

            // skip closing token
            context.MoveNext();

            return list;
        }

       /// <summary>
        /// Parses an inline fragment.
        /// <see cref="FragmentSpreadNode" />:
        /// ... TypeCondition? Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="start">The start token of the current fragment node.</param>
        private InlineFragmentNode ParseInlineFragment(
          ParserContext context, Token start,
          NamedTypeNode typeCondition)
        {
            NameNode name = ParseFragmentName(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context);
            Location location = context.CreateLocation(start);

            return new InlineFragmentNode
            (
                location,
                typeCondition,
                directives,
                selectionSet
            );
        }
    }
}