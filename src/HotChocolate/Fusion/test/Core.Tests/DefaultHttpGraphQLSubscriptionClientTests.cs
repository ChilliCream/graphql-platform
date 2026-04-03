using System.Net;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion;

public class DefaultHttpGraphQLSubscriptionClientTests
{
    [Fact]
    public async Task SubscribeAsync_Passes_CancellationToken_To_Sse_Enumeration()
    {
        var sseStream = new ObservingSseStream();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(sseStream),
        };
        response.Content.Headers.ContentType = new("text/event-stream");

        using var httpClient = new HttpClient(new StaticResponseHandler(response));

        var config = new HttpClientConfiguration(
            clientName: "test",
            subgraphName: "reviews",
            endpointUri: new Uri("http://localhost/graphql"));

        await using var client = new DefaultHttpGraphQLSubscriptionClient(config, httpClient);

        var request = new SubgraphGraphQLRequest(
            subgraph: "reviews",
            document: "subscription OnNewReview { onNewReview { body } }",
            variableValues: null,
            extensions: null);

        using var cts = new CancellationTokenSource();
        await using var stream = client.SubscribeAsync(request, cts.Token).GetAsyncEnumerator();

        var moveNext = stream.MoveNextAsync().AsTask();
        await sseStream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cts.Cancel();

        var linked = await WaitUntilAsync(
            () => sseStream.CapturedToken.IsCancellationRequested,
            TimeSpan.FromSeconds(1));

        Assert.True(linked, "SSE enumeration token is not linked to the caller cancellation token.");

        sseStream.Release();
        await Task.WhenAny(moveNext, Task.Delay(TimeSpan.FromSeconds(2)));
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var end = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < end)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(20);
        }

        return condition();
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class ObservingSseStream : Stream
    {
        private readonly CancellationTokenSource _release = new();

        public TaskCompletionSource ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken CapturedToken { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => BlockUntilCanceledOrReleasedAsync(cancellationToken);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public void Release() => _release.Cancel();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _release.Cancel();
                _release.Dispose();
            }

            base.Dispose(disposing);
        }

        private async ValueTask<int> BlockUntilCanceledOrReleasedAsync(CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            ReadStarted.TrySetResult();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _release.Token);

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, linked.Token);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected in this test.
            }

            return 0;
        }
    }
}
