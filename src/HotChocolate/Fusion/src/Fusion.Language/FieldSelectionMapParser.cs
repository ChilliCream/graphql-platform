using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using TokenKind = HotChocolate.Fusion.Language.FieldSelectionMapTokenKind;
using static HotChocolate.Fusion.Language.Properties.FusionLanguageResources;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Parses nodes from source text representing a field selection map.
/// </summary>
public ref struct FieldSelectionMapParser
{
    private readonly FieldSelectionMapParserOptions _options;

    private FieldSelectionMapReader _reader;
    private int _parsedNodes;

    public FieldSelectionMapParser(
        ReadOnlySpan<char> sourceText,
        FieldSelectionMapParserOptions? options = null)
    {
        if (sourceText.Length == 0)
        {
            throw new ArgumentException(SourceTextCannotBeEmpty, nameof(sourceText));
        }

        options ??= FieldSelectionMapParserOptions.Default;

        _options = options;
        _reader = new FieldSelectionMapReader(sourceText, options.MaxAllowedTokens);
    }

    public static IValueSelectionNode Parse(
        string sourceText,
        FieldSelectionMapParserOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceText);
        var parser = new FieldSelectionMapParser(sourceText.AsSpan(), options);
        return parser.Parse();
    }

    public IValueSelectionNode Parse()
    {
        _parsedNodes = 0;

        Expect(TokenKind.StartOfFile);

        var selectedValue = ParseValueSelectionOrChoice();

        Expect(TokenKind.EndOfFile);

        return selectedValue;
    }

    private IValueSelectionNode ParseValueSelectionOrChoice()
    {
        var start = Start();

        var first = ParseValueSelection();
        ImmutableArray<IValueSelectionNode>.Builder? branches = null;

        while (_reader.TokenKind == TokenKind.Pipe)
        {
            if (branches is null)
            {
                branches = ImmutableArray.CreateBuilder<IValueSelectionNode>();
                branches.Add(first);
            }

            MoveNext(); // skip "|"
            branches.Add(ParseValueSelection());
        }

        var location = CreateLocation(in start);

        return branches is null
            ? first
            : new ChoiceValueSelectionNode(location, branches.ToImmutable());
    }

    private IValueSelectionNode ParseValueSelection()
    {
        var start = Start();

        PathNode? path = null;
        ObjectValueSelectionNode? objectValueSelection = null;
        ListValueSelectionNode? listValueSelection = null;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (_reader.TokenKind)
        {
            case TokenKind.Name: // For a PathSegment.
            case TokenKind.LeftAngleBracket: // For a <TypeName>.
                path = ParsePath();

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (_reader.TokenKind)
                {
                    case TokenKind.Period:
                        MoveNext(); // skip "."
                        objectValueSelection = ParseObjectValueSelection();
                        break;

                    case TokenKind.LeftSquareBracket:
                        listValueSelection = ParseListValueSelection();
                        break;
                }

                break;

            case TokenKind.LeftBrace:
                objectValueSelection = ParseObjectValueSelection();
                break;

            default:
                throw new FieldSelectionMapSyntaxException(
                    _reader,
                    UnexpectedToken,
                    _reader.TokenKind);
        }

        if (path is not null)
        {
            if (objectValueSelection is not null)
            {
                return new PathObjectValueSelectionNode(CreateLocation(in start), path, objectValueSelection);
            }

            if (listValueSelection is not null)
            {
                return new PathListValueSelectionNode(CreateLocation(in start), path, listValueSelection);
            }

            return path;
        }

        if (objectValueSelection is not null)
        {
            return objectValueSelection;
        }

        throw new FieldSelectionMapSyntaxException(_reader, "Unexpected value selection.");
    }

    private PathNode ParsePath()
    {
        var start = Start();

        NameNode? typeName = null;

        if (_reader.TokenKind == TokenKind.LeftAngleBracket)
        {
            MoveNext(); // skip "<"
            typeName = ParseName();
            Expect(TokenKind.RightAngleBracket);
            Expect(TokenKind.Period);
        }

        var pathSegment = ParsePathSegment();
        var location = CreateLocation(in start);

        return new PathNode(location, pathSegment, typeName);
    }

    private PathSegmentNode ParsePathSegment()
    {
        var start = Start();

        var fieldName = ParseName();

        var arguments = ParseArguments();

        NameNode? typeName = null;

        if (_reader.TokenKind == TokenKind.LeftAngleBracket)
        {
            MoveNext(); // skip "<"
            typeName = ParseName();
            Expect(TokenKind.RightAngleBracket);
        }

        PathSegmentNode? pathSegment = null;

        if (typeName is not null)
        {
            Expect(TokenKind.Period);
            pathSegment = ParsePathSegment();
        }
        else if (_reader.TokenKind == TokenKind.Period && _reader.GetNextTokenKind() != TokenKind.LeftBrace)
        {
            MoveNext(); // skip "."
            pathSegment = ParsePathSegment();
        }

        var location = CreateLocation(in start);

        return new PathSegmentNode(location, fieldName, arguments, typeName, pathSegment);
    }

    private ObjectValueSelectionNode ParseObjectValueSelection()
    {
        var start = Start();

        Expect(TokenKind.LeftBrace);

        var fields = ImmutableArray.CreateBuilder<ObjectFieldSelectionNode>();

        while (_reader.TokenKind != TokenKind.RightBrace)
        {
            fields.Add(ParseObjectFieldSelection());
        }

        Expect(TokenKind.RightBrace);

        var location = CreateLocation(in start);

        return new ObjectValueSelectionNode(location, fields.ToImmutable());
    }

    private ObjectFieldSelectionNode ParseObjectFieldSelection()
    {
        var start = Start();

        var name = ParseName();

        var arguments = ParseArguments();

        // An object field is either "Name : SelectedValue" or the shorthand "Name Arguments?".
        // Arguments are only valid on the shorthand form, so a value selection following a
        // non-empty argument list is a syntax error.
        if (arguments.Length > 0 && _reader.TokenKind == TokenKind.Colon)
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                ArgumentsNotAllowedOnObjectFieldWithValue);
        }

        IValueSelectionNode? selectedValue = null;
        if (_reader.TokenKind == TokenKind.Colon)
        {
            MoveNext(); // skip ":"
            selectedValue = ParseValueSelectionOrChoice();
        }

        var location = CreateLocation(in start);

        return new ObjectFieldSelectionNode(location, name, arguments, selectedValue);
    }

    private ImmutableArray<ArgumentNode> ParseArguments()
    {
        if (_reader.TokenKind != TokenKind.LeftParenthesis)
        {
            return [];
        }

        Expect(TokenKind.LeftParenthesis);

        var arguments = ImmutableArray.CreateBuilder<ArgumentNode>();

        while (_reader.TokenKind != TokenKind.RightParenthesis)
        {
            arguments.Add(ParseArgument());
        }

        if (arguments.Count == 0)
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                UnexpectedToken,
                _reader.TokenKind);
        }

        Expect(TokenKind.RightParenthesis);

        return arguments.ToImmutable();
    }

    private ArgumentNode ParseArgument()
    {
        var start = Start();

        var name = ParseName();
        Expect(TokenKind.Colon);
        var value = ParseValueLiteral();

        var location = CreateLocation(in start);

        return new ArgumentNode(location, name, value);
    }

    private IValueNode ParseValueLiteral()
    {
        switch (_reader.TokenKind)
        {
            case TokenKind.LeftSquareBracket:
                return ParseListValue();

            case TokenKind.LeftBrace:
                return ParseObjectValue();

            case TokenKind.IntValue:
                return ParseIntValue();

            case TokenKind.FloatValue:
                return ParseFloatValue();

            case TokenKind.StringValue:
            case TokenKind.BlockStringValue:
                return ParseStringValue();

            case TokenKind.Name:
                return ParseEnumOrKeywordValue();

            default:
                throw new FieldSelectionMapSyntaxException(
                    _reader,
                    UnexpectedToken,
                    _reader.TokenKind);
        }
    }

    private ListValueNode ParseListValue()
    {
        var start = Start();

        Expect(TokenKind.LeftSquareBracket);

        var items = ImmutableArray.CreateBuilder<IValueNode>();

        while (_reader.TokenKind != TokenKind.RightSquareBracket)
        {
            items.Add(ParseValueLiteral());
        }

        Expect(TokenKind.RightSquareBracket);

        var location = CreateLocation(in start);

        return new ListValueNode(location, items.ToImmutable());
    }

    private ObjectValueNode ParseObjectValue()
    {
        var start = Start();

        Expect(TokenKind.LeftBrace);

        var fields = ImmutableArray.CreateBuilder<ObjectFieldNode>();

        while (_reader.TokenKind != TokenKind.RightBrace)
        {
            fields.Add(ParseObjectField());
        }

        Expect(TokenKind.RightBrace);

        var location = CreateLocation(in start);

        return new ObjectValueNode(location, fields.ToImmutable());
    }

    private ObjectFieldNode ParseObjectField()
    {
        var start = Start();

        var name = ParseName();
        Expect(TokenKind.Colon);
        var value = ParseValueLiteral();

        var location = CreateLocation(in start);

        return new ObjectFieldNode(location, name, value);
    }

    private IntValueNode ParseIntValue()
    {
        var start = Start();
        var value = _reader.Value.ToString();
        Expect(TokenKind.IntValue);
        var location = CreateLocation(in start);

        return new IntValueNode(location, value);
    }

    private FloatValueNode ParseFloatValue()
    {
        var start = Start();
        var value = _reader.Value.ToString();
        Expect(TokenKind.FloatValue);
        var location = CreateLocation(in start);

        return new FloatValueNode(location, value);
    }

    private StringValueNode ParseStringValue()
    {
        var start = Start();
        var isBlock = _reader.TokenKind == TokenKind.BlockStringValue;

        // The reader exposes the raw lexeme between the quotes. Decode it into the semantic
        // value before advancing, since the span is only valid until the next read.
        var value = isBlock
            ? StringValueHelper.TrimBlockString(_reader.Value, _reader)
            : StringValueHelper.UnescapeString(_reader.Value, _reader);

        if (!_reader.Skip(TokenKind.StringValue) && !_reader.Skip(TokenKind.BlockStringValue))
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                InvalidToken,
                TokenKind.StringValue,
                _reader.TokenKind);
        }

        var location = CreateLocation(in start);

        return new StringValueNode(location, value, isBlock);
    }

    private IValueNode ParseEnumOrKeywordValue()
    {
        var start = Start();
        var value = _reader.Value.ToString();
        Expect(TokenKind.Name);
        var location = CreateLocation(in start);

        return value switch
        {
            "true" => new BooleanValueNode(location, true),
            "false" => new BooleanValueNode(location, false),
            "null" => new NullValueNode(location),
            _ => new EnumValueNode(location, value)
        };
    }

    private ListValueSelectionNode ParseListValueSelection()
    {
        var start = Start();

        Expect(TokenKind.LeftSquareBracket);

        ListValueSelectionNode? selectedListValue = null;
        IValueSelectionNode? selectedValue = null;

        if (_reader.TokenKind == TokenKind.LeftSquareBracket)
        {
            selectedListValue = ParseListValueSelection();
        }
        else
        {
            selectedValue = ParseValueSelectionOrChoice();
        }

        Expect(TokenKind.RightSquareBracket);

        var location = CreateLocation(in start);

        if (selectedListValue is not null)
        {
            return new ListValueSelectionNode(location, selectedListValue);
        }

        if (selectedValue is not null)
        {
            return new ListValueSelectionNode(location, selectedValue);
        }

        throw new InvalidOperationException();
    }

    private NameNode ParseName()
    {
        var start = Start();
        var name = ExpectName();
        var location = CreateLocation(in start);

        return new NameNode(location, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TokenInfo Start()
    {
        if (++_parsedNodes > _options.MaxAllowedNodes)
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                string.Format(MaxAllowedNodesExceeded, _options.MaxAllowedNodes));
        }

        return _options.NoLocations
            ? default
            : new TokenInfo(_reader.Start, _reader.End, _reader.Line, _reader.Column);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MoveNext() => _reader.Read();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Expect(TokenKind tokenKind)
    {
        if (!_reader.Skip(tokenKind))
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                InvalidToken,
                tokenKind,
                _reader.TokenKind);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ExpectName()
    {
        if (_reader.TokenKind == TokenKind.Name)
        {
            var name = _reader.Value.ToString();
            MoveNext();

            return name;
        }

        throw new FieldSelectionMapSyntaxException(
            _reader,
            InvalidToken,
            TokenKind.Name,
            _reader.TokenKind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Location? CreateLocation(in TokenInfo start)
    {
        return _options.NoLocations
            ? null
            : new Location(start.Start, _reader.End, start.Line, start.Column);
    }
}
