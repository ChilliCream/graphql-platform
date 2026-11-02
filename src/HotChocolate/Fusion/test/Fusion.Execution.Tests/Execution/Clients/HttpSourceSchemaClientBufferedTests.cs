using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class HttpSourceSchemaClientBufferedTests : SourceSchemaClientTestBase
{
    private const string JsonLines = "application/jsonl";

    [Fact]
    public async Task ExecuteBufferedAsync_Should_MatchExecuteAsync_When_RequestHasOneVariableSet()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            "application/json",
            """{"data":{"field":"a"}}""");
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 1);

        // act
        var streamed = await ExecuteStreamedAsync(client, context, request);
        var buffered = await ExecuteBufferedAsync(client, context, request);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(["""0 [0] {"field":"a"}"""]);
    }

    [Fact]
    public async Task ExecuteBufferedAsync_Should_MatchExecuteAsync_When_VariableBatchResultsArriveOutOfOrder()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"variableIndex":2,"data":{"field":"c"}}""",
            """{"variableIndex":0,"data":{"field":"a"}}""",
            """{"variableIndex":1,"data":{"field":"b"}}""");
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 3);

        // act
        var streamed = await ExecuteStreamedAsync(client, context, request);
        var buffered = await ExecuteBufferedAsync(client, context, request);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 [2] {"field":"c"}""",
            """0 [0] {"field":"a"}""",
            """0 [1] {"field":"b"}"""
        ]);
    }

    [Fact]
    public async Task ExecuteBufferedAsync_Should_FanOutErrorResult_When_VariableIndexIsMissing()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"variableIndex":1,"data":{"field":"b"}}""",
            """{"errors":[{"message":"broken"}]}""",
            """{"variableIndex":2,"data":{"field":"c"}}""");
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 3);

        // act
        var streamed = await ExecuteStreamedAsync(client, context, request);
        var buffered = await ExecuteBufferedAsync(client, context, request);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 [1] {"field":"b"}""",
            "0 [0] errors",
            "0 [1] errors",
            "0 [2] errors"
        ]);
    }

    [Fact]
    public async Task ExecuteBufferedAsync_Should_FanOutErrorResult_When_OperationBatchReturnsTooManyResults()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.RequestBatching,
            JsonLines,
            """{"data":{"field":"a"}}""",
            """{"data":{"field":"b"}}""",
            """{"errors":[{"message":"unexpected"}]}""");
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 2);

        // act
        var streamed = await ExecuteStreamedAsync(client, context, request);
        var buffered = await ExecuteBufferedAsync(client, context, request);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 [0] {"field":"a"}""",
            """0 [1] {"field":"b"}""",
            "0 [0] errors",
            "0 [1] errors"
        ]);
    }

    [Fact]
    public async Task ExecuteBufferedAsync_Should_DeliverReadResultsThenThrow_When_ResponseFailsMidStream()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            static () => new FailingStream(
                CreateJsonLines(
                    """{"variableIndex":0,"data":{"field":"a"}}""",
                    """{"variableIndex":1,"data":{"field":"b"}}""")),
            JsonLines);
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 3);

        // act
        var streamed = await ExecuteStreamedAsync(client, context, request);
        var buffered = await ExecuteBufferedAsync(client, context, request);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 [0] {"field":"a"}""",
            """0 [1] {"field":"b"}""",
            "IOException: The connection was reset."
        ]);
    }

    [Fact]
    public async Task ExecuteBufferedAsync_Should_StopReadingAndDisposeResponse_When_SinkDeclinesResult()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        var stream = new TrackingStream(
            CreateJsonLines(
                """{"variableIndex":0,"data":{"field":"a"}}""",
                """{"variableIndex":1,"data":{"field":"b"}}"""));
        await using var client = CreateClient(SourceSchemaClientCapabilities.Default, () => stream, JsonLines);
        var context = fixture.CreateContext();
        var request = CreateRequest(fixture.RootNode, variableCount: 2);
        var sink = new RecordingSink(stopAfter: 1);

        // act
        await client.ExecuteBufferedAsync(context, request, sink, TestContext.Current.CancellationToken);

        // assert
        new { ReceivedResults = sink.Format(), StreamDisposed = stream.IsDisposed }.MatchInlineSnapshot(
            """
            {
              "ReceivedResults": [
                "0 [0] {\"field\":\"a\"}"
              ],
              "StreamDisposed": true
            }
            """);
        sink.DisposeResults();
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_MatchExecuteBatchAsync_When_ResultsCarryRequestIndex()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"requestIndex":1,"data":{"field":"b"}}""",
            """{"requestIndex":0,"data":{"field":"a"}}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 0),
            CreateRequest(fixture.RootNode, variableCount: 0));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """1 root {"field":"b"}""",
            """0 root {"field":"a"}"""
        ]);
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_MapResultToEveryRequest_When_RequestIndexIsMissing()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"errors":[{"message":"broken"}]}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 0),
            CreateRequest(fixture.RootNode, variableCount: 2));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            "0 root errors",
            "1 [0] errors"
        ]);
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_MapResultToEveryVariableSet_When_VariableIndexIsMissing()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"requestIndex":1,"data":{"field":"b"}}""",
            """{"requestIndex":0,"errors":[{"message":"broken"}]}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 2),
            CreateRequest(fixture.RootNode, variableCount: 0));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """1 root {"field":"b"}""",
            "0 [0] errors",
            "0 [1] errors"
        ]);
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_MapFlattenedRequestIndex_When_VariableBatchingIsNotSupported()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.RequestBatching,
            JsonLines,
            """{"requestIndex":2,"data":{"field":"c"}}""",
            """{"requestIndex":0,"data":{"field":"a"}}""",
            """{"requestIndex":1,"data":{"field":"b"}}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 2),
            CreateRequest(fixture.RootNode, variableCount: 1));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """1 [0] {"field":"c"}""",
            """0 [0] {"field":"a"}""",
            """0 [1] {"field":"b"}"""
        ]);
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_MapResultsInOrder_When_FlattenedResultsCarryNoRequestIndex()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.RequestBatching,
            JsonLines,
            """{"data":{"field":"a"}}""",
            """{"data":{"field":"b"}}""",
            """{"data":{"field":"c"}}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 2),
            CreateRequest(fixture.RootNode, variableCount: 0));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 [0] {"field":"a"}""",
            """0 [1] {"field":"b"}""",
            """1 root {"field":"c"}"""
        ]);
    }

    [Fact]
    public async Task ExecuteBatchBufferedAsync_Should_DeliverPriorResultsThenThrow_When_RequestIndexIsOutOfRange()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var client = CreateClient(
            SourceSchemaClientCapabilities.Default,
            JsonLines,
            """{"requestIndex":0,"data":{"field":"a"}}""",
            """{"requestIndex":5,"data":{"field":"b"}}""");
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode, variableCount: 0),
            CreateRequest(fixture.RootNode, variableCount: 0));

        // act
        var streamed = await ExecuteBatchStreamedAsync(client, context, requests);
        var buffered = await ExecuteBatchBufferedAsync(client, context, requests);

        // assert
        Assert.Equal(streamed, buffered);
        buffered.MatchInlineSnapshots(
        [
            """0 root {"field":"a"}""",
            "InvalidOperationException: The batch response contains an invalid requestIndex '5'."
        ]);
    }

    [Fact]
    public async Task CanExecuteBuffered_Should_ReturnFalse_When_RequestNeedsIteratorPath()
    {
        // arrange
        await using var defaultClient = CreateClient(SourceSchemaClientCapabilities.Default, "application/json");
        await using var aliasClient = CreateClient(SourceSchemaClientCapabilities.AliasBatching, "application/json");

        // act
        var result = new
        {
            Query = defaultClient.CanExecuteBuffered(
                OperationType.Query,
                requiresFileUpload: false,
                variableCount: 2),
            Subscription = defaultClient.CanExecuteBuffered(
                OperationType.Subscription,
                requiresFileUpload: false,
                variableCount: 2),
            FileUpload = defaultClient.CanExecuteBuffered(
                OperationType.Query,
                requiresFileUpload: true,
                variableCount: 2),
            AliasBatchedVariableSets = aliasClient.CanExecuteBuffered(
                OperationType.Query,
                requiresFileUpload: false,
                variableCount: 2),
            AliasBatchingWithOneVariableSet = aliasClient.CanExecuteBuffered(
                OperationType.Query,
                requiresFileUpload: false,
                variableCount: 1)
        };

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "Query": true,
              "Subscription": false,
              "FileUpload": false,
              "AliasBatchedVariableSets": false,
              "AliasBatchingWithOneVariableSet": true
            }
            """);
    }

    [Fact]
    public async Task CanExecuteBatchBuffered_Should_ReturnFalse_When_BatchNeedsIteratorPath()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        await using var defaultClient = CreateClient(SourceSchemaClientCapabilities.Default, "application/json");
        await using var singleRequestClient = CreateClient(SourceSchemaClientCapabilities.None, "application/json");
        await using var aliasClient = CreateClient(SourceSchemaClientCapabilities.AliasBatching, "application/json");
        var query = CreateRequest(fixture.RootNode, variableCount: 0);

        // act
        var result = new
        {
            Query = defaultClient.CanExecuteBatchBuffered([query, query]),
            Subscription = defaultClient.CanExecuteBatchBuffered(
                [query, query with { OperationType = OperationType.Subscription }]),
            FileUpload = defaultClient.CanExecuteBatchBuffered(
                [query with { RequiresFileUpload = true }, query with { RequiresFileUpload = true }]),
            SingleRequests = singleRequestClient.CanExecuteBatchBuffered([query, query]),
            AliasBatchedOperations = aliasClient.CanExecuteBatchBuffered([query, query])
        };

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "Query": true,
              "Subscription": false,
              "FileUpload": false,
              "SingleRequests": false,
              "AliasBatchedOperations": false
            }
            """);
    }

    private static HttpSourceSchemaClient CreateClient(
        SourceSchemaClientCapabilities capabilities,
        string contentType,
        params string[] documents)
        => CreateClient(
            capabilities,
            () => new MemoryStream(
                contentType == JsonLines
                    ? CreateJsonLines(documents)
                    : Encoding.UTF8.GetBytes(string.Concat(documents))),
            contentType);

    private static HttpSourceSchemaClient CreateClient(
        SourceSchemaClientCapabilities capabilities,
        Func<Stream> createContent,
        string contentType)
    {
        var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new StaticResponseHandler(createContent, contentType)),
            disposeInnerClient: true);

        return new HttpSourceSchemaClient(
            graphQLClient,
            new HttpSourceSchemaClientConfiguration(
                "A",
                new Uri("http://localhost:5000/graphql"),
                capabilities: capabilities));
    }

    private static byte[] CreateJsonLines(params string[] documents)
    {
        var builder = new StringBuilder();

        foreach (var document in documents)
        {
            builder.Append(document);
            builder.Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static SourceSchemaClientRequest CreateRequest(ExecutionNode node, int variableCount)
    {
        var sourceText = "query { field }"u8.ToArray();
        var variables = ImmutableArray.CreateBuilder<VariableValues>(variableCount);

        for (var i = 0; i < variableCount; i++)
        {
            variables.Add(new VariableValues(new CompactPath([1, ~i]), JsonSegment.Empty));
        }

        return new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = "A",
            OperationType = OperationType.Query,
            OperationSourceText = new OperationSourceText(
                "Op",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            OperationDocument = Utf8GraphQLOperationParser.Parse(sourceText),
            Variables = variables.MoveToImmutable()
        };
    }

    private static async Task<List<string>> ExecuteStreamedAsync(
        HttpSourceSchemaClient client,
        OperationPlanContext context,
        SourceSchemaClientRequest request)
    {
        var sink = new RecordingSink();

        try
        {
            await foreach (var result in client.ExecuteAsync(context, request, TestContext.Current.CancellationToken))
            {
                sink.OnResult(0, result);
            }
        }
        catch (Exception exception)
        {
            sink.OnException(exception);
        }

        return sink.FormatAndDispose();
    }

    private static async Task<List<string>> ExecuteBufferedAsync(
        HttpSourceSchemaClient client,
        OperationPlanContext context,
        SourceSchemaClientRequest request)
    {
        var sink = new RecordingSink();

        try
        {
            await client.ExecuteBufferedAsync(context, request, sink, TestContext.Current.CancellationToken);
        }
        catch (Exception exception)
        {
            sink.OnException(exception);
        }

        return sink.FormatAndDispose();
    }

    private static async Task<List<string>> ExecuteBatchStreamedAsync(
        HttpSourceSchemaClient client,
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        var sink = new RecordingSink();

        try
        {
            await foreach (var batchResult in client.ExecuteBatchAsync(
                context,
                requests,
                TestContext.Current.CancellationToken))
            {
                sink.OnResult(batchResult.RequestIndex, batchResult.Result);
            }
        }
        catch (Exception exception)
        {
            sink.OnException(exception);
        }

        return sink.FormatAndDispose();
    }

    private static async Task<List<string>> ExecuteBatchBufferedAsync(
        HttpSourceSchemaClient client,
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        var sink = new RecordingSink();

        try
        {
            await client.ExecuteBatchBufferedAsync(context, requests, sink, TestContext.Current.CancellationToken);
        }
        catch (Exception exception)
        {
            sink.OnException(exception);
        }

        return sink.FormatAndDispose();
    }

    private sealed class RecordingSink(int stopAfter = int.MaxValue) : ISourceSchemaResultSink
    {
        private readonly List<(int RequestIndex, SourceSchemaResult Result)> _results = [];
        private Exception? _exception;

        public bool OnResult(int requestIndex, SourceSchemaResult result)
        {
            _results.Add((requestIndex, result));
            return _results.Count < stopAfter;
        }

        public void OnException(Exception exception) => _exception = exception;

        public List<string> Format()
        {
            var lines = new List<string>();

            foreach (var (requestIndex, result) in _results)
            {
                lines.Add($"{requestIndex} {FormatPath(result.Path)} {FormatData(result)}");
            }

            if (_exception is not null)
            {
                lines.Add($"{_exception.GetType().Name}: {_exception.Message}");
            }

            return lines;
        }

        public List<string> FormatAndDispose()
        {
            var lines = Format();
            DisposeResults();
            return lines;
        }

        public void DisposeResults()
        {
            foreach (var (_, result) in _results)
            {
                result.Dispose();
            }

            _results.Clear();
        }

        private static string FormatPath(CompactPath path)
        {
            if (path.IsRoot)
            {
                return "root";
            }

            var builder = new StringBuilder();

            foreach (var segment in path.Segments)
            {
                builder.Append('[');
                builder.Append(~segment);
                builder.Append(']');
            }

            return builder.ToString();
        }

        private static string FormatData(SourceSchemaResult result)
        {
            if (result.Data.ValueKind is JsonValueKind.Object)
            {
                return result.Data.GetRawText();
            }

            return result.Errors is null ? "none" : "errors";
        }
    }

    private sealed class StaticResponseHandler(Func<Stream> createContent, string contentType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = new StreamContent(createContent());
            content.Headers.TryAddWithoutValidation("Content-Type", contentType + "; charset=utf-8");

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
        }
    }

    private sealed class TrackingStream(byte[] content) : MemoryStream(content)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingStream(byte[] prefix) : Stream
    {
        private int _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_position == prefix.Length)
            {
                throw new IOException("The connection was reset.");
            }

            var length = Math.Min(buffer.Length, prefix.Length - _position);
            prefix.AsSpan(_position, length).CopyTo(buffer);
            _position += length;
            return length;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Read(buffer.Span));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromResult(Read(buffer.AsSpan(offset, count)));

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
