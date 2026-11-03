using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

/// <summary>
/// Exposes the items of the list root of a <see cref="ResultDocument"/> as the items of a streamed batch.
/// </summary>
internal sealed class ResultDocumentItemCollection : OperationResultItemCollection
{
    private readonly ResultDocument _document;
    private readonly ResultElement _list;

    public ResultDocumentItemCollection(ResultDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        foreach (var property in document.Data.EnumerateObject())
        {
            _list = property.Value;
        }
    }

    public override void WriteDataTo(JsonWriter jsonWriter)
    {
        ArgumentNullException.ThrowIfNull(jsonWriter);

        var cursor = _list.Cursor;
        var formatter = new ResultDocument.RawJsonFormatter(_document, jsonWriter);
        formatter.WriteValue(cursor, _document._metaDb.Get(cursor));
    }

    public override IEnumerator<OperationResultItem> GetEnumerator()
    {
        if (_list.ValueKind is not JsonValueKind.Array)
        {
            return Enumerable.Empty<OperationResultItem>().GetEnumerator();
        }

        return EnumerateItems();
    }

    protected override void Release() => _document.Dispose();

    private IEnumerator<OperationResultItem> EnumerateItems()
    {
        var index = _document.StartIndex;

        foreach (var item in _list.EnumerateArray())
        {
            yield return new OperationResultItem(index++, item);
        }
    }
}
