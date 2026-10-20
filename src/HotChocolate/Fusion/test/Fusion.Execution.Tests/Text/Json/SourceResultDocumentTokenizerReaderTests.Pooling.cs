using System.Buffers;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerReaderTests
{
    [Theory]
    [MemberData(nameof(StitchedInputs))]
    public void Parse_Should_ReturnStitchBuffers_When_ParsingEnds(string input, bool accepted)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);
        var (segments, usedChunks, lastLength) = CreateSegments(bytes, ReaderLayout.Separate);
        using var listener = new BytePoolListener();

        // act
        listener.Start();
        var result = DescribeSource(segments, usedChunks, lastLength, padding: 0);
        listener.Stop();

        // assert
        Assert.Equal(accepted, result != Rejected);
        Assert.True(listener.RentCount > 0);
        Assert.Empty(listener.Outstanding);
    }

    public static TheoryData<string, bool> StitchedInputs()
    {
        var number = new string('1', 3000);

        return new TheoryData<string, bool>
        {
            { "[" + number + "]", true },
            { "{\"n\":" + number + "}trailing", true },
            { "[" + number + ",]", false },
            { "[" + number + "x]", false },
            { "{\"n\":" + number, false },
            { "{\"n\":" + number + ",\"s\":\"\\u12", false },
            { "[" + number + ",\"" + new string('s', 16000), false },
            { "[" + number + "," + new string('[', 64) + new string(']', 64) + "]", false }
        };
    }

    private sealed class BytePoolListener : EventListener
    {
        private readonly int _poolId = RuntimeHelpers.GetHashCode(ArrayPool<byte>.Shared);
        private readonly Dictionary<int, int> _outstanding = [];
        private int _threadId;

        public int RentCount { get; private set; }

        public IEnumerable<int> Outstanding => _outstanding.Where(t => t.Value != 0).Select(t => t.Key);

        public void Start() => _threadId = Environment.CurrentManagedThreadId;

        public void Stop() => _threadId = 0;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Buffers.ArrayPoolEventSource")
            {
                EnableEvents(eventSource, EventLevel.Verbose);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_threadId != Environment.CurrentManagedThreadId
                || eventData.EventName is not ("BufferRented" or "BufferReturned")
                || eventData.Payload is not { Count: >= 3 } payload
                || payload[2] is not int poolId
                || poolId != _poolId)
            {
                return;
            }

            var bufferId = (int)payload[0]!;
            _outstanding.TryGetValue(bufferId, out var count);

            if (eventData.EventName == "BufferRented")
            {
                RentCount++;
                _outstanding[bufferId] = count + 1;
            }
            else
            {
                _outstanding[bufferId] = count - 1;
            }
        }
    }
}
