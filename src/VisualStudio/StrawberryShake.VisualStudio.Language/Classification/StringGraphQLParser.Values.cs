using System;
using System.Collections.Generic;
using System.Globalization;
using StrawberryShake.VisualStudio.Language.Properties;
using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Values section.
    public ref partial struct StringGraphQLClassifier
    {
        /// <summary>
        /// Parses a value.
        /// <see cref="IValueNode" />:
        /// - Variable [only if isConstant is <c>false</c>]
        /// - IntValue
        /// - FloatValue
        /// - StringValue
        /// - BooleanValue
        /// - NullValue
        /// - EnumValue
        /// - ListValue[isConstant]
        /// - ObjectValue[isConstant]
        /// <see cref="BooleanValueNode" />: true or false.
        /// <see cref="NullValueNode" />: null
        /// <see cref="EnumValueNode" />: Name but not true, false or null.
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        private void ParseValueLiteral(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            if (_reader.Kind == TokenKind.LeftBracket)
            {
                ParseList(classifications, isConstant);
            }

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                ParseObject(classifications, isConstant);
            }

            if (_isString[(int)_reader.Kind])
            {
                ParseStringLiteral(classifications);
            }

            if (_isScalar[(int)_reader.Kind])
            {
                ParseNonStringScalarValue();
            }

            if (_reader.Kind == TokenKind.Name)
            {
                return ParseEnumValue();
            }

            if (_reader.Kind == TokenKind.Dollar && !isConstant)
            {
                return ParseVariable();
            }

            throw Unexpected(_reader.Kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseStringLiteral(
            ICollection<SyntaxClassification> classifications,
            SyntaxClassificationKind kind = SyntaxClassificationKind.StringLiteral)
        {
            ISyntaxToken start = _reader.Token;

            bool isBlock = _reader.Kind == TokenKind.BlockString;
            string value = ExpectString();
            var location = new Location(start, _reader.Token);

            classifications.Add(new SyntaxClassification(
                kind, location.Start, location.Length));
        }

        /// <summary>
        /// Parses a list value.
        /// <see cref="ListValueNode" />:
        /// - [ ]
        /// - [ Value[isConstant]+ ]
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseList(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            if (_reader.Kind == TokenKind.LeftBracket)
            {
                // skip opening token
                classifications.AddClassification(
                    SyntaxClassificationKind.Bracket,
                    _reader.Token);
                MoveNext();

                while (_reader.Kind != TokenKind.RightBracket)
                {
                    ParseValueLiteral(classifications, isConstant);
                }

                // skip closing token
                classifications.AddClassification(
                    SyntaxClassificationKind.Bracket,
                    _reader.Token);
                Expect(TokenKind.RightBracket);
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
        }

        /// <summary>
        /// Parses an object value.
        /// <see cref="ObjectValueNode" />:
        /// - { }
        /// - { Value[isConstant]+ }
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <param name="isConstant">
        /// Defines if only constant values are allowed;
        /// otherwise, variables are allowed.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObject(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            ISyntaxToken start = _reader.Token;

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                // skip opening token
                classifications.AddClassification(SyntaxClassificationKind.Brace, _reader.Token);
                MoveNext(classifications);

                while (_reader.Kind != TokenKind.RightBrace)
                {
                    ParseObjectField(isConstant);
                }

                // skip closing token
                classifications.AddClassification(SyntaxClassificationKind.Brace, _reader.Token);
                Expect(classifications, SyntaxClassificationKind.Brace, TokenKind.RightBrace);
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(
            ICollection<SyntaxClassification> classifications,
            bool isConstant)
        {
            ISyntaxToken start = _reader.Token;

            ParseName(classifications, SyntaxClassificationKind.InputFieldIdentifier);

            ExpectColon();

            ParseValueLiteral(classifications, isConstant);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseNumberScalarValue(
            ICollection<SyntaxClassification> classifications)
        {
            if (_reader.Kind == TokenKind.Float
                || _reader.Kind == TokenKind.Integer)
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.NumberLiteral,
                    _reader.Token);
            }
            else
            {
                classifications.AddClassification(
                    SyntaxClassificationKind.Error,
                    _reader.Token);
            }

            MoveNext(classifications);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe IValueNode ParseEnumValue()
        {
            ISyntaxToken start = _reader.Token;

            Location location;

            if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
            {
                MoveNext();
                location = new Location(start, _reader.Token);
                return new BooleanValueNode(location, true);
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
            {
                MoveNext();
                location = new Location(start, _reader.Token);
                return new BooleanValueNode(location, false);
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
            {
                MoveNext();

                location = new Location(start, _reader.Token);
                return new NullValueNode(location);
            }

            fixed (char* c = _reader.Value)
            {
                var value = new string(c, 0, _reader.Value.Length);
                MoveNext();
                location = new Location(start, _reader.Token);

                return new EnumValueNode
                (
                    location,
                    value
                );
            }
        }
    }
}
