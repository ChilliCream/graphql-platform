using System.Globalization;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private object? ParseValue()
    {
        return _reader.Kind switch
        {
            TokenKind.LeftBracket => ParseList(),
            TokenKind.LeftBrace => ParseObject(),
            TokenKind.String => ParseScalar(),
            TokenKind.Integer => ParseScalar(),
            TokenKind.Float => ParseScalar(),
            TokenKind.Name => ParseScalar(),
            _ => throw ThrowHelper.UnexpectedToken(_reader),
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
            _ => throw ThrowHelper.UnexpectedToken(_reader),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IReadOnlyDictionary<string, object?> ParseObject()
    {
        _reader.Expect(TokenKind.LeftBrace);

        var obj = new Dictionary<string, object?>();

        while (_reader.Kind != TokenKind.RightBrace)
        {
            ParseObjectField(obj);
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
    private void ParseObjectField(IDictionary<string, object?> obj)
    {
        if (_reader.Kind != TokenKind.String)
        {
            throw new SyntaxException(
                _reader,
                ParseMany_InvalidOpenToken,
                TokenKind.String,
                TokenPrinter.Print(ref _reader));
        }

        var name = _reader.GetString();
        _reader.MoveNext();
        _reader.Expect(TokenKind.Colon);
        var value = ParseValue();
        obj.Add(name, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipObjectField()
    {
        if (_reader.Kind != TokenKind.String)
        {
            throw new SyntaxException(
                _reader,
                ParseMany_InvalidOpenToken,
                TokenKind.String,
                TokenPrinter.Print(ref _reader));
        }

        _reader.MoveNext();
        _reader.Expect(TokenKind.Colon);
        SkipValue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IReadOnlyList<object?> ParseList()
    {
        if (_reader.Kind != TokenKind.LeftBracket)
        {
            throw new SyntaxException(
                _reader,
                ParseMany_InvalidOpenToken,
                TokenKind.LeftBracket,
                TokenPrinter.Print(ref _reader));
        }

        var list = new List<object?>();

        // skip opening token
        _reader.MoveNext();

        while (_reader.Kind != TokenKind.RightBracket)
        {
            list.Add(ParseValue());
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
    private object? ParseScalar()
    {
        string? value;

        switch (_reader.Kind)
        {
            case TokenKind.String:
                value = _reader.GetString();
                _reader.MoveNext();
                return value;

            case TokenKind.Integer:
                value = _reader.GetScalarValue();
                _reader.MoveNext();
                return long.Parse(value, CultureInfo.InvariantCulture);

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
