using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Fragments section.
    public partial class Utf8Parser
    {
        /// <summary>
        /// Parses a fragment spred or inline fragment within a selection set.
        /// <see cref="ParseFragmentSpread" /> and
        /// <see cref="ParseInlineFragment" />.
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ISelectionNode ParseFragment(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            ParserHelper.ExpectSpread(in reader);
            var isOnKeyword = reader.Value.SequenceEqual(Utf8Keywords.On);

            if (!isOnKeyword && TokenHelper.IsName(in reader))
            {
                return ParseFragmentSpread(context, in reader);
            }

            NamedTypeNode typeCondition = null;
            if (isOnKeyword)
            {
                reader.Read();
                typeCondition = ParseNamedType(context, in reader);
            }

            return ParseInlineFragment(context, in reader, typeCondition);
        }

        /// <summary>
        /// Parses a fragment definition.
        /// <see cref="FragmentDefinitionNode" />:
        /// fragment FragmentName on TypeCondition Directives? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static FragmentDefinitionNode ParseFragmentDefinition(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            ParserHelper.ExpectFragmentKeyword(in reader);

            // Experimental support for defining variables within fragments
            // changesthe grammar of FragmentDefinition:
            // fragment FragmentName VariableDefinitions? on TypeCondition Directives? SelectionSet
            if (context.Options.Experimental.AllowFragmentVariables)
            {
                NameNode name = ParseFragmentName(context, in reader);
                List<VariableDefinitionNode> variableDefinitions =
                  ParseVariableDefinitions(context, in reader);
                ParserHelper.ExpectOnKeyword(in reader);
                NamedTypeNode typeCondition = ParseNamedType(context, in reader);
                List<DirectiveNode> directives =
                    ParseDirectives(context, in reader, false);
                SelectionSetNode selectionSet = ParseSelectionSet(context, in reader);
                Location location = context.CreateLocation(in reader);

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
                NameNode name = ParseFragmentName(context, in reader);
                ParserHelper.ExpectOnKeyword(in reader);
                NamedTypeNode typeCondition = ParseNamedType(context, in reader);
                List<DirectiveNode> directives =
                    ParseDirectives(context, in reader, false);
                SelectionSetNode selectionSet = ParseSelectionSet(context, in reader);
                Location location = context.CreateLocation(in reader);

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
            in Utf8GraphQLReader reader)
        {
            NameNode name = ParseFragmentName(context, in reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, false);
            Location location = context.CreateLocation(in reader);

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
            in Utf8GraphQLReader reader,
            NamedTypeNode typeCondition)
        {
            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context, in reader);
            Location location = context.CreateLocation(in reader);

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
            in Utf8GraphQLReader reader)
        {
            if (reader.Value.SequenceEqual(Utf8Keywords.On))
            {
                throw ParserHelper.Unexpected(in reader, reader.Kind);
            }
            return ParseName(context, in reader);
        }
    }
}
