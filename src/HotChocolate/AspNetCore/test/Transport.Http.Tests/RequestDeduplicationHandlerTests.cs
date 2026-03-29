using System.Net;
using HotChocolate.Language;

namespace HotChocolate.Transport.Http;

public class RequestDeduplicationHandlerTests
{
    [Fact]
    public async Task SendAsync_Should_DeduplicateIdenticalRequests_When_InFlight()
    {
        // arrange
        var callCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerHandler = new MockHandler(async (request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            await gate.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"hello\":\"world\"}}")
            };
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");

        // act
        var task1 = client.SendAsync(request1);
        var task2 = client.SendAsync(request2);

        // Let both requests enter the handler before releasing
        await Task.Delay(50);
        gate.SetResult();

        var response1 = await task1;
        var response2 = await task2;

        // assert
        Assert.Equal(1, callCount);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        var body2 = await response2.Content.ReadAsStringAsync();
        Assert.Equal("{\"data\":{\"hello\":\"world\"}}", body1);
        Assert.Equal("{\"data\":{\"hello\":\"world\"}}", body2);
    }

    [Fact]
    public async Task SendAsync_Should_NotDeduplicate_When_RequestsDiffer()
    {
        // arrange
        var callCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerHandler = new MockHandler(async (request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            await gate.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ goodbye }\"}");

        // act
        var task1 = client.SendAsync(request1);
        var task2 = client.SendAsync(request2);

        await Task.Delay(50);
        gate.SetResult();

        await task1;
        await task2;

        // assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_NotDeduplicate_When_HeadersDiffer()
    {
        // arrange
        var callCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerHandler = new MockHandler(async (request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            await gate.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        request1.Headers.TryAddWithoutValidation("Authorization", "Bearer token-a");

        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        request2.Headers.TryAddWithoutValidation("Authorization", "Bearer token-b");

        // act
        var task1 = client.SendAsync(request1);
        var task2 = client.SendAsync(request2);

        await Task.Delay(50);
        gate.SetResult();

        await task1;
        await task2;

        // assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_PassThrough_When_Mutation()
    {
        // arrange
        var callCount = 0;

        var innerHandler = new MockHandler((request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/graphql")
        {
            Content = new StringContent("{\"query\":\"mutation { create }\"}")
        };
        request.Options.Set(GraphQLHttpRequest.OperationKindOptionsKey, OperationType.Mutation);

        // act
        await client.SendAsync(request);

        // assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_PassThrough_When_NoOperationKind()
    {
        // arrange
        var callCount = 0;

        var innerHandler = new MockHandler((request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        // No operation kind set
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/graphql")
        {
            Content = new StringContent("{\"query\":\"{ hello }\"}")
        };

        // act
        await client.SendAsync(request);

        // assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_PassThrough_When_Subscription()
    {
        // arrange
        var callCount = 0;

        var innerHandler = new MockHandler((request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/graphql")
        {
            Content = new StringContent("{\"query\":\"subscription { onCreated }\"}")
        };
        request.Options.Set(GraphQLHttpRequest.OperationKindOptionsKey, OperationType.Subscription);

        // act
        await client.SendAsync(request);

        // assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateException_When_LeaderFails()
    {
        // arrange
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerHandler = new MockHandler(async (request, ct) =>
        {
            await gate.Task.WaitAsync(ct);
            throw new HttpRequestException("Connection refused");
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");

        // act
        var task1 = client.SendAsync(request1);
        var task2 = client.SendAsync(request2);

        await Task.Delay(50);
        gate.SetResult();

        // assert
        await Assert.ThrowsAsync<HttpRequestException>(() => task1);
        await Assert.ThrowsAsync<HttpRequestException>(() => task2);
    }

    [Fact]
    public async Task SendAsync_Should_PassThrough_When_MultipartContent()
    {
        // arrange
        var callCount = 0;

        var innerHandler = new MockHandler((request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("{\"query\":\"{ hello }\"}"), "operations");
        multipart.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.txt");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/graphql")
        {
            Content = multipart
        };
        request.Options.Set(GraphQLHttpRequest.OperationKindOptionsKey, OperationType.Query);

        // act
        await client.SendAsync(request);

        // assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_NotInterfere_When_SequentialIdenticalRequests()
    {
        // arrange
        var callCount = 0;

        var innerHandler = new MockHandler((request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        // act - send two identical requests sequentially
        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        await client.SendAsync(request1);

        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        await client.SendAsync(request2);

        // assert - both should hit the inner handler since the first completed before the second started
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SendAsync_Should_DeduplicateWithSameAuthHeaders_When_InFlight()
    {
        // arrange
        var callCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerHandler = new MockHandler(async (request, ct) =>
        {
            Interlocked.Increment(ref callCount);
            await gate.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        });

        using var handler = new RequestDeduplicationHandler { InnerHandler = innerHandler };
        using var client = new HttpClient(handler);

        var request1 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        request1.Headers.TryAddWithoutValidation("Authorization", "Bearer same-token");

        var request2 = CreateQueryRequest("http://localhost/graphql", "{\"query\":\"{ hello }\"}");
        request2.Headers.TryAddWithoutValidation("Authorization", "Bearer same-token");

        // act
        var task1 = client.SendAsync(request1);
        var task2 = client.SendAsync(request2);

        await Task.Delay(50);
        gate.SetResult();

        await task1;
        await task2;

        // assert
        Assert.Equal(1, callCount);
    }

    private static HttpRequestMessage CreateQueryRequest(string uri, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body)
        };
        request.Options.Set(GraphQLHttpRequest.OperationKindOptionsKey, OperationType.Query);
        return request;
    }

    private sealed class MockHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
