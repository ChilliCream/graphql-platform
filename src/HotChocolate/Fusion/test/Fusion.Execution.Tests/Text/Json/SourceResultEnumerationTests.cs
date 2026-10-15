using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultEnumerationTests
{
    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"first\":1,\"nested\":{\"x\":[2]},\"last\":3}", true)]
    [InlineData("{\"same\":null,\"same\":false,\"escaped\":\"a\\tb\"}", true)]
    [InlineData("[]", false)]
    [InlineData("[null]", false)]
    [InlineData("[1,{},[2,null],\"three\",false]", false)]
    public void EnumerateValues_Should_PreserveValues_When_EnumeratorIsCopied(string json, bool isObject)
    {
        // arrange
        using var arena = new MemoryArena();
        var bytes = Encoding.UTF8.GetBytes(json);
        using var document = SourceResultDocument.Parse(arena, bytes, bytes.Length);
        using var expected = JsonDocument.Parse(json);
        var expectedValues = isObject
            ? expected.RootElement.EnumerateObject().Select(t => t.Value.GetRawText()).ToArray()
            : expected.RootElement.EnumerateArray().Select(t => t.GetRawText()).ToArray();
        var snapshot = document.Root.CreateSnapshot();
        var enumerator = isObject ? snapshot.EnumerateObjectValues() : snapshot.EnumerateArrayValues();
        var values = new List<string>();
        var copyValues = new List<string>();

        // act
        if (enumerator.MoveNext())
        {
            var copy = enumerator;
            do
            {
                copyValues.Add(copy.Current.GetRawText());
            }
            while (copy.MoveNext());

            do
            {
                values.Add(enumerator.Current.GetRawText());
            }
            while (enumerator.MoveNext());
        }

        // assert
        Assert.Equal(expectedValues, values);
        Assert.Equal(expectedValues, copyValues);
    }

    [Fact]
    public void EnumerateSnapshot_Should_PreserveNestedValues_When_MetadataCrossesChunks()
    {
        // arrange
        using var arena = new MemoryArena();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            Enumerable.Range(0, 300).Select(i => new { id = i, child = new { values = new int?[] { i, null } } }));
        using var document = SourceResultDocument.Parse(arena, bytes, bytes.Length);
        using var expected = JsonDocument.Parse(bytes);
        var expectedValues = expected.RootElement.EnumerateArray()
            .SelectMany(t => t.EnumerateObject().Select(p => p.Value.GetRawText()))
            .ToArray();
        var elements = document.Root.CreateSnapshot().EnumerateArrayValues();
        var values = new List<string>();

        // act
        while (elements.MoveNext())
        {
            var properties = elements.Current.EnumerateObjectValues();
            while (properties.MoveNext())
            {
                values.Add(properties.Current.GetRawText());
            }
        }

        // assert
        Assert.Equal(expectedValues, values);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnumerateSnapshot_Should_PreserveException_When_ValueHasWrongKind(bool array)
    {
        // arrange
        using var arena = new MemoryArena();
        var bytes = "1"u8.ToArray();
        using var document = SourceResultDocument.Parse(arena, bytes, bytes.Length);

        // act
        var expected = Assert.Throws<InvalidOperationException>(() =>
        {
            if (array)
            {
                document.Root.EnumerateArray();
            }
            else
            {
                document.Root.EnumerateObject();
            }
        });
        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            if (array)
            {
                document.Root.CreateSnapshot().EnumerateArrayValues();
            }
            else
            {
                document.Root.CreateSnapshot().EnumerateObjectValues();
            }
        });

        // assert
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(expected.Source, actual.Source);
    }

    [Fact]
    public void EnumerateSnapshot_Should_RejectUndefinedValue_When_SnapshotIsDefault()
    {
        // arrange
        static void EnumerateObject() => default(SourceResultElementSnapshot).EnumerateObjectValues();
        static void EnumerateArray() => default(SourceResultElementSnapshot).EnumerateArrayValues();

        // act
        var objectError = Record.Exception(EnumerateObject);
        var arrayError = Record.Exception(EnumerateArray);

        // assert
        Assert.IsType<InvalidOperationException>(objectError);
        Assert.IsType<InvalidOperationException>(arrayError);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public void EnumerateSnapshot_Should_RejectDisposedDocument_When_MetadataWasReadBeforeDisposal(string json)
    {
        // arrange
        using var arena = new MemoryArena();
        var bytes = Encoding.UTF8.GetBytes(json);
        using var document = SourceResultDocument.Parse(arena, bytes, bytes.Length);
        var snapshot = document.Root.CreateSnapshot();
        document.Dispose();
        Exception? error = null;

        // act
        try
        {
            if (snapshot.ValueKind is JsonValueKind.Array)
            {
                snapshot.EnumerateArrayValues();
            }
            else
            {
                snapshot.EnumerateObjectValues();
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }

        // assert
        Assert.IsType<ObjectDisposedException>(error);
    }
}
