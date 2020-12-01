using System;
using System.Collections.Generic;
using System.Globalization;
using StrawberryShake.VisualStudio.Language.Properties;
using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Values section.
    public ref partial struct StringGraphQLParser
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
        private IValueNode ParseValueLiteral(bool isConstant)
        {
            if (_reader.Kind == TokenKind.LeftBracket)
            {
                return ParseList(isConstant);
            }

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                return ParseObject(isConstant);
            }

            if (_isString[(int)_reader.Kind])
            {
                return ParseStringLiteral();
            }

            if (_isScalar[(int)_reader.Kind])
            {
                return ParseNonStringScalarValue();
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


        private StringValueNode ParseStringLiteral()
        {
            ISyntaxToken start = _reader.Token;

            bool isBlock = _reader.Kind == TokenKind.BlockString;
            string value = ExpectString();
            var location = new Location(start, _reader.Token);

            return new StringValueNode(location, value, isBlock);
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

        private ListValueNode ParseList(bool isConstant)
        {
            ISyntaxToken start = _reader.Token;

            if (_reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var items = new List<IValueNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                items.Add(ParseValueLiteral(isConstant));
            }

            // skip closing token
            Expect(TokenKind.RightBracket);

            var location = new Location(start, _reader.Token);

            return new ListValueNode
            (
                location,
                items
            );
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

        private ObjectValueNode ParseObject(bool isConstant)
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

            var fields = new List<ObjectFieldNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                fields.Add(ParseObjectField(isConstant));
            }

            // skip closing token
            Expect(TokenKind.RightBrace);

            var location = new Location(start, _reader.Token);

            return new ObjectValueNode
            (
                location,
                fields
            );
        }


        private ObjectFieldNode ParseObjectField(bool isConstant)
        {
            ISyntaxToken start = _reader.Token;

            NameNode name = ParseName();

            ExpectColon();

            IValueNode value = ParseValueLiteral(isConstant);

            var location = new Location(start, _reader.Token);

            return new ObjectFieldNode
            (
                location,
                name,
                value
            );
        }


        private unsafe IValueNode ParseNonStringScalarValue()
        {
            ISyntaxToken start = _reader.Token;
            TokenKind kind = _reader.Kind;

            fixed (char* c = _reader.Value)
            {
                string value = new string(c, 0, _reader.Value.Length);
                FloatFormat? format = _reader.FloatFormat;
                MoveNext();

                var location = new Location(start, _reader.Token);

                if (kind == TokenKind.Float)
                {
                    return new FloatValueNode
                    (
                        location,
                        value,
                        format ?? FloatFormat.FixedPoint
                    );
                }

                if (kind == TokenKind.Integer)
                {
                    return new IntValueNode
                    (
                        location,
                        value
                    );
                }

                throw Unexpected(kind);
            }
        }


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
