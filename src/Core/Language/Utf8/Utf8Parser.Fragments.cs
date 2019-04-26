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
        private static ISelectionNode ParseFragment(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.ExpectSpread(ref reader);
            var isOnKeyword = reader.Value.SequenceEqual(GraphQLKeywords.On);

            if (!isOnKeyword && TokenHelper.IsName(ref reader))
            {
                return ParseFragmentSpread(context, ref reader);
            }

            NamedTypeNode typeCondition = null;
            if (isOnKeyword)
            {
                ParserHelper.MoveNext(ref reader);
                typeCondition = ParseNamedType(context, ref reader);
            }

            return ParseInlineFragment(context, ref reader, typeCondition);
        }

        /// <summary>
        /// Parses a fragment definition.
        /// <see cref="FragmentDefinitionNode" />:
        /// fragment FragmentName on TypeCondition Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static FragmentDefinitionNode ParseFragmentDefinition(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            ParserHelper.ExpectFragmentKeyword(ref reader);

            // Experimental support for defining variables within fragments
            // changesthe grammar of FragmentDefinition:
            // fragment FragmentName VariableDefinitions? on TypeCondition Directives? SelectionSet
            if (context.Options.Experimental.AllowFragmentVariables)
            {
                NameNode name = ParseFragmentName(context, ref reader);
                List<VariableDefinitionNode> variableDefinitions =
                  ParseVariableDefinitions(context, ref reader);
                ParserHelper.ExpectOnKeyword(ref reader);
                NamedTypeNode typeCondition = ParseNamedType(context, ref reader);
                List<DirectiveNode> directives =
                    ParseDirectives(context, ref reader, false);
                SelectionSetNode selectionSet = ParseSelectionSet(context, ref reader);
                Location location = context.CreateLocation(ref reader);

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
                NameNode name = ParseFragmentName(context, ref reader);
                ParserHelper.ExpectOnKeyword(ref reader);
                NamedTypeNode typeCondition = ParseNamedType(context, ref reader);
                List<DirectiveNode> directives =
                    ParseDirectives(context, ref reader, false);
                SelectionSetNode selectionSet = ParseSelectionSet(context, ref reader);
                Location location = context.CreateLocation(ref reader);

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
        private static FragmentSpreadNode ParseFragmentSpread(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            NameNode name = ParseFragmentName(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, false);
            Location location = context.CreateLocation(ref reader);

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
        private static InlineFragmentNode ParseInlineFragment(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            NamedTypeNode typeCondition)
        {
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context, ref reader);
            Location location = context.CreateLocation(ref reader);

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
        private static NameNode ParseFragmentName(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Value.SequenceEqual(GraphQLKeywords.On))
            {
                throw ParserHelper.Unexpected(ref reader, reader.Kind);
            }
            return ParseName(context, ref reader);
        }
    }
}
