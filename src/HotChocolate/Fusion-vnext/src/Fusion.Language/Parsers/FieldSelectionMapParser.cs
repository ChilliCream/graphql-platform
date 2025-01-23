using System.Runtime.CompilerServices;
using static HotChocolate.Fusion.Properties.FusionLanguageResources;
using TokenKind = HotChocolate.Fusion.FieldSelectionMapTokenKind;

namespace HotChocolate.Fusion;

/// <summary>
/// Parses nodes from source text representing a field selection map.
/// </summary>
internal ref struct FieldSelectionMapParser
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

    public SelectedValueNode Parse()
    {
        _parsedNodes = 0;

        Expect(TokenKind.StartOfFile);

        var selectedValue = ParseSelectedValue();

        Expect(TokenKind.EndOfFile);

        return selectedValue;
    }

    /// <summary>
    /// Parses a <see cref="SelectedValueNode"/>.
    ///
    /// <code>
    /// SelectedValue ::
    ///     SelectedValue | SelectedValueEntry
    ///     |opt SelectedValueEntry
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="SelectedValueNode"/>.</returns>
    private SelectedValueNode ParseSelectedValue()
    {
        var start = Start();

        var selectedValueEntry = ParseSelectedValueEntry();
        SelectedValueNode? selectedValue = null;

        if (_reader.TokenKind == TokenKind.Pipe)
        {
            MoveNext(); // skip "|"
            selectedValue = ParseSelectedValue();
        }

        var location = CreateLocation(in start);

        return new SelectedValueNode(location, selectedValueEntry, selectedValue);
    }

    /// <summary>
    /// Parses a <see cref="SelectedValueEntryNode"/>.
    ///
    /// <code>
    /// SelectedValueEntry ::
    ///     Path [lookahead != .]
    ///     Path . SelectedObjectValue
    ///     Path SelectedListValue
    ///     SelectedObjectValue
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="SelectedValueEntryNode"/>.</returns>
    private SelectedValueEntryNode ParseSelectedValueEntry()
    {
        var start = Start();

        PathNode? path = null;
        SelectedObjectValueNode? selectedObjectValue = null;
        SelectedListValueNode? selectedListValue = null;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (_reader.TokenKind)
        {
            case TokenKind.Name: // For a PathSegment.
            case TokenKind.LeftAngleBracket: // For a <TypeName>.
                path = ParsePath();

                if (_reader.TokenKind == TokenKind.LeftSquareBracket)
                {
                    selectedListValue = ParseSelectedListValue();
                }

                break;

            case TokenKind.LeftBrace:
                selectedObjectValue = ParseSelectedObjectValue();
                break;

            default:
                throw new FieldSelectionMapSyntaxException(
                    _reader,
                    UnexpectedToken,
                    _reader.TokenKind);
        }

        var location = CreateLocation(in start);

        return new SelectedValueEntryNode(location, path, selectedObjectValue, selectedListValue);
    }

    /// <summary>
    /// Parses a <see cref="PathNode"/>.
    ///
    /// <code>
    /// Path ::
    ///     &lt; TypeName &gt; . PathSegment
    ///     PathSegment
    ///
    /// PathSegment ::
    ///     FieldName
    ///     FieldName . PathSegment
    ///     FieldName &lt; TypeName &gt; . PathSegment
    ///
    /// FieldName ::
    ///     Name
    ///
    /// TypeName ::
    ///     Name
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="PathNode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Parses a <see cref="PathSegmentNode"/>.
    ///
    /// <code>
    /// PathSegment ::
    ///     FieldName
    ///     FieldName . PathSegment
    ///     FieldName &lt; TypeName &gt; . PathSegment
    ///
    /// FieldName ::
    ///     Name
    ///
    /// TypeName ::
    ///     Name
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="PathSegmentNode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        else if (_reader.TokenKind == TokenKind.Period)
        {
            MoveNext(); // skip "."
            pathSegment = ParsePathSegment();
        }

        var location = CreateLocation(in start);

        return new PathSegmentNode(location, fieldName, typeName, pathSegment);
    }

    /// <summary>
    /// Parses a <see cref="SelectedObjectValueNode"/>.
    ///
    /// <code>
    /// SelectedObjectValue ::
    ///     { SelectedObjectField+ }
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="SelectedObjectValueNode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SelectedObjectValueNode ParseSelectedObjectValue()
    {
        var start = Start();

        Expect(TokenKind.LeftBrace);

        var fields = new List<SelectedObjectFieldNode>();

        while (_reader.TokenKind != TokenKind.RightBrace)
        {
            fields.Add(ParseSelectedObjectField());
        }

        Expect(TokenKind.RightBrace);

        var location = CreateLocation(in start);

        return new SelectedObjectValueNode(location, fields);
    }

    /// <summary>
    /// Parses a <see cref="SelectedObjectFieldNode"/>.
    ///
    /// <code>
    /// SelectedObjectField ::
    ///     Name : SelectedValue
    ///     Name
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="SelectedObjectFieldNode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SelectedObjectFieldNode ParseSelectedObjectField()
    {
        var start = Start();

        var name = ParseName();

        SelectedValueNode? selectedValue = null;
        if (_reader.TokenKind == TokenKind.Colon)
        {
            MoveNext(); // skip ":"
            selectedValue = ParseSelectedValue();
        }

        var location = CreateLocation(in start);

        return new SelectedObjectFieldNode(location, name, selectedValue);
    }

    /// <summary>
    /// Parses a <see cref="SelectedListValueNode"/>.
    ///
    /// <code>
    /// SelectedListValue ::
    ///     [ SelectedValue ]
    ///     [ SelectedListValue ]
    /// </code>
    /// </summary>
    /// <returns>The parsed <see cref="SelectedListValueNode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SelectedListValueNode ParseSelectedListValue()
    {
        var start = Start();

        Expect(TokenKind.LeftSquareBracket);

        SelectedListValueNode? selectedListValue = null;
        SelectedValueNode? selectedValue = null;

        if (_reader.TokenKind == TokenKind.LeftSquareBracket)
        {
            selectedListValue = ParseSelectedListValue();
        }
        else
        {
            selectedValue = ParseSelectedValue();
        }

        Expect(TokenKind.RightSquareBracket);

        var location = CreateLocation(in start);

        if (selectedListValue is not null)
        {
            return new SelectedListValueNode(location, selectedListValue);
        }

        if (selectedValue is not null)
        {
            return new SelectedListValueNode(location, selectedValue);
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Parses a <see cref="NameNode"/>.
    /// </summary>
    /// <returns>The parsed <see cref="NameNode"/>.</returns>
    /// <seealso href="https://spec.graphql.org/October2021/#sec-Names">Specification</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
