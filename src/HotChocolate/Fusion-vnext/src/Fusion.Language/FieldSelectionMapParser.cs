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

        return new PathSegmentNode(location, fieldName, typeName, pathSegment);
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
