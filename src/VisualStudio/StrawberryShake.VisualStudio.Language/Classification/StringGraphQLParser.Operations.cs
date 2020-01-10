using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using StrawberryShake.VisualStudio.Language.Properties;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Operations section.
    public ref partial struct StringGraphQLClassifier
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
        private OperationDefinitionNode ParseOperationDefinition(
            ICollection<SyntaxClassification> classifications)
        {
            ISyntaxToken start = _reader.Token;

            ParseOperationType(classifications);

            if (_reader.Kind == TokenKind.Name)
            {
                ParseName(classifications, SyntaxClassificationKind.Identifier);
            }

            ParseVariableDefinitions(classifications);
            ParseDirectives(false);
            SelectionSetNode selectionSet = ParseSelectionSet();
            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;
            SelectionSetNode selectionSet = ParseSelectionSet();
            var location = new Location(start, _reader.Token);

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
        private void ParseOperationType(
            ICollection<SyntaxClassification> classifications)
        {
            SyntaxClassificationKind kind = (_reader.Kind == TokenKind.Name
                || _reader.Value.SequenceEqual(GraphQLKeywords.Query)
                || _reader.Value.SequenceEqual(GraphQLKeywords.Mutation)
                || _reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
                ? SyntaxClassificationKind.OperationKind
                : SyntaxClassificationKind.Error;
            classifications.AddClassification(kind, _reader.Token);
            MoveNext();
        }

        /// <summary>
        /// Parses variable definitions.
        /// <see cref="IEnumerable{VariableDefinitionNode}" />:
        /// ( VariableDefinition+ )
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseVariableDefinitions(
            ICollection<SyntaxClassification> classifications)
        {
            if (_reader.Kind == TokenKind.LeftParenthesis)
            {
                // skip opening token
                classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis)
                {
                    ParseVariableDefinition();
                }

                // skip closing token
                classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                ExpectRightParenthesis();
            }
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
            ISyntaxToken start = _reader.Token;

            VariableNode variable = ParseVariable();
            ExpectColon();
            ITypeNode type = ParseTypeReference();
            IValueNode? defaultValue = SkipEqual()
                ? ParseValueLiteral(true)
                : null;
            List<DirectiveNode> directives = ParseDirectives(true);

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;
            ExpectDollar();
            NameNode name = ParseName();
            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

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

            var location = new Location(start, _reader.Token);

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
            ISyntaxToken start = _reader.Token;

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

            var location = new Location(start, _reader.Token);

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
        private void ParseArguments(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            if (_reader.Kind == TokenKind.LeftParenthesis)
            {
                // skip opening token
                classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightParenthesis)
                {
                    ParseArgument(isConstant);
                }

                // skip closing token
                classifications.AddClassification(
                    SyntaxClassificationKind.Parenthesis,
                    _reader.Token);
                ExpectRightParenthesis();
            }
        }


        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        /// <param name="context">The parser context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseArgument(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            classifications.AddClassification(
                SyntaxClassificationKind.ArgumentIdentifier,
                _reader.Token);
            ExpectName();

            ExpectColon();

            ParseValueLiteral(isConstant);
        }
    }
}
