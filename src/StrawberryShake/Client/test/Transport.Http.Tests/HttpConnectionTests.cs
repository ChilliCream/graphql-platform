using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using StrawberryShake.Json;

namespace StrawberryShake.Transport.Http;

public class HttpConnectionTests : ServerTestBase
{
    public HttpConnectionTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Simple_Request()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument("query Test { __typename }");
        var request = new OperationRequest("Test", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        Assert.Collection(
            results,
            t => t.RootElement.ToString().MatchSnapshot());
    }

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            """
            query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                        ... HeroAppearsIn2 @defer(label: "HeroAppearsIn2")
                    }
                }
            }

            fragment HeroAppearsIn2 on Character {
                appearsIn
            }
            """);
        var request = new OperationRequest("GetHero", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        var snapshot = Snapshot.Create();

        var i = 0;
        foreach (var result in results.OrderBy(
            r => r.RootElement.GetPropertyOrNull("path")?.ToString()))
        {
            // The order of the patches is not guaranteed, that is why we normalize the order and
            // normalize the hasNext... overall the guarantee of patchability lies with the server.
            snapshot.Add(
                result.RootElement
                    .ToString()
                    .Replace("\"hasNext\":false", "\"hasNext\":true"),
                $"Result {++i}");
        }

        await snapshot.MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            """
            query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                    ... HeroAppearsIn @defer(label: "HeroAppearsIn")
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                    }
                }
            }

            fragment HeroAppearsIn on Character {
                appearsIn
            }
            """);
        var request = new OperationRequest("GetHero", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        var snapshot = Snapshot.Create();

        var i = 0;
        foreach (var result in results)
        {
            snapshot.Add(result.RootElement.ToString(), $"Result {++i}");
        }

        await snapshot.MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessResponseOffContextThread_When_CalledOnSynchronizationContext()
    {
        // arrange
        // the handler answers on a pool thread, like a response arriving from the network
        using var context = new SingleThreadSynchronizationContext();
        var client = new HttpClient(new PoolThreadHttpMessageHandler());
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument("query Test { __typename }");
        var request = new OperationRequest("Test", document);
        var connection = new ThreadRecordingHttpConnection(client);

        // act
        var (count, callerThreadId) = await context.RunAsync(
            async () =>
            {
                var responses = 0;

                await foreach (var _ in connection.ExecuteAsync(request))
                {
                    responses++;
                }

                return (responses, Environment.CurrentManagedThreadId);
            });

        // assert
        Assert.Equal(1, count);
        Assert.NotEqual(context.ThreadId, connection.ResponseThreadId);
        Assert.Equal(context.ThreadId, callerThreadId);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Complete_When_ContextThreadBlocksOnTask()
    {
        // arrange
        // the handler answers on a pool thread, like a response arriving from the network
        using var context = new SingleThreadSynchronizationContext();
        var client = new HttpClient(new PoolThreadHttpMessageHandler());
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument("query Test { __typename }");
        var request = new OperationRequest("Test", document);
        var connection = new HttpConnection(() => client);

        // act
        var completed = await context.RunAsync(
            () =>
            {
                var task = CountResponsesAsync(connection, request);
                return Task.FromResult(task.Wait(TimeSpan.FromSeconds(5)));
            });

        // assert
        Assert.True(completed);
    }

    private static async Task<int> CountResponsesAsync(HttpConnection connection, OperationRequest request)
    {
        var count = 0;

        // the generated client's executor consumes the connection the same way
        await foreach (var _ in connection.ExecuteAsync(request).ConfigureAwait(false))
        {
            count++;
        }

        return count;
    }

    private sealed class PoolThreadHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":{"__typename":"Query"}}""",
                    Encoding.UTF8,
                    "application/graphql-response+json")
            };
        }
    }

    private sealed class ThreadRecordingHttpConnection(HttpClient client) : HttpConnection(() => client)
    {
        public int ResponseThreadId { get; private set; }

        protected override Response<JsonDocument> CreateResponse(HttpResponseContext responseContext)
        {
            ResponseThreadId = Environment.CurrentManagedThreadId;
            return base.CreateResponse(responseContext);
        }
    }

    private sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly Thread _thread;

        public SingleThreadSynchronizationContext()
        {
            _thread = new Thread(ProcessQueue) { IsBackground = true };
            _thread.Start();
        }

        public int ThreadId => _thread.ManagedThreadId;

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

        public Task<T> RunAsync<T>(Func<Task<T>> action)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(_ => _ = RunCoreAsync(action, completion), null);
            return completion.Task;
        }

        public void Dispose() => _queue.CompleteAdding();

        private static async Task RunCoreAsync<T>(Func<Task<T>> action, TaskCompletionSource<T> completion)
        {
            try
            {
                completion.SetResult(await action());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }

        private void ProcessQueue()
        {
            SetSynchronizationContext(this);

            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }

    private sealed class MockDocument : IDocument
    {
        private readonly byte[] _query;

        public MockDocument([StringSyntax("graphql")] string query)
        {
            _query = Encoding.UTF8.GetBytes(query);
        }

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => _query;

        public DocumentHash Hash { get; } = new("MD5", "ABC");
    }
}
