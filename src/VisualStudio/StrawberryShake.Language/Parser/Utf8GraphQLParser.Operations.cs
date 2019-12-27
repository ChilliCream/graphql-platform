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
        private OperationDefinitionNode ParseOperationDefinition()
        {
            TokenInfo start = Start();

            OperationType operation = ParseOperationType();
            NameNode? name = _reader.Kind == TokenKind.Name
                ? ParseName()
                : null;
            List<VariableDefinitionNode> variableDefinitions =
                ParseVariableDefinitions();
            List<DirectiveNode> directives =
                ParseDirectives(false);
            SelectionSetNode selectionSet = ParseSelectionSet();
            Location? location = CreateLocation(in start);

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
        private OperationDefinitionNode ParseShortOperationDefinition()
        {
            TokenInfo start = Start();
            SelectionSetNode selectionSet = ParseSelectionSet();
            Location? location = CreateLocation(in start);

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
        private OperationType ParseOperationType()
        {
            if (_reader.Kind == TokenKind.Name)
            {
                if (_reader.Value.SequenceEqual(GraphQLKeywords.Query))
                {
                    MoveNext();
                    return OperationType.Query;
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Mutation))
                {
                    MoveNext();
                    return OperationType.Mutation;
                }

                if (_reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                {
                    MoveNext();
                    return OperationType.Subscription;
                }
            }

            throw Unexpected(TokenKind.Name);
        }

        /// <summary>
        /// Parses variable definitions.
        /// <see cref="IEnumerable{VariableDefinitionNode}" />:
        /// ( VariableDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<VariableDefinitionNode> ParseVariableDefinitions()
        {
            if (_reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<VariableDefinitionNode>();

                // skip opening token
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis)
                {
                    list.Add(ParseVariableDefinition());
                }

                // skip closing token
                ExpectRightParenthesis();

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
        private VariableDefinitionNode ParseVariableDefinition()
        {
            TokenInfo start = Start();

            VariableNode variable = ParseVariable();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            IValueNode? defaultValue = SkipEqual()
                ? ParseValueLiteral(true)
                : null;
            List<DirectiveNode> directives =
                ParseDirectives(true);

            Location? location = CreateLocation(in start);

            return new VariableDefinitionNode
            (
                location,
                variable,
                type,
                defaultValue,
                directives
            );
        }

        /// <summary>
        /// Parse a variable.
        /// <see cref="VariableNode" />:
        /// $Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private VariableNode ParseVariable()
        {
            TokenInfo start = Start();
            ExpectDollar();
            NameNode name = ParseName();
            Location? location = CreateLocation(in start);

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
        private SelectionSetNode ParseSelectionSet()
        {
            TokenInfo start = Start();

            if (_reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var selections = new List<ISelectionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                selections.Add(ParseSelection());
            }

            // skip closing token
            ExpectRightBrace();

            Location? location = CreateLocation(in start);

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
        private ISelectionNode ParseSelection()
        {
            if (_reader.Kind == TokenKind.Spread)
            {
                return ParseFragment();
            }
            return ParseField();
        }

        /// <summary>
        /// Parses a field.
        /// <see cref="FieldNode"  />:
        /// Alias? : Name Arguments? Directives? SelectionSet?
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FieldNode ParseField()
        {
            TokenInfo start = Start();

            NameNode name = ParseName();
            NameNode? alias = null;

            if (SkipColon())
            {
                alias = name;
                name = ParseName();
            }

            List<ArgumentNode> arguments = ParseArguments(false);
            List<DirectiveNode> directives = ParseDirectives(false);
            SelectionSetNode? selectionSet = _reader.Kind == TokenKind.LeftBrace
                ? ParseSelectionSet()
                : null;

            Location? location = CreateLocation(in start);

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
        private List<ArgumentNode> ParseArguments(bool isConstant)
        {
            if (_reader.Kind == TokenKind.LeftParenthesis)
            {
                var list = new List<ArgumentNode>();

                // skip opening token
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis)
                {
                    list.Add(ParseArgument(isConstant));
                }

                // skip closing token
                ExpectRightParenthesis();

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
        private ArgumentNode ParseArgument(bool isConstant)
        {
            TokenInfo start = Start();

            NameNode name = ParseName();
            ExpectColon();
            IValueNode value = ParseValueLiteral(isConstant);

            Location? location = CreateLocation(in start);

            return new ArgumentNode
            (
                location,
                name,
                value
            );
        }
    }
}
