using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal readonly partial struct SourceResultElementBuilder
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly SourceResultDocumentBuilder _builder;
    internal readonly int _index;

    internal SourceResultElementBuilder(SourceResultDocumentBuilder builder, int index)
    {
        _builder = builder;
        _index = index;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ElementTokenType TokenType => _builder?.GetElementTokenType(_index) ?? ElementTokenType.None;

    public JsonValueKind ValueKind => TokenType.ToValueKind();

    public SourceResultElementBuilder CreateObjectValue(Selection parent, ulong includeFlags)
    {
        AssertValidInstance();

        var selectionSet = parent.DeclaringSelectionSet.DeclaringOperation.GetSelectionSet(parent);
        var objectIndex = _builder.CreateObjectValue(selectionSet.Selections, includeFlags);
        var element = new SourceResultElementBuilder(_builder, objectIndex);
        _builder.AssignReference(this, element);
        return element;
    }

    public SourceResultElementBuilder CreateListValue(int length)
    {
        AssertValidInstance();

        var arrayIndex = _builder.CreateListValue(length);
        var element = new SourceResultElementBuilder(_builder, arrayIndex);
        _builder.AssignReference(this, element);
        return element;
    }

    public void SetStringValue(ReadOnlySpan<byte> value)
    {
        AssertValidInstance();

        var writer = _builder._data;
        var writeIndex = _builder._data.Length;

        var requiredSize = value.Length + 2;
        var target = writer.GetSpan(requiredSize);
        target[0] = (byte)'"';
        value.CopyTo(target[1..]);
        target[^1..][0] = (byte)'"';
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

        _builder._metaDb.SetElementTokenType(_index, value ? ElementTokenType.True : ElementTokenType.False);
    }

    public void SetNullValue()
    {
        AssertValidInstance();

        _builder._metaDb.SetElementTokenType(_index, ElementTokenType.Null);
    }

    public SourceResultElementBuilder CreateProperty(Selection selection, int index)
    {
        var propertyIndex = _index + (index * 2) + 1;
        _builder._metaDb.SetLocation(propertyIndex, selection.Id);
        Debug.Assert(_builder._metaDb.GetElementTokenType(propertyIndex) is ElementTokenType.PropertyName);
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
