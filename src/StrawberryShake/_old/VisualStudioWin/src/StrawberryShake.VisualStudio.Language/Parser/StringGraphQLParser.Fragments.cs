using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Fragments section.
    public ref partial struct StringGraphQLParser
    {
        /// <summary>
        /// Parses a fragment spread or inline fragment within a selection set.
        /// <see cref="ParseFragmentSpread" /> and
        /// <see cref="ParseInlineFragment" />.
        /// </summary>
        /// <param name="context">The parser context.</param>

        private ISelectionNode ParseFragment()
        {
            ISyntaxToken start = _reader.Token;

            ExpectSpread();
            var isOnKeyword = _reader.Value.SequenceEqual(GraphQLKeywords.On);

            if (!isOnKeyword && _reader.Kind == TokenKind.Name)
            {
                return ParseFragmentSpread(start);
            }

            NamedTypeNode? typeCondition = null;

            if (isOnKeyword)
            {
                MoveNext();
                typeCondition = ParseNamedType();
            }

            return ParseInlineFragment(start, typeCondition);
        }

        /// <summary>
        /// Parses a fragment definition.
        /// <see cref="FragmentDefinitionNode" />:
        /// fragment FragmentName on TypeCondition Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private FragmentDefinitionNode ParseFragmentDefinition()
        {
            ISyntaxToken start = _reader.Token;

            ExpectFragmentKeyword();

            NameNode name = ParseFragmentName();
            ExpectOnKeyword();
            NamedTypeNode typeCondition = ParseNamedType();
            List<DirectiveNode> directives = ParseDirectives(false);
            SelectionSetNode selectionSet = ParseSelectionSet();
            var location = new Location(start, _reader.Token);

            return new FragmentDefinitionNode
            (
              location,
              name,
              typeCondition,
              directives,
              selectionSet
            );
        }

        /// <summary>
        /// Parses a fragment spread.
        /// <see cref="FragmentSpreadNode" />:
        /// ... FragmentName Directives?
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="start">
        /// The start token of the current fragment node.
        /// </param>
        private FragmentSpreadNode ParseFragmentSpread(ISyntaxToken start)
        {
            NameNode name = ParseFragmentName();
            List<DirectiveNode> directives = ParseDirectives(false);
            var location = new Location(start, _reader.Token);

            return new FragmentSpreadNode
            (
                location,
                name,
                directives
            );
        }

        /// <summary>
        /// Parses an inline fragment.
        /// <see cref="FragmentSpreadNode" />:
        /// ... TypeCondition? Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="start">
        /// The start token of the current fragment node.
        /// </param>
        /// <param name="typeCondition">
        /// The fragment type condition.
        /// </param>
        private InlineFragmentNode ParseInlineFragment(
            ISyntaxToken start,
            NamedTypeNode? typeCondition)
        {
            List<DirectiveNode> directives = ParseDirectives(false);
            SelectionSetNode selectionSet = ParseSelectionSet();
            var location = new Location(start, _reader.Token);

            return new InlineFragmentNode
            (
                location,
                typeCondition,
                directives,
                selectionSet
            );
        }

        /// <summary>
        /// Parse fragment name.
        /// <see cref="NameNode" />:
        /// Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        private NameNode ParseFragmentName()
        {
            if (_reader.Value.SequenceEqual(GraphQLKeywords.On))
            {
                throw Unexpected(_reader.Kind);
            }
            return ParseName();
        }
    }
}
