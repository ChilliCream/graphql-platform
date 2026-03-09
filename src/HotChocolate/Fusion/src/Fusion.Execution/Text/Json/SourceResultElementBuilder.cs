using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal readonly partial struct SourceResultElementBuilder
{
    private static readonly JavaScriptEncoder s_encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly SourceResultDocumentBuilder _builder;
    internal readonly int _index;

    internal SourceResultElementBuilder(SourceResultDocumentBuilder builder, int index)
    {
        _builder = builder;
        _index = index;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ElementTokenType TokenType
    {
        get
        {
            if (_builder is null)
            {
                return ElementTokenType.None;
            }

            var tokenType = _builder.GetElementTokenType(_index);
            return tokenType is ElementTokenType.Reference
                ? _builder.GetElementTokenType(_builder._metaDb.GetLocation(_index))
                : tokenType;
        }
    }

    public JsonValueKind ValueKind => TokenType.ToValueKind();

    public SourceResultElementBuilder CreateObjectValue(Selection parent, ulong includeFlags)
    {
        AssertValidInstance();

        Debug.Assert(_builder._metaDb.GetElementTokenType(_index)
            is ElementTokenType.None or ElementTokenType.Null
            or ElementTokenType.Reference);

        var selectionSet = parent.DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(parent);
        var objectIndex = _builder.CreateObjectValue(selectionSet.Selections, includeFlags);
        var element = new SourceResultElementBuilder(_builder, objectIndex);
        _builder.AssignReference(this, element);
        return element;
    }

    public SourceResultElementBuilder CreateListValue(int length)
    {
        AssertValidInstance();

        Debug.Assert(_builder._metaDb.GetElementTokenType(_index)
            is ElementTokenType.None or ElementTokenType.Null
            or ElementTokenType.Reference);

        var arrayIndex = _builder.CreateListValue(length);
        var element = new SourceResultElementBuilder(_builder, arrayIndex);
        _builder.AssignReference(this, element);
        return element;
    }

    public void SetStringValue(ReadOnlySpan<byte> value)
    {
        AssertValidInstance();

        Debug.Assert(_builder._metaDb.GetElementTokenType(_index)
            is ElementTokenType.None or ElementTokenType.Null
            or ElementTokenType.String);

        var writer = _builder._data;
        var writeIndex = _builder._data.Length;

        var jsonEncoded = JsonEncodedText.Encode(value, s_encoder);
        var requiredSize = jsonEncoded.EncodedUtf8Bytes.Length + 2;
        var target = writer.GetSpan(requiredSize);

        target[0] = (byte)'"';
        jsonEncoded.EncodedUtf8Bytes.CopyTo(target[1..]);
        target[requiredSize - 1] = (byte)'"';

        writer.Advance(requiredSize);

        _builder._metaDb.SetLocation(_index, writeIndex);
        _builder._metaDb.SetSizeOrLength(_index, requiredSize);
        _builder._metaDb.SetElementTokenType(_index, ElementTokenType.String);
    }

    public void SetStringValue(string value)
        => SetStringValue(s_utf8Encoding.GetBytes(value));

    public void SetBooleanValue(bool value)
    {
        AssertValidInstance();

        Debug.Assert(_builder._metaDb.GetElementTokenType(_index)
            is ElementTokenType.None or ElementTokenType.Null
            or ElementTokenType.True or ElementTokenType.False);
        _builder._metaDb.SetElementTokenType(_index, value ? ElementTokenType.True : ElementTokenType.False);
    }

    public void SetNullValue()
    {
        AssertValidInstance();

        Debug.Assert(_builder._metaDb.GetElementTokenType(_index)
            is ElementTokenType.None or ElementTokenType.Null
            or ElementTokenType.String or ElementTokenType.Number
            or ElementTokenType.True or ElementTokenType.False
            or ElementTokenType.Reference);
        _builder._metaDb.SetElementTokenType(_index, ElementTokenType.Null);
    }

    public SourceResultElementBuilder CreateProperty(Selection selection, int index)
    {
        var startIndex = _builder.GetStartIndex(_index);
        Debug.Assert(_builder._metaDb.GetElementTokenType(startIndex) is ElementTokenType.StartObject);

        var propertyIndex = startIndex + (index * 2) + 1;
        Debug.Assert(startIndex + _builder._metaDb.GetNumberOfRows(startIndex) - 1 > propertyIndex);
        Debug.Assert(_builder._metaDb.GetElementTokenType(propertyIndex) is ElementTokenType.PropertyName);

        _builder._metaDb.SetLocation(propertyIndex, selection.Id);
        return new SourceResultElementBuilder(_builder, propertyIndex + 1);
    }

    public IEnumerable<SourceResultElementBuilder> EnumerateArray()
        => new ArrayEnumerator(this);

    private void AssertValidInstance()
    {
        if (_builder is null)
        {
            throw new InvalidOperationException();
        }
    }
}
