using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Transport.Http;

public class JsonResultEnumerableTests
{
    [Theory]
    [InlineData("[]", new string[0])]
    [InlineData(" \r\n[ ]", new string[0])]
    [InlineData("[{\"data\":{\"a\":1}}]", new[] { "{\"data\":{\"a\":1}}" })]
    [InlineData(
        " [ {\"data\":null} , 2,\"s\" ,[true,null] ]",
        new[] { "{\"data\":null}", "2", "\"s\"", "[true,null]" })]
    [InlineData("[{\"a\":1}] trailing", new[] { "{\"a\":1}" })]
    [InlineData("[{\"a\":1}]]", new[] { "{\"a\":1}" })]
    [InlineData("[1] [2", new[] { "1" })]
    public async Task GetAsyncEnumerator_Should_YieldEachElement_When_BodyIsArray(
        string body,
        string[] expected)
    {
        // act
        var elements = await ReadElementsAsync(body, arena: null);

        // assert
        Assert.Equal(expected, elements);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[{}")]
    [InlineData("[{},")]
    [InlineData("[{},]")]
    [InlineData("[,{}]")]
    [InlineData("[{} {}]")]
    [InlineData("[{\"a\":1]")]
    [InlineData("[1")]
    public async Task GetAsyncEnumerator_Should_Throw_When_ArrayIsInvalid(string body)
    {
        // act
        var exception = await Record.ExceptionAsync(() => ReadElementsAsync(body, arena: null));

        // assert
        Assert.IsAssignableFrom<JsonException>(exception);
    }

    [Fact]
    public async Task GetAsyncEnumerator_Should_Throw_When_ElementExceedsMaxDepth()
    {
        // arrange
        var accepted = "[{}," + new string('[', 63) + new string(']', 63) + "]";
        var rejected = "[{}," + new string('[', 64) + new string(']', 64) + "]";

        // act
        var elements = await ReadElementsAsync(accepted, arena: null);
        var exception = await Record.ExceptionAsync(() => ReadElementsAsync(rejected, arena: null));

        // assert
        Assert.Equal(2, elements.Count);
        Assert.IsAssignableFrom<JsonException>(exception);
    }

    [Theory]
    [InlineData(ChunkLayout.Adjacent)]
    [InlineData(ChunkLayout.Separate)]
    [InlineData(ChunkLayout.Paired)]
    public async Task GetAsyncEnumerator_Should_YieldEachElement_When_ElementsCrossChunks(ChunkLayout layout)
    {
        // arrange
        var expected = Enumerable.Range(0, 12)
            .Select(i => "{\"data\":{\"value\":\"" + new string((char)('a' + i), 700 * i) + "\\n\",\"n\":" + i + "}}")
            .ToArray();

        // act
        var elements = await ReadElementsAsync(
            "[" + string.Join(" , ", expected) + "]",
            new LayoutMemoryArena(layout));

        // assert
        Assert.Equal(expected, elements);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(3072)]
    public async Task GetAsyncEnumerator_Should_YieldEachElement_When_BodyFillsLastChunk(int length)
    {
        // arrange
        const string first = "{\"a\":1}";
        var second = "\"" + new string('x', length - first.Length - 5) + "\"";
        var body = "[" + first + "," + second + "]";

        // act
        var elements = await ReadElementsAsync(body, arena: null);

        // assert
        Assert.Equal(length, Encoding.UTF8.GetByteCount(body));
        Assert.Equal([first, second], elements);
    }

    /// <summary>
    /// Reads the elements of <paramref name="body"/> with chunks from <paramref name="arena"/>, or
    /// from a new <see cref="MemoryArena"/> when it is <c>null</c>.
    /// </summary>
    private static async Task<List<string>> ReadElementsAsync(string body, IMemoryArena? arena)
    {
        using var memoryArena = new MemoryArena();
        using var message = new HttpResponseMessage();
        message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        var arenaSource = new FixedMemoryArenaSource(arena ?? memoryArena);
        var enumerable = new JsonResultEnumerable(message, arenaSource, "utf-8");
        var elements = new List<string>();

        try
        {
            await foreach (var document in enumerable)
            {
                using (document)
                {
                    elements.Add(document.Root.GetRawText());
                }
            }
        }
        finally
        {
            memoryArena.Seal();
        }

        return elements;
    }

    public enum ChunkLayout
    {
        Adjacent,
        Separate,
        Paired
    }

    /// <summary>
    /// An arena that places the segments it rents next to each other, in separate buffers, or in pairs
    /// of adjacent segments.
    /// </summary>
    private sealed class LayoutMemoryArena(ChunkLayout layout) : IMemoryArena
    {
        private readonly byte[] _backing = new byte[1024 * 1024];
        private byte[] _pair = [];
        private int _offset;
        private int _rents;

        public MemorySegment Rent(int size)
        {
            MemorySegment segment;

            switch (layout)
            {
                case ChunkLayout.Adjacent:
                    segment = new MemorySegment(_backing, _offset, size);
                    _offset += size;
                    break;

                case ChunkLayout.Separate:
                    segment = new MemorySegment(new byte[size], 0, size);
                    break;

                default:
                    if (_rents % 2 == 0 || _offset + size > _pair.Length)
                    {
                        _pair = new byte[size * 3];
                        _offset = 0;
                    }

                    segment = new MemorySegment(_pair, _offset, size);
                    _offset += size;

                    break;
            }

            _rents++;
            return segment;
        }

        public MemorySegment[] RentSegmentTable(int minLength) => new MemorySegment[minLength];

        public void GrowSegmentTable(ref MemorySegment[] table) => Array.Resize(ref table, table.Length * 2);
    }
}
