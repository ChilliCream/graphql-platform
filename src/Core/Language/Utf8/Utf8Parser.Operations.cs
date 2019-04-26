using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Operations section.
    public ref partial struct Utf8GraphQLParser
    {
        private static readonly List<VariableDefinitionNode> _emptyVariableDefinitions =
            new List<VariableDefinitionNode>();
        private static readonly List<ArgumentNode> _emptyArguments =
            new List<ArgumentNode>();


        /// <summary>
        /// Parses an operation definition.
        /// <see cref="OperationDefinitionNode" />:
        /// OperationType? OperationName? ($x : Type = DefaultValue?)? SelectionSet
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationDefinitionNode ParseOperationDefinition()
        {
            context.Start(ref reader);

            OperationType operation = ParseOperationType(context, ref reader);
            NameNode name = reader.Kind == TokenKind.Name
                ? ParseName(context, ref reader)
                : null;
            List<VariableDefinitionNode> variableDefinitions =
                ParseVariableDefinitions(context, ref reader);
            List<DirectiveNode> directives =
                ParseDirectives(context, ref reader, false);
            SelectionSetNode selectionSet = ParseSelectionSet(context, ref reader);
            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationDefinitionNode ParseOperationDefinitionShortHandForm(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);
            SelectionSetNode selectionSet = ParseSelectionSet(context, ref reader);
            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationType ParseOperationType(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.Name)
            {
                if (reader.Value.SequenceEqual(GraphQLKeywords.Query))
                {
                    ParserHelper.MoveNext(ref reader);
                    return OperationType.Query;
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Mutation))
                {
                    ParserHelper.MoveNext(ref reader);
                    return OperationType.Mutation;
                }

                if (reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                {
                    ParserHelper.MoveNext(ref reader);
                    return OperationType.Subscription;
                }
            }

            throw ParserHelper.Unexpected(ref reader, TokenKind.Name);
        }

        /// <summary>
        /// Parses variable definitions.
        /// <see cref="IEnumerable{VariableDefinitionNode}" />:
        /// ( VariableDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<VariableDefinitionNode> ParseVariableDefinitions(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<VariableDefinitionNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.RightParenthesis)
                {
                    list.Add(ParseVariableDefinition(context, ref reader));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightParenthesis);

                return list;
            }

            return _emptyVariableDefinitions;
        }

        /// <summary>
        /// Parses a variable definition.
        /// <see cref="VariableDefinitionNode" />:
        /// $variable : Type = DefaultValue?
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static VariableDefinitionNode ParseVariableDefinition(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            VariableNode variable = ParseVariable(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            ITypeNode type = ParseTypeReference(context, ref reader);
            IValueNode defaultValue = ParserHelper.Skip(ref reader, TokenKind.Equal)
                ? ParseValueLiteral(context, ref reader, true)
                : null;

            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static VariableNode ParseVariable(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);
            ParserHelper.ExpectDollar(ref reader);
            NameNode name = ParseName(context, ref reader);
            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SelectionSetNode ParseSelectionSet(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            if (reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in reader)));
            }

            var selections = new List<ISelectionNode>();

            // skip opening token
            ParserHelper.MoveNext(ref reader);

            while (reader.Kind != TokenKind.RightBrace)
            {
                selections.Add(ParseSelection(context, ref reader));
            }

            // skip closing token
            ParserHelper.Expect(ref reader, TokenKind.RightBrace);

            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ISelectionNode ParseSelection(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsSpread(ref reader))
            {
                return ParseFragment(context, ref reader);
            }
            return ParseField(context, ref reader);
        }

        /// <summary>
        /// Parses a field.
        /// <see cref="FieldNode"  />:
        /// Alias? : Name Arguments? Directives? SelectionSet?
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FieldNode ParseField(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            NameNode name = ParseName(context, ref reader);
            NameNode alias = null;

            if (ParserHelper.Skip(ref reader, TokenKind.Colon))
            {
                alias = name;
                name = ParseName(context, ref reader);
            }

            List<ArgumentNode> arguments = ParseArguments(context, ref reader, false);
            List<DirectiveNode> directives = ParseDirectives(context, ref reader, false);
            SelectionSetNode selectionSet = reader.Kind == TokenKind.LeftBrace
                ? ParseSelectionSet(context, ref reader)
                : null;

            Location location = context.CreateLocation(ref reader);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<ArgumentNode> ParseArguments(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            if (reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<ArgumentNode>();

                // skip opening token
                ParserHelper.MoveNext(ref reader);

                while (reader.Kind != TokenKind.RightParenthesis)
                {
                    list.Add(ParseArgument(context, ref reader, isConstant));
                }

                // skip closing token
                ParserHelper.Expect(ref reader, TokenKind.RightParenthesis);

                return list;
            }
            return _emptyArguments;
        }


        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ArgumentNode ParseArgument(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader,
            bool isConstant)
        {
            context.Start(ref reader);

            NameNode name = ParseName(context, ref reader);
            ParserHelper.ExpectColon(ref reader);
            IValueNode value = ParseValueLiteral(context, ref reader, isConstant);

            Location location = context.CreateLocation(ref reader);

            return new ArgumentNode
            (
                location,
                name,
                value
            );
        }
    }
}
