using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Operations section.
    public ref partial struct StringGraphQLClassifier
    {
        /// <summary>
        /// Parses an operation definition.
        /// <see cref="OperationDefinitionNode" />:
        /// OperationType? OperationName? ($x : Type = DefaultValue?)? SelectionSet
        /// </summary>
        private void ParseOperationDefinition()
        {
            ParseOperationType();

            if (_reader.Kind == TokenKind.Name)
            {
                ParseName(SyntaxClassificationKind.OperationIdentifier);
            }

            ParseVariableDefinitions();
            ParseDirectives(false);
            ParseSelectionSet();
        }

        /// <summary>
        /// Parses a short-hand form operation definition.
        /// <see cref="OperationDefinitionNode" />:
        /// SelectionSet
        /// </summary>
        private void ParseShortOperationDefinition() =>
            ParseSelectionSet();

        /// <summary>
        /// Parses the <see cref="OperationType" />.
        /// </summary>
        private void ParseOperationType()
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
        private void ParseVariableDefinitions()
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
        private void ParseVariableDefinition()
        {
            ParseVariableName(false);
            ParseColon();
            ParseTypeReference();

            if (SkipEqual())
            {
                ParseValueLiteral(true);
            }

            ParseDirectives(true);
        }

        /// <summary>
        /// Parse a variable.
        /// <see cref="VariableNode" />:
        /// $Name
        /// </summary>
        private void ParseVariableName(bool isReference)
        {
            ISyntaxToken start = _reader.Token;
            SyntaxClassificationKind classificationKind = isReference
                ? SyntaxClassificationKind.VariableReference
                : SyntaxClassificationKind.VariableIdentifier;

            if (_reader.Kind == TokenKind.Dollar)
            {
                MoveNext();
                classifications.AddClassification(
                    _reader.Kind == TokenKind.Name
                        ? classificationKind
                        : SyntaxClassificationKind.Error,
                    new Location(start, _reader.Token));
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }

            MoveNext();
        }

        /// <summary>
        /// Parses a selection set.
        /// <see cref="SelectionSetNode" />:
        /// { Selection+ }
        /// </summary>
        private void ParseSelectionSet()
        {
            if (_reader.Kind == TokenKind.LeftBrace)
            {
                // skip opening token
                classifications.AddClassification(
                    SyntaxClassificationKind.Brace,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    ParseSelection();
                }

                // skip closing token
                ParseRightBrace();
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
        }

        /// <summary>
        /// Parses a selection.
        /// <see cref="ISelectionNode" />:
        /// - Field
        /// - FragmentSpread
        /// - InlineFragment
        /// </summary>
        private void ParseSelection()
        {
            if (_reader.Kind == TokenKind.Spread)
            {
                ParseFragment();
            }
            else
            {
                ParseField();
            }
        }

        /// <summary>
        /// Parses a field.
        /// <see cref="FieldNode"  />:
        /// Alias? : Name Arguments? Directives? SelectionSet?
        /// </summary>
        private void ParseField()
        {
            ISyntaxToken start = _reader.Token;

            if (start.Kind == TokenKind.Name)
            {
                MoveNext();

                if (SkipColon())
                {
                    classifications.AddClassification(
                        SyntaxClassificationKind.FieldAlias,
                        start);
                    ParseName(SyntaxClassificationKind.FieldReference);
                }
                else
                {
                    classifications.AddClassification(
                        SyntaxClassificationKind.FieldReference,
                        start);
                }
            }

            ParseArguments(false);
            ParseDirectives(false);

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                ParseSelectionSet();
            }
        }

        /// <summary>
        /// Parses an argument.
        /// <see cref="ArgumentNode" />:
        /// Name : Value[isConstant]
        /// </summary>
        private void ParseArguments(bool isConstant)
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
        private void ParseArgument(bool isConstant)
        {
            ParseName(SyntaxClassificationKind.ArgumentReference);
            ParseColon();
            ParseValueLiteral(isConstant);
        }
    }
}
