using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private IValueNode ParseValueSyntax()
        {
            return _reader.Kind switch
            {
                TokenKind.LeftBracket => ParseListSyntax(),
                TokenKind.LeftBrace => ParseObjectSyntax(),
                TokenKind.String => ParseScalarSyntax(),
                TokenKind.Integer => ParseScalarSyntax(),
                TokenKind.Float => ParseScalarSyntax(),
                TokenKind.Name => ParseScalarSyntax(),
                _ => throw ThrowHelper.UnexpectedToken(_reader)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ObjectValueNode ParseObjectSyntax()
        {
            _reader.Expect(TokenKind.LeftBrace);

            var fields = new List<ObjectFieldNode>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                fields.Add(ParseObjectFieldSyntax());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return new ObjectValueNode(fields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ObjectFieldNode ParseObjectFieldSyntax()
        {
            if (_reader.Kind != TokenKind.String)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.String,
                        TokenVisualizer.Visualize(in _reader)));
            }

            string name = _reader.GetString();
            _reader.MoveNext();
            _reader.Expect(TokenKind.Colon);
            IValueNode value = ParseValueSyntax();

            return new ObjectFieldNode(name, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ListValueNode ParseListSyntax()
        {
            if (_reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var list = new List<IValueNode>();

            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                list.Add(ParseValueSyntax());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return new ListValueNode(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IValueNode ParseScalarSyntax()
        {
            switch (_reader.Kind)
            {
                case TokenKind.String:
                {
                    string value = _reader.GetString();
                    _reader.MoveNext();
                    return new StringValueNode(value);
                }

                case TokenKind.Integer:
                {
                    ReadOnlyMemory<byte> value = _reader.Value.ToArray();
                    _reader.MoveNext();
                    return new IntValueNode(null, value);
                }

                case TokenKind.Float:
                {
                    ReadOnlyMemory<byte> value = _reader.Value.ToArray();
                    FloatFormat? format = _reader.FloatFormat;
                    _reader.MoveNext();
                    return new FloatValueNode(null, value, format ?? FloatFormat.FixedPoint);
                }

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        _reader.MoveNext();
                        return BooleanValueNode.True;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        _reader.MoveNext();
                        return BooleanValueNode.False;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        _reader.MoveNext();
                        return NullValueNode.Default;
                    }

                    throw ThrowHelper.UnexpectedToken(_reader);

                default:
                    throw ThrowHelper.UnexpectedToken(_reader);
            }
        }
    }
}
