using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Transport.Http;

public class DefaultGraphQLHttpClientWriterReuseTests
{
    private static readonly Uri s_uri = new("http://localhost/graphql");

    [Fact]
    public async Task SendAsync_Should_PreserveBothBodies_When_RequestsOverlap()
    {
        // arrange
        var handler = new CapturingHandler { HoldFirst = true };
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var first = client.SendAsync(new GraphQLHttpRequest("{ first }", s_uri), TestContext.Current.CancellationToken);
        await handler.FirstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var second = await client.SendAsync(
            new GraphQLHttpRequest("{ second }", s_uri), TestContext.Current.CancellationToken);
        handler.ReleaseFirst.TrySetResult();
        using var firstResult = await first;

        // assert
        Assert.Equal(["{\"query\":\"{ first }\"}", "{\"query\":\"{ second }\"}"], handler.Bodies.ToArray());
        Assert.Equal("{\"query\":\"{ first }\"}", handler.FirstBodyAfterRelease);
    }

    [Fact]
    public async Task SendAsync_Should_IsolateWriter_When_QueryAccessReentersClient()
    {
        // arrange
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        using var memory = new ReentrantMemory("{ outer }"u8.ToArray());
        var operation = new OperationRequest(memory.Memory, null, null, null, VariableValues.Empty, JsonSegment.Empty);
        Task<GraphQLHttpResponse>? nested = null;
        memory.OnRead = () => nested = client.SendAsync(
            new GraphQLHttpRequest("{ nested }", s_uri), TestContext.Current.CancellationToken);

        // act
        using var outer = await client.SendAsync(new GraphQLHttpRequest(operation, s_uri), TestContext.Current.CancellationToken);
        using var inner = await nested!;

        // assert
        Assert.Equal(["{\"query\":\"{ nested }\"}", "{\"query\":\"{ outer }\"}"], handler.Bodies.ToArray());
    }

    [Fact]
    public async Task SendAsync_Should_ResetWriter_When_QueryMemoryAccessThrows()
    {
        // arrange
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        using var memory = new ReentrantMemory("{ throwing }"u8.ToArray());
        var invalid = new OperationRequest(memory.Memory, null, null, null, VariableValues.Empty, JsonSegment.Empty);
        memory.OnRead = () => throw new InvalidOperationException("Query memory unavailable.");

        // act
        var error = await Record.ExceptionAsync(() => client.SendAsync(
            new GraphQLHttpRequest(invalid, s_uri), TestContext.Current.CancellationToken));
        using var valid = await client.SendAsync(
            new GraphQLHttpRequest("{ valid }", s_uri), TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(["{\"query\":\"{ valid }\"}"], handler.Bodies.ToArray());
    }

    [Fact]
    public async Task SendAsync_Should_KeepDistinctWriters_When_BatchesContainCustomOperations()
    {
        // arrange
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        var first = new RetainingOperation();
        var second = new RetainingOperation();
        var ordinary = new OperationRequest("{ regular }"u8.ToArray(), null, null, null, VariableValues.Empty, JsonSegment.Empty);

        // act
        using var firstResult = await client.SendAsync(
            new GraphQLHttpRequest(new OperationBatchRequest([first, ordinary]), s_uri), TestContext.Current.CancellationToken);
        using var secondResult = await client.SendAsync(
            new GraphQLHttpRequest(new OperationBatchRequest([second, ordinary]), s_uri), TestContext.Current.CancellationToken);

        // assert
        Assert.NotSame(first.Writer, second.Writer);
        Assert.Equal(
            Enumerable.Repeat("[{\"query\":\"{ custom }\"},{\"query\":\"{ regular }\"}]", 2),
            handler.Bodies.ToArray());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public ConcurrentQueue<string> Bodies { get; } = new();
        public bool HoldFirst { get; set; }
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? FirstBodyAfterRelease { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Bodies.Enqueue(await request.Content!.ReadAsStringAsync(cancellationToken));

            if (HoldFirst)
            {
                HoldFirst = false;
                FirstStarted.TrySetResult();
                await ReleaseFirst.Task.WaitAsync(cancellationToken);
                FirstBodyAfterRelease = Encoding.UTF8.GetString(
                    await request.Content.ReadAsByteArrayAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{}}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class ReentrantMemory(byte[] bytes) : MemoryManager<byte>
    {
        public Action? OnRead { get; set; }

        public override Span<byte> GetSpan()
        {
            var onRead = OnRead;
            OnRead = null;
            onRead?.Invoke();
            return bytes;
        }

        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }
    }

    private sealed class RetainingOperation : IOperationRequest
    {
        public string? Id => null;
        public ReadOnlyMemory<byte> Query => "{ custom }"u8.ToArray();
        public string? OperationName => null;
        public ErrorHandlingMode? OnError => null;
        public JsonSegment Extensions => JsonSegment.Empty;
        public JsonWriter? Writer { get; private set; }

        public void WriteTo(JsonWriter writer)
        {
            Writer = writer;
            writer.WriteStartObject();
            writer.WritePropertyName("query");
            writer.WriteStringValue("{ custom }");
            writer.WriteEndObject();
        }
    }
}
