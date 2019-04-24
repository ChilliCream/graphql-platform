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
            ParserContext context)
        {
            SyntaxToken start = context.Current;

            if (start.IsLeftBrace())
            {
                return ParseOperationDefinitionShortHandForm(context, start);
            }

            OperationType operation = ParseOperationType(context);
            NameNode name = context.Current.IsName()
                ? context.ParseName()
                : null;
            List<VariableDefinitionNode> variableDefinitions =
                ParseVariableDefinitions(context);
            List<DirectiveNode> directives =
                ParseDirectives(context, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context);
            Location location = context.CreateLocation(start);

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
            Location location = context.CreateLocation(start);

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
            SyntaxToken token = context.ExpectName();

            switch (token.Value)
            {
                case Keywords.Query:
                    return OperationType.Query;
                case Keywords.Mutation:
                    return OperationType.Mutation;
                case Keywords.Subscription:
                    return OperationType.Subscription;
            }

            throw context.Unexpected(token);
        }

        /// <summary>
        /// Parses variable definitions.
        /// <see cref="IEnumerable{VariableDefinitionNode}" />:
        /// ( VariableDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<VariableDefinitionNode> ParseVariableDefinitions(
            ParserContext context)
        {
            if (context.Current.IsLeftParenthesis())
            {
                return ParseMany(context,
                    TokenKind.LeftParenthesis,
                    ParseVariableDefinition,
                    TokenKind.RightParenthesis);
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
            IValueNode defaultValue = context.Skip(TokenKind.Equal)
                ? ParseValueLiteral(context, true)
                : null;

            Location location = context.CreateLocation(start);

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
