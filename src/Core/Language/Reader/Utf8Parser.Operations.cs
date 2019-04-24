using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Operations section.
    public partial class Utf8Parser
    {
        /// <summary>
        /// Parses an operation definition.
        /// <see cref="OperationDefinitionNode" />:
        /// OperationType? OperationName? ($x : Type = DefaultValue?)? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static OperationDefinitionNode ParseOperationDefinition(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            if (reader.Kind == TokenKind.LeftBrace)
            {
                return ParseOperationDefinitionShortHandForm(context, in reader);
            }

            OperationType operation = ParseOperationType(context, in reader);
            NameNode name = reader.Kind == TokenKind.Name
                ? ParseName(context, in reader)
                : null;
            List<VariableDefinitionNode> variableDefinitions =
                ParseVariableDefinitions(context, in reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, in reader, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context, in reader);
            Location location = context.CreateLocation(in reader);

            return new OperationDefinitionNode
            (
                location,
                name,
                operation,
                variableDefinitions,
                directives,
                selectionSet
            );
        }

        /// <summary>
        /// Parses a short-hand form operation definition.
        /// <see cref="OperationDefinitionNode" />:
        /// SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static OperationDefinitionNode ParseOperationDefinitionShortHandForm(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            SelectionSetNode selectionSet = ParseSelectionSet(context, in reader);
            Location location = context.CreateLocation(in reader);

            return new OperationDefinitionNode
            (
                location,
                null,
                OperationType.Query,
                Array.Empty<VariableDefinitionNode>(),
                Array.Empty<DirectiveNode>(),
                selectionSet
            );
        }

        /// <summary>
        /// Parses the <see cref="OperationType" />.
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static OperationType ParseOperationType(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            ParserHelper.ExpectName(in reader);

            if (reader.Value.SequenceEqual(Utf8Keywords.Query))
            {
                return OperationType.Query;
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.Mutation))
            {
                return OperationType.Mutation;
            }

            if (reader.Value.SequenceEqual(Utf8Keywords.Subscription))
            {
                return OperationType.Subscription;
            }

            throw ParserHelper.Unexpected(in reader, TokenKind.Name);
        }

        /// <summary>
        /// Parses variable definitions.
        /// <see cref="IEnumerable{VariableDefinitionNode}" />:
        /// ( VariableDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<VariableDefinitionNode> ParseVariableDefinitions(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<VariableDefinitionNode>();

                // skip opening token
                reader.Read();

                while (reader.Kind != TokenKind.LeftParenthesis)
                {
                    list.Add(ParseVariableDefinition(context, in reader));
                }

                // skip closing token
                ParserHelper.Expect(in reader, TokenKind.RightParenthesis);

                return list;
            }
            return new List<VariableDefinitionNode>();
        }

        /// <summary>
        /// Parses a variable definition.
        /// <see cref="VariableDefinitionNode" />:
        /// $variable : Type = DefaultValue?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static VariableDefinitionNode ParseVariableDefinition(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            VariableNode variable = ParseVariable(context, in reader);
            ParserHelper.ExpectColon(in reader);
            ITypeNode type = ParseTypeReference(context, in reader);
            IValueNode defaultValue = ParserHelper.Skip(in reader, TokenKind.Equal)
                ? ParseValueLiteral(context, in reader, true)
                : null;

            Location location = context.CreateLocation(in reader);

            return new VariableDefinitionNode
            (
                location,
                variable,
                type,
                defaultValue
            );
        }

        /// <summary>
        /// Parse a variable.
        /// <see cref="VariableNode" />:
        /// $Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static VariableNode ParseVariable(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            ParserHelper.ExpectDollar(in reader);
            NameNode name = ParseName(context, in reader);
            Location location = context.CreateLocation(in reader);

            return new VariableNode
            (
                location,
                name
            );
        }

        /// <summary>
        /// Parses a selection set.
        /// <see cref="SelectionSetNode" />:
        /// { Selection+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static SelectionSetNode ParseSelectionSet(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            if (reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(reader)));
            }

            var selections = new List<ISelectionNode>();

            // skip opening token
            reader.Read();

            while (reader.Kind != TokenKind.RightBrace)
            {
                selections.Add(ParseSelection(context, in reader));
            }

            // skip closing token
            ParserHelper.Expect(in reader, TokenKind.RightBrace);

            Location location = context.CreateLocation(in reader);

            return new SelectionSetNode
            (
                location,
                selections
            );
        }

        /// <summary>
        /// Parses a selection.
        /// <see cref="ISelectionNode" />:
        /// - Field
        /// - FragmentSpread
        /// - InlineFragment
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ISelectionNode ParseSelection(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsSpread(in reader))
            {
                return ParseFragment(context, in reader);
            }
            return ParseField(context, in reader);
        }

        /// <summary>
        /// Parses a field.
        /// <see cref="FieldNode"  />:
        /// Alias? : Name Arguments? Directives? SelectionSet?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static FieldNode ParseField(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            NameNode name = ParseName(context, in reader);
            NameNode alias = null;

            if (TokenHelper.IsColon(in reader))
            {
                alias = name;
                name = ParseName(context, in reader);
            }

            List<ArgumentNode> arguments = ParseArguments(context, in reader, false);
            List<DirectiveNode> directives = ParseDirectives(context, in reader, false);
            SelectionSetNode selectionSet = TokenHelper.IsLeftBrace(in reader)
                ? ParseSelectionSet(context, in reader)
                : null;

            Location location = context.CreateLocation(in reader);

            return new FieldNode
            (
                location,
                name,
                alias,
                directives,
                arguments,
                selectionSet
            );
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<ArgumentNode> ParseArguments(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            if (reader.Kind != TokenKind.LeftParenthesis)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftParenthesis,
                        TokenVisualizer.Visualize(reader)));
            }

            var list = new List<ArgumentNode>();

            // skip opening token
            reader.Read();

            while (reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseArgument(context, in reader, isConstant));
            }

            // skip closing token
            ParserHelper.Expect(in reader, TokenKind.RightParenthesis);

            return list;
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        /// <param name="context">The parser context.</param>
        internal static ArgumentNode ParseArgument(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(in reader);

            NameNode name = ParseName(context, in reader);
            ParserHelper.ExpectColon(in reader);
            IValueNode value = ParseValueLiteral(context, in reader, isConstant);

            Location location = context.CreateLocation(in reader);

            return new ArgumentNode
            (
                location,
                name,
                value
            );
        }
    }
}
