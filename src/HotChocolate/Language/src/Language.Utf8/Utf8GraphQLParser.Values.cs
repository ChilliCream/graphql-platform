using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;
using static HotChocolate.Language.TokenPrinter;

namespace HotChocolate.Language;

// Implements the parsing rules in the Values section.
public ref partial struct Utf8GraphQLParser
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

        if (TokenHelper.IsScalarValue(ref _reader))
        {
            return ParseScalarValue();
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
    private StringValueNode ParseStringLiteral()
    {
        var start = Start();

        var isBlock = _reader.Kind == TokenKind.BlockString;
        var value = ExpectString();
        var location = CreateLocation(in start);

        return new StringValueNode(location, value, isBlock);
    }

    /// <summary>
    /// Parses a list value.
    /// <see cref="ListValueNode" />:
    /// - [ ]
    /// - [ Value[isConstant]+ ]
    /// </summary>
    /// <param name="isConstant">
    /// Defines if only constant values are allowed;
    /// otherwise, variables are allowed.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ListValueNode ParseList(bool isConstant)
    {
        var start = Start();

        if (_reader.Kind != TokenKind.LeftBracket)
        {
            throw new SyntaxException(
                _reader,
                ParseMany_InvalidOpenToken,
                TokenKind.LeftBracket,
                Print(ref _reader));
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

        var location = CreateLocation(in start);

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
    /// <param name="isConstant">
    /// Defines if only constant values are allowed;
    /// otherwise, variables are allowed.
    /// </param>
    private ObjectValueNode ParseObject(bool isConstant)
    {
        var start = Start();

        if (_reader.Kind != TokenKind.LeftBrace)
        {
            throw new SyntaxException(
                _reader,
                ParseMany_InvalidOpenToken,
                TokenKind.LeftBrace,
                Print(ref _reader));
        }

        var fields = new List<ObjectFieldNode>();

        // skip opening token
        MoveNext();

        while (_reader.Kind != TokenKind.RightBrace)
        {
            var fieldStart = Start();
            var name = ParseName();
            ExpectColon();
            var value = ParseValueLiteral(isConstant);
            var fieldLocation = CreateLocation(in fieldStart);

            fields.Add(new ObjectFieldNode(fieldLocation, name, value));
        }

        // skip closing token
        Expect(TokenKind.RightBrace);

        var location = CreateLocation(in start);

        return new ObjectValueNode
        (
            location,
            fields
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IValueNode ParseScalarValue()
    {
        if (TokenHelper.IsString(ref _reader))
        {
            return ParseStringLiteral();
        }

        var start = Start();
        var kind = _reader.Kind;

        if (!TokenHelper.IsScalarValue(ref _reader))
        {
            throw new SyntaxException(_reader, Parser_InvalidScalarToken, _reader.Kind);
        }

        ReadOnlyMemory<byte> value = _reader.Value.ToArray();
        var format = _reader.FloatFormat;
        MoveNext();

        var location = CreateLocation(in start);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IValueNode ParseEnumValue()
    {
        var start = Start();

        Location? location;

        if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
        {
            MoveNext();
            location = CreateLocation(in start);
            return new BooleanValueNode(location, true);
        }

        if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
        {
            MoveNext();
            location = CreateLocation(in start);
            return new BooleanValueNode(location, false);
        }

        if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
        {
            MoveNext();
            if (_createLocation)
            {
                location = CreateLocation(in start);
                return new NullValueNode(location);
            }
            return NullValueNode.Default;
        }

        var value = _reader.GetString();
        MoveNext();
        location = CreateLocation(in start);

        return new EnumValueNode
        (
            location,
            value
        );
    }
}
