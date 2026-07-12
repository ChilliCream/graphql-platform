using System.Net;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Transport.Http;

public class SseReaderTests
{
    private const string PayloadPrefix = "{\"data\":{\"value\":\"";
    private const string PayloadSuffix = "\"}}";

    [Theory]
    [InlineData(32)]
    [InlineData(175)]
    [InlineData(2 * 1024)]
    [InlineData(10 * 1024)]
    [InlineData(16 * 1024)]
    public async Task Read_Should_Use_Single_Segment_When_Payload_Is_Within_Threshold(
        int payloadLength)
    {
        // arrange
        var (payload, expectedValue) = CreatePayload(payloadLength);
        var arena = new RecordingMemoryArena(CommonTestExtensions.CreateArena());
        var arenaSource = new RecordingMemoryArenaSource(arena);

        // act
        var values = await ReadValuesAsync(payload, arenaSource);

        // assert
        Assert.Equal([expectedValue], values);
        Assert.Equal(1, arenaSource.GetNextArenaCalls);
        Assert.Equal(
            [
                new ArenaCall(nameof(IMemoryArena.Rent), payloadLength),
                new ArenaCall(nameof(IMemoryArena.RentSegmentTable), 1)
            ],
            arena.Calls.Take(2));
    }

    [Theory]
    [InlineData((16 * 1024) + 1)]
    [InlineData(20 * 1024)]
    public async Task Read_Should_Use_Geometric_Segments_When_Payload_Exceeds_Threshold(
        int payloadLength)
    {
        // arrange
        var (payload, expectedValue) = CreatePayload(payloadLength);
        var arena = new RecordingMemoryArena(CommonTestExtensions.CreateArena());
        var arenaSource = new RecordingMemoryArenaSource(arena);

        // act
        var values = await ReadValuesAsync(payload, arenaSource);

        // assert
        Assert.Equal([expectedValue], values);
        Assert.Equal(1, arenaSource.GetNextArenaCalls);
        Assert.Equal(
            [
                new ArenaCall(nameof(IMemoryArena.RentSegmentTable), 64),
                new ArenaCall(nameof(IMemoryArena.Rent), 1024),
                new ArenaCall(nameof(IMemoryArena.Rent), 2048),
                new ArenaCall(nameof(IMemoryArena.Rent), 4096),
                new ArenaCall(nameof(IMemoryArena.Rent), 8192)
            ],
            arena.Calls.Take(5));
    }

    [Fact]
    public async Task Read_Should_Not_Request_An_Arena_When_Next_Event_Has_No_Data()
    {
        // arrange
        var arena = new RecordingMemoryArena(CommonTestExtensions.CreateArena());
        var arenaSource = new RecordingMemoryArenaSource(arena);

        // act
        var values = await ReadValuesAsync(string.Empty, arenaSource);

        // assert
        Assert.Empty(values);
        Assert.Equal(0, arenaSource.GetNextArenaCalls);
        Assert.Empty(arena.Calls);
    }

    private static async Task<IReadOnlyList<string>> ReadValuesAsync(
        string payload,
        IMemoryArenaSource arenaSource)
    {
        var responseBody = $"event: next\ndata: {payload}\n\nevent: complete\n\n";
        using var message = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(responseBody))
        };
        var reader = new SseReader(message, arenaSource);
        var values = new List<string>();

        await foreach (var document in reader.WithCancellation(TestContext.Current.CancellationToken))
        {
            using (document)
            {
                values.Add(document.Root.GetProperty("data").GetProperty("value").GetString()!);
            }
        }

        return values;
    }

    private static (string Payload, string Value) CreatePayload(int payloadLength)
    {
        var valueLength = payloadLength - PayloadPrefix.Length - PayloadSuffix.Length;

        if (valueLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength));
        }

        var value = new string('x', valueLength);
        return (PayloadPrefix + value + PayloadSuffix, value);
    }

    private readonly record struct ArenaCall(string Method, int Size);

    private sealed class RecordingMemoryArena(IMemoryArena inner) : IMemoryArena
    {
        public List<ArenaCall> Calls { get; } = [];

        public MemorySegment Rent(int size)
        {
            Calls.Add(new ArenaCall(nameof(Rent), size));
            return inner.Rent(size);
        }

        public MemorySegment[] RentSegmentTable(int minLength)
        {
            Calls.Add(new ArenaCall(nameof(RentSegmentTable), minLength));
            return inner.RentSegmentTable(minLength);
        }

        public void GrowSegmentTable(ref MemorySegment[] table)
        {
            Calls.Add(new ArenaCall(nameof(GrowSegmentTable), table.Length));
            inner.GrowSegmentTable(ref table);
        }
    }

    private sealed class RecordingMemoryArenaSource(IMemoryArena arena) : IMemoryArenaSource
    {
        public int GetNextArenaCalls { get; private set; }

        public IMemoryArena GetNextArena()
        {
            GetNextArenaCalls++;
            return arena;
        }
    }
}
