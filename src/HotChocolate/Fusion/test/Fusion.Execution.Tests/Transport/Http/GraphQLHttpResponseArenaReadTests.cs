using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Transport;

public class GraphQLHttpResponseArenaReadTests
{
    [Theory]
    [InlineData(128, 1, false)]
    [InlineData(1024, 1024, false)]
    [InlineData(1025, 1024, false)]
    [InlineData(3072, 4096, false)]
    [InlineData(3073, 4096, false)]
    [InlineData(32768, 4096, false)]
    [InlineData(3073, 137, true)]
    public async Task ReadAsResult_Should_PreservePayload_When_ReadsAreFragmented(
        int payloadLength,
        int fragmentLength,
        bool asynchronous)
    {
        // arrange
        var payload = CreatePayload(payloadLength);
        var stream = new FragmentedStream(Encoding.UTF8.GetBytes(payload), fragmentLength, asynchronous);
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        using var arena = new MemoryArena();

        // act
        using var document = await response.ReadAsResultAsync(arena, TestContext.Current.CancellationToken);
        arena.Seal();

        // assert
        Assert.Equal(payload, document.Root.GetRawText());
        Assert.Equal(payloadLength, stream.BytesRead);
        Assert.True(stream.IsDisposed);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(128)]
    [InlineData(200)]
    [InlineData(32768)]
    public async Task ReadAsResult_Should_PreservePayload_When_ContentLengthIsDeclared(int contentLength)
    {
        // arrange
        var payload = CreatePayload(128);
        var stream = new FragmentedStream(Encoding.UTF8.GetBytes(payload), 31, asynchronous: false);
        using var response = CreateResponse(stream, contentLength, "utf-8");
        using var arena = new MemoryArena();

        // act
        using var document = await response.ReadAsResultAsync(arena, TestContext.Current.CancellationToken);
        arena.Seal();

        // assert
        Assert.Equal(payload, document.Root.GetRawText());
        Assert.True(stream.IsDisposed);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4096)]
    public async Task ReadAsResult_Should_TranscodePayload_When_ResponseUsesUtf16(int fragmentLength)
    {
        // arrange
        var value = string.Concat(Enumerable.Repeat("é🔭", 4096));
        var payload = "{\"data\":{\"value\":\"" + value + "\"}}";
        var stream = new FragmentedStream(Encoding.Unicode.GetBytes(payload), fragmentLength, asynchronous: false);
        using var response = CreateResponse(stream, contentLength: null, "utf-16");
        using var arena = new MemoryArena();

        // act
        using var document = await response.ReadAsResultAsync(arena, TestContext.Current.CancellationToken);
        arena.Seal();

        // assert
        Assert.Equal(value, document.Root.GetProperty("data").GetProperty("value").GetString());
        Assert.True(stream.IsDisposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"data\":")]
    public async Task ReadAsResult_Should_DisposeStream_When_PayloadIsInvalid(string payload)
    {
        // arrange
        var stream = new FragmentedStream(Encoding.UTF8.GetBytes(payload), 3, asynchronous: false);
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        using var arena = new MemoryArena();

        // act
        var exception = await Record.ExceptionAsync(async () =>
            await response.ReadAsResultAsync(arena, TestContext.Current.CancellationToken));
        arena.Seal();

        // assert
        Assert.IsAssignableFrom<JsonException>(exception);
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public async Task ReadAsResult_Should_DisposeStream_When_ReadIsCanceled()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var stream = new FragmentedStream(Encoding.UTF8.GetBytes(CreatePayload(3073)), 137, asynchronous: true)
        {
            BeforeRead = position =>
            {
                if (position > 0)
                {
                    cancellation.Cancel();
                }
            }
        };
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        using var arena = new MemoryArena();

        // act
        var exception = await Record.ExceptionAsync(async () =>
            await response.ReadAsResultAsync(arena, cancellation.Token));
        arena.Seal();

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.Equal(137, stream.BytesRead);
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public async Task ReadAsResult_Should_ObserveCancellation_When_StreamIgnoresToken()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var stream = new FragmentedStream(Encoding.UTF8.GetBytes(CreatePayload(3073)), 137, asynchronous: false)
        {
            IgnoreCancellation = true,
            AfterRead = _ => cancellation.Cancel()
        };
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        using var arena = new MemoryArena();

        // act
        var exception = await Record.ExceptionAsync(async () =>
        {
            using var document = await response.ReadAsResultAsync(arena, cancellation.Token);
        });
        arena.Seal();

        // assert
        var canceled = Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(137, stream.BytesRead);
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public async Task ReadAsResult_Should_ObserveCancellation_When_FullCapacityRequiresEofProbe()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var capacity = 0L;
        for (var i = 0; i < SourceResultDocument.DataMaxChunks; i++)
        {
            capacity += SourceResultDocument.GetDataChunkSize(i);
        }
        var stream = new CapacityReadStream(capacity, new IOException("The EOF probe must not be read."))
        {
            AfterRead = position =>
            {
                if (position == capacity)
                {
                    cancellation.Cancel();
                }
            }
        };
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        var arena = new UnparsedMemoryArena();

        // act
        var exception = await Record.ExceptionAsync(async () =>
            await response.ReadAsResultAsync(arena, cancellation.Token));

        // assert
        var canceled = Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(capacity, stream.BytesRead);
    }

    [Fact]
    public async Task ReadAsResult_Should_ObserveReadFailure_When_FullCapacityRequiresEofProbe()
    {
        // arrange
        var capacity = 0L;
        for (var i = 0; i < SourceResultDocument.DataMaxChunks; i++)
        {
            capacity += SourceResultDocument.GetDataChunkSize(i);
        }
        var expected = new IOException("The final body read failed.");
        var stream = new CapacityReadStream(capacity, expected);
        using var response = CreateResponse(stream, contentLength: null, "utf-8");
        var arena = new UnparsedMemoryArena();

        // act
        var exception = await Record.ExceptionAsync(async () =>
            await response.ReadAsResultAsync(arena, TestContext.Current.CancellationToken));

        // assert
        Assert.Same(expected, exception);
        Assert.Equal(capacity, stream.BytesRead);
    }

    private static string CreatePayload(int length)
    {
        const string prefix = "{\"data\":{\"value\":\"";
        const string suffix = "\"}}";
        return prefix + new string('a', length - prefix.Length - suffix.Length) + suffix;
    }

    private static GraphQLHttpResponse CreateResponse(Stream stream, long? contentLength, string charset)
    {
        var message = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        };
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/graphql-response+json")
        {
            CharSet = charset
        };
        message.Content.Headers.ContentLength = contentLength;
        return new GraphQLHttpResponse(message);
    }

    private sealed class FragmentedStream(byte[] payload, int fragmentLength, bool asynchronous) : Stream
    {
        public int BytesRead { get; private set; }

        public bool IsDisposed { get; private set; }

        public Action<int>? BeforeRead { get; init; }

        public Action<int>? AfterRead { get; init; }

        public bool IgnoreCancellation { get; init; }

        public override bool CanRead => !IsDisposed;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (asynchronous)
            {
                await Task.Yield();
            }

            BeforeRead?.Invoke(BytesRead);
            if (!IgnoreCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var length = Math.Min(Math.Min(buffer.Length, fragmentLength), payload.Length - BytesRead);
            payload.AsMemory(BytesRead, length).CopyTo(buffer);
            BytesRead += length;
            AfterRead?.Invoke(BytesRead);
            return length;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class UnparsedMemoryArena : IMemoryArena
    {
        private readonly byte[] _buffer = new byte[128 * 1024];

        public MemorySegment Rent(int size) => new(_buffer, 0, size);

        public MemorySegment[] RentSegmentTable(int minLength) => new MemorySegment[minLength];

        public void GrowSegmentTable(ref MemorySegment[] table) => Array.Resize(ref table, table.Length * 2);
    }

    private sealed class CapacityReadStream(long capacity, IOException exception) : Stream
    {
        public long BytesRead { get; private set; }
        public Action<long>? AfterRead { get; init; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (BytesRead == capacity)
            {
                throw exception;
            }

            var count = (int)Math.Min(buffer.Length, capacity - BytesRead);
            BytesRead += count;
            AfterRead?.Invoke(BytesRead);
            return ValueTask.FromResult(count);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
