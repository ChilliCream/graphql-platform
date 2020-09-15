using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private object? ParseValue(bool preserveNumbers)
        {
            return _reader.Kind switch
            {
                TokenKind.LeftBracket => ParseList(preserveNumbers),
                TokenKind.LeftBrace => ParseObject(preserveNumbers),
                TokenKind.String => ParseScalar(preserveNumbers),
                TokenKind.Integer => ParseScalar(preserveNumbers),
                TokenKind.Float => ParseScalar(preserveNumbers),
                TokenKind.Name => ParseScalar(preserveNumbers),
                _ => throw ThrowHelper.UnexpectedToken(_reader)
            };
        }

        private int SkipValue()
        {
            return _reader.Kind switch
            {
                TokenKind.LeftBracket => SkipList(),
                TokenKind.LeftBrace => SkipObject(),
                TokenKind.String => SkipScalar(),
                TokenKind.Integer => SkipScalar(),
                TokenKind.Float => SkipScalar(),
                TokenKind.Name => SkipScalar(),
                _ => throw ThrowHelper.UnexpectedToken(_reader)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyDictionary<string, object?> ParseObject(bool preserveNumbers)
        {
            _reader.Expect(TokenKind.LeftBrace);

            var obj = new Dictionary<string, object?>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseObjectField(obj, preserveNumbers);
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SkipObject()
        {
            _reader.Expect(TokenKind.LeftBrace);

            while (_reader.Kind != TokenKind.RightBrace)
            {
                SkipObjectField();
            }

            // skip closing token
            var end = _reader.End;
            _reader.Expect(TokenKind.RightBrace);
            return end;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(IDictionary<string, object?> obj, bool preserveNumbers)
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
            object? value = ParseValue(preserveNumbers);
            obj.Add(name, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipObjectField()
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

            _reader.MoveNext();
            _reader.Expect(TokenKind.Colon);
            SkipValue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyList<object?> ParseList(bool preserveNumbers)
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

            var list = new List<object?>();

            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                list.Add(ParseValue(preserveNumbers));
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SkipList()
        {
            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                SkipValue();
            }

            // skip closing token
            var end = _reader.End;
            _reader.Expect(TokenKind.RightBracket);
            return end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object? ParseScalar(bool preserveNumbers)
        {
            string? value;

            switch (_reader.Kind)
            {
                case TokenKind.String:
                    value = _reader.GetString();
                    _reader.MoveNext();
                    return value;

                case TokenKind.Integer when preserveNumbers:
                    IValueNode integerLiteral = Utf8GraphQLParser.Syntax.ParseValueLiteral(_reader);
                    _reader.MoveNext();
                    return integerLiteral;

                case TokenKind.Integer:
                    value = _reader.GetScalarValue();
                    _reader.MoveNext();
                    return long.Parse(value, CultureInfo.InvariantCulture);

                case TokenKind.Float when preserveNumbers:
                    IValueNode floatLiteral =  Utf8GraphQLParser.Syntax.ParseValueLiteral(_reader);
                    _reader.MoveNext();
                    return floatLiteral;

                case TokenKind.Float:
                    value = _reader.GetScalarValue();
                    _reader.MoveNext();
                    return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        _reader.MoveNext();
                        return true;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        _reader.MoveNext();
                        return false;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        _reader.MoveNext();
                        return null;
                    }

                    throw ThrowHelper.UnexpectedToken(_reader);

                default:
                    throw ThrowHelper.UnexpectedToken(_reader);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SkipScalar()
        {
            var end = _reader.End;

            switch (_reader.Kind)
            {
                case TokenKind.String:
                    _reader.MoveNext();
                    return end;

                case TokenKind.Integer:
                    _reader.MoveNext();
                    return end;

                case TokenKind.Float:
                    _reader.MoveNext();
                    return end;

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        _reader.MoveNext();
                        return end;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        _reader.MoveNext();
                        return end;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        _reader.MoveNext();
                        return end;
                    }

                    throw ThrowHelper.UnexpectedToken(_reader);

                default:
                    throw ThrowHelper.UnexpectedToken(_reader);
            }
        }
    }
}
