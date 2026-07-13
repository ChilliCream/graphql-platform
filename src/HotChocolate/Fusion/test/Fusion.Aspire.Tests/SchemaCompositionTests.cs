using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

public sealed class SchemaCompositionTests
{
    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_RetryWithoutLeakingEndpoint_When_ResponseReadFails()
    {
        // arrange
        const string secretUrl =
            "https://user:secret@products.example.com/graphql?token=secret";
        var attempts = 0;
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;

                return attempts == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(new FailingReadStream(secretUrl))
                    }
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("type Query { hello: String }")
                    };
            }));

        // act
        var schema = await composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri(secretUrl),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry =>
                    $"{entry.Message} | Exception: {entry.Exception?.Message ?? "<none>"}"));

        $$"""
        Schema: {{schema}}
        Attempts: {{attempts}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Schema: type Query { hello: String }
            Attempts: 2
            Debug:
            Waiting for schema service Products | Exception: <none>
            Schema service Products was unavailable (attempt 1/2) | Exception: <none>
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_PreserveCallerCancellation_When_RequestIsCanceled()
    {
        // arrange
        var attempts = 0;
        using var cancellation = new CancellationTokenSource();
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }));

        // act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 2,
                retryDelay: TimeSpan.Zero,
                cancellation.Token));

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry => entry.Message));

        $$"""
        Attempts: {{attempts}}
        Caller cancellation preserved: {{exception.CancellationToken == cancellation.Token}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Attempts: 1
            Caller cancellation preserved: True
            Debug:
            Waiting for schema service Products
            """);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }

    private sealed class FailingReadStream(string message) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new IOException(message);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => Task.FromException<int>(new IOException(message));

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<int>(new IOException(message));

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(new(logLevel, formatter(state, exception), exception));
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
