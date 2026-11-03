using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

public class ResultDocumentItemCollectionTests
{
    [Fact]
    public void GetEnumerator_Should_YieldItemsWithTheirOverallIndex_When_StartIndexIsSet()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 3, characterCount: 0);
        using var items = new ResultDocumentItemCollection(document);

        // act
        var result = items.Select(t => $"{t.Index}:{((ResultElement)t.Element).Path.Print()}");

        // assert
        result.MatchInlineSnapshots(["5:films[5]", "6:films[6]", "7:films[7]"]);
    }

    [Fact]
    public void GetEnumerator_Should_YieldNothing_When_ListIsNull()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 3, characterCount: 0);
        document.Data.GetProperty("films").SetNullValue();
        using var items = new ResultDocumentItemCollection(document);

        // act
        var result = items.ToList();

        // assert
        Assert.Empty(result);
    }

    [Fact]
    public void WriteDataTo_Should_WriteTheItemsAsJsonArray_When_BatchHasObjectItems()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 2, characterCount: 2);
        using var items = new ResultDocumentItemCollection(document);
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        // act
        items.WriteDataTo(writer);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchInlineSnapshot(
            """
            [{"title":"film","characters":["character","character"]},{"title":"film","characters":["character","character"]}]
            """);
    }

    [Fact]
    public void WriteDataTo_Should_BeByteIdenticalToTheListElement_When_BatchIsWritten()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 3, characterCount: 2);
        using var items = new ResultDocumentItemCollection(document);
        using var collectionBuffer = new PooledArrayWriter();
        using var elementBuffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            collectionBuffer,
            new JsonWriterOptions
            {
                Indented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        // act
        items.WriteDataTo(writer);
        document.Data.GetProperty("films").WriteTo(elementBuffer);

        // assert
        Assert.True(collectionBuffer.WrittenSpan.SequenceEqual(elementBuffer.WrittenSpan));
    }

    [Fact]
    public void Dispose_Should_ReleaseTheDocument_When_BatchWasNeverWritten()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 3, characterCount: 0);
        var items = new ResultDocumentItemCollection(document);

        // act
        items.Dispose();
        items.Dispose();

        // assert
        Assert.Throws<ObjectDisposedException>(() => document.Data.GetPropertyCount());
    }

    [Fact]
    public async Task Dispose_Should_ReleaseTheDocument_When_OwningOperationResultIsDisposed()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var document = StreamBatchDocumentFactory.Create(operation, startIndex: 5, itemCount: 3, characterCount: 0);
        var items = new ResultDocumentItemCollection(document);
        var result = new OperationResult(new OperationResultData(items, isValueNull: false, items, items));

        // act
        await result.DisposeAsync();

        // assert
        Assert.Throws<ObjectDisposedException>(() => document.Data.GetPropertyCount());
    }
}
