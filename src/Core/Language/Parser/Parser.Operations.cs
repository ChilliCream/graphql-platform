using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    // Implements the parsing rules in the Operations section.
    public partial class Parser
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
            ParserContext context, SyntaxToken start)
        {
            SelectionSetNode selectionSet = ParseSelectionSet(context);
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
        private static OperationType ParseOperationType(ParserContext context)
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
            ParserContext context)
        {
            SyntaxToken start = context.Current;
            VariableNode variable = ParseVariable(context);
            context.ExpectColon();
            ITypeNode type = ParseTypeReference(context);
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
        private static VariableNode ParseVariable(ParserContext context)
        {
            SyntaxToken start = context.ExpectDollar();
            NameNode name = ParseName(context);
            Location location = context.CreateLocation(start);

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
        private static SelectionSetNode ParseSelectionSet(ParserContext context)
        {
            SyntaxToken start = context.Current;
            List<ISelectionNode> selections = ParseMany(context,
                TokenKind.LeftBrace,
                ParseSelection,
                TokenKind.RightBrace);
            Location location = context.CreateLocation(start);

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
        private static ISelectionNode ParseSelection(ParserContext context)
        {
            if (context.Current.IsSpread())
            {
                return ParseFragment(context);
            }
            return ParseField(context);
        }

        /// <summary>
        /// Parses a field.
        /// <see cref="FieldNode"  />:
        /// Alias? : Name Arguments? Directives? SelectionSet?
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static FieldNode ParseField(ParserContext context)
        {
            SyntaxToken start = context.Current;
            var hasAlias = context.Peek(TokenKind.Colon);
            NameNode alias = null;
            NameNode name = null;

            if (hasAlias)
            {
                alias = ParseName(context);
                context.ExpectColon();
                name = ParseName(context);
            }
            else
            {
                name = ParseName(context);
            }

            List<ArgumentNode> arguments = ParseArguments(context, false);
            List<DirectiveNode> directives = ParseDirectives(context, false);
            SelectionSetNode selectionSet = context.Current.IsLeftBrace()
                ? ParseSelectionSet(context)
                : null;
            Location location = context.CreateLocation(start);

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
        /// Parses a collection of arguments.
        /// <see cref="IReadOnlyCollection{ArgumentNode}" />:
        /// ( Argument[isConstant]+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static List<ArgumentNode> ParseArguments(
            ParserContext context, bool isConstant)
        {
            if (isConstant)
            {
                return ParseArguments(context, ParseConstantArgument);
            }
            return ParseArguments(context, ParseArgument);
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        /// <param name="context">The parser context.</param>
        internal static List<ArgumentNode> ParseArguments(
            ParserContext context,
            Func<ParserContext, ArgumentNode> parseArgument)
        {
            if (context.Current.IsLeftParenthesis())
            {
                return ParseMany(
                    context,
                    TokenKind.LeftParenthesis,
                    parseArgument,
                    TokenKind.RightParenthesis);
            }
            return new List<ArgumentNode>();
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant=true]
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ArgumentNode ParseConstantArgument(
            ParserContext context)
        {
            return ParseArgument(context, ParseConstantValue);
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant=false]
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ArgumentNode ParseArgument(ParserContext context)
        {
            return ParseArgument(context, c => ParseValueLiteral(c, false));
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value
        /// </summary>
        /// <param name="context">The parser context.</param>
        internal static ArgumentNode ParseArgument(
            ParserContext context,
            Func<ParserContext, IValueNode> parseValue)
        {
            SyntaxToken start = context.Current;
            NameNode name = ParseName(context);
            context.ExpectColon();
            IValueNode value = parseValue(context);
            Location location = context.CreateLocation(start);

            return new ArgumentNode
            (
                location,
                name,
                value
            );
        }
    }
}
