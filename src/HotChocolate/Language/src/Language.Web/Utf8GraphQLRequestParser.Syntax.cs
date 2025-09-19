using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

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

    private ObjectFieldNode ParseObjectFieldSyntax()
    {
        if (_reader.Kind != TokenKind.String)
        {
            throw new SyntaxException(_reader,
                ParseMany_InvalidOpenToken,
                TokenKind.String,
                TokenPrinter.Print(ref _reader));
        }

        var name = _reader.GetString();
        _reader.MoveNext();
        _reader.Expect(TokenKind.Colon);
        var value = ParseValueSyntax();

        return new ObjectFieldNode(name, value);
    }

    private ListValueNode ParseListSyntax()
    {
        if (_reader.Kind != TokenKind.LeftBracket)
        {
            throw new SyntaxException(_reader,
                ParseMany_InvalidOpenToken,
                TokenKind.LeftBracket,
                TokenPrinter.Print(ref _reader));
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

    private IValueNode ParseScalarSyntax()
    {
        switch (_reader.Kind)
        {
            case TokenKind.String:
            {
                _memory ??= new Utf8MemoryBuilder();
                var index = _memory.NextIndex;
                var length = _reader.GetRawString(_memory);
                var value = _memory.GetMemorySegment(index, length);
                _reader.MoveNext();
                return new StringValueNode(null, value, block: false);
            }

            case TokenKind.Integer:
            {
                _memory ??= new Utf8MemoryBuilder();
                var value = _memory.Write(_reader.Value);
                _reader.MoveNext();
                return new IntValueNode(null, value);
            }

            case TokenKind.Float:
            {
                _memory ??= new Utf8MemoryBuilder();
                var value = _memory.Write(_reader.Value);
                var format = _reader.FloatFormat;
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
