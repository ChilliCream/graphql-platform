using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Fragments section.
    public ref partial struct Utf8GraphQLParser
    {
        /// <summary>
        /// Parses a fragment spred or inline fragment within a selection set.
        /// <see cref="ParseFragmentSpread" /> and
        /// <see cref="ParseInlineFragment" />.
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ISelectionNode ParseFragment()
        {
            TokenInfo start = Start();

            ExpectSpread();
            var isOnKeyword = _reader.Value.SequenceEqual(GraphQLKeywords.On);

            if (!isOnKeyword && _reader.Kind == TokenKind.Name)
            {
                return ParseFragmentSpread(in start);
            }

            NamedTypeNode? typeCondition = null;

            if (isOnKeyword)
            {
                MoveNext();
                typeCondition = ParseNamedType();
            }

            return ParseInlineFragment(in start, typeCondition);
        }

        /// <summary>
        /// Parses a fragment definition.
        /// <see cref="FragmentDefinitionNode" />:
        /// fragment FragmentName on TypeCondition Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private FragmentDefinitionNode ParseFragmentDefinition()
        {
            TokenInfo start = Start();

            ExpectFragmentKeyword();

            // Experimental support for defining variables within fragments
            // changesthe grammar of FragmentDefinition:
            // fragment FragmentName VariableDefinitions? on
            //    TypeCondition Directives? SelectionSet
            if (_allowFragmentVars)
            {
                NameNode name = ParseFragmentName();
                List<VariableDefinitionNode> variableDefinitions =
                  ParseVariableDefinitions();
                ExpectOnKeyword();
                NamedTypeNode typeCondition = ParseNamedType();
                List<DirectiveNode> directives = ParseDirectives(false);
                SelectionSetNode selectionSet = ParseSelectionSet();
                Location? location = CreateLocation(in start);

                return new FragmentDefinitionNode
                (
                  location,
                  name,
                  variableDefinitions,
                  typeCondition,
                  directives,
                  selectionSet
                );
            }
            else
            {
                NameNode name = ParseFragmentName();
                ExpectOnKeyword();
                NamedTypeNode typeCondition = ParseNamedType();
                List<DirectiveNode> directives = ParseDirectives(false);
                SelectionSetNode selectionSet = ParseSelectionSet();
                Location? location = CreateLocation(in start);

                return new FragmentDefinitionNode
                (
                  location,
                  name,
                  Array.Empty<VariableDefinitionNode>(),
                  typeCondition,
                  directives,
                  selectionSet
                );
            }
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
        private FragmentSpreadNode ParseFragmentSpread(in TokenInfo start)
        {
            NameNode name = ParseFragmentName();
            List<DirectiveNode> directives = ParseDirectives(false);
            Location? location = CreateLocation(in start);

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
            in TokenInfo start,
            NamedTypeNode? typeCondition)
        {
            List<DirectiveNode> directives = ParseDirectives(false);
            SelectionSetNode selectionSet = ParseSelectionSet();
            Location? location = CreateLocation(in start);

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
