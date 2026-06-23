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
        IReadOnlyList<PathArgumentNode> arguments = [];

        if (_reader.TokenKind == TokenKind.LeftParenthesis)
        {
            arguments = ParseArguments(fieldName, _reader.ReadBalancedParentheses());
        }

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

    private IReadOnlyList<PathArgumentNode> ParseArguments(
        NameNode fieldName,
        string argumentList)
    {
        var arguments = new List<PathArgumentNode>();
        var sourceText = argumentList.AsSpan(1, argumentList.Length - 2);
        var position = 0;

        while (position < sourceText.Length)
        {
            SkipWhitespaceAndCommas(sourceText, ref position);

            if (position >= sourceText.Length)
            {
                break;
            }

            var name = ParseArgumentName(sourceText, ref position, fieldName);
            SkipWhitespace(sourceText, ref position);

            if (position >= sourceText.Length || sourceText[position] != CharConstants.Colon)
            {
                throw new FieldSelectionMapSyntaxException(
                    _reader,
                    $"Expected a ':' after argument `{name.Value}` on field `{fieldName.Value}`.");
            }

            position++;
            SkipWhitespace(sourceText, ref position);

            var value = ParseArgumentValue(sourceText, ref position, fieldName);
            arguments.Add(new PathArgumentNode(name, value));
        }

        return arguments;
    }

    private static void SkipWhitespaceAndCommas(ReadOnlySpan<char> sourceText, ref int position)
    {
        while (position < sourceText.Length
            && (char.IsWhiteSpace(sourceText[position]) || sourceText[position] == CharConstants.Comma))
        {
            position++;
        }
    }

    private static void SkipWhitespace(ReadOnlySpan<char> sourceText, ref int position)
    {
        while (position < sourceText.Length && char.IsWhiteSpace(sourceText[position]))
        {
            position++;
        }
    }

    private NameNode ParseArgumentName(
        ReadOnlySpan<char> sourceText,
        ref int position,
        NameNode fieldName)
    {
        if (position >= sourceText.Length || !sourceText[position].IsLetterOrUnderscore())
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                $"Expected an argument name on field `{fieldName.Value}`.");
        }

        var start = position++;

        while (position < sourceText.Length
            && (sourceText[position].IsLetterOrUnderscore() || char.IsDigit(sourceText[position])))
        {
            position++;
        }

        return new NameNode(sourceText[start..position].ToString());
    }

    private string ParseArgumentValue(
        ReadOnlySpan<char> sourceText,
        ref int position,
        NameNode fieldName)
    {
        var start = position;
        var parenthesesDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        var inString = false;
        var escaped = false;

        while (position < sourceText.Length)
        {
            var code = sourceText[position];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (code == '\\')
                {
                    escaped = true;
                }
                else if (code == '"')
                {
                    inString = false;
                }

                position++;
                continue;
            }

            switch (code)
            {
                case '"':
                    inString = true;
                    break;

                case CharConstants.LeftParenthesis:
                    parenthesesDepth++;
                    break;

                case CharConstants.RightParenthesis:
                    parenthesesDepth--;
                    break;

                case CharConstants.LeftSquareBracket:
                    bracketDepth++;
                    break;

                case CharConstants.RightSquareBracket:
                    bracketDepth--;
                    break;

                case CharConstants.LeftBrace:
                    braceDepth++;
                    break;

                case CharConstants.RightBrace:
                    braceDepth--;
                    break;

                case CharConstants.Comma
                    when parenthesesDepth == 0 && bracketDepth == 0 && braceDepth == 0:
                    return sourceText[start..position].Trim().ToString();
            }

            position++;
        }

        var value = sourceText[start..position].Trim();

        if (value.IsEmpty)
        {
            throw new FieldSelectionMapSyntaxException(
                _reader,
                $"Expected a value for an argument on field `{fieldName.Value}`.");
        }

        return value.ToString();
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

        IValueSelectionNode? selectedValue = null;
        if (_reader.TokenKind == TokenKind.Colon)
        {
            MoveNext(); // skip ":"
            selectedValue = ParseValueSelectionOrChoice();
        }

        var location = CreateLocation(in start);

        return new ObjectFieldSelectionNode(location, name, selectedValue);
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
