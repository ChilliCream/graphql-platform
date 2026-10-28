using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Text.Json;

public class ResultDocumentValueTests
{
    [Fact]
    public void SetStringValue_Should_RoundTripBytes_When_WritingSingleMultiMegabyteValue()
    {
        // arrange
        var expected = new byte[5 * 1024 * 1024];
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        for (var i = 0; i < expected.Length; i++)
        {
            expected[i] = (byte)('a' + (i % 26));
        }

        // act
        resultValue.SetStringValue(expected);
        var actual = Encoding.UTF8.GetBytes(resultValue.GetString()!);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetStringValue_Should_KeepPreviousDataChunkTable_When_DataChunkTableGrows()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var previous = GetData(resultDocument);

        // act
        resultValue.SetStringValue(new byte[5 * 1024 * 1024]);

        // assert
        // the shared pool serves a rent from the array this thread returned last
        var rented = ArrayPool<MemorySegment>.Shared.Rent(previous.Length);
        ArrayPool<MemorySegment>.Shared.Return(rented);
        Assert.NotSame(previous, GetData(resultDocument));
        Assert.NotSame(previous, rented);
    }

    [Fact]
    public void SetArrayValue_Should_KeepPreviousRowChunkTables_When_RowChunkTableGrowsTwice()
    {
        // arrange
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        var previous = GetChunks(ref resultDocument._metaDb);
        var firstChunk = previous[0].Buffer;

        // act
        resultValue.SetArrayValue(previous.Length * 2 * (128 * 1024 / ResultDocument.DbRow.Size));

        // assert
        Assert.True(GetChunks(ref resultDocument._metaDb).Length > previous.Length * 2);
        Assert.Same(firstChunk, previous[0].Buffer);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_data")]
    private static extern ref MemorySegment[] GetData(ResultDocument resultDocument);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_chunks")]
    private static extern ref MemorySegment[] GetChunks(ref ResultDocument.MetaDb metaDb);
}
