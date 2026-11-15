using System.Text;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class ResponseContentLengthTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Theory]
    [InlineData("application/graphql-response+json")]
    [InlineData("application/json")]
    public async Task Post_Should_SendContentLength_When_ResultIsSingleJson(string accept)
    {
        // arrange
        using var server = CreateServer();
        var client = server.CreateClient();
        using var request = CreateRequest("""{ "query": "{ greeting }" }""", accept);

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var contentLength = response.Content.Headers.ContentLength;
        var body = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(body.Length, contentLength);
        Encoding.UTF8.GetString(body).MatchInlineSnapshot(
            """
            {"data":{"greeting":"hello"}}
            """);
    }

    [Theory]
    [InlineData("""{ "query": "{ greeting ... @defer { items(count: 2) } }" }""", "multipart/mixed")]
    [InlineData("""{ "query": "{ greeting ... @defer { items(count: 2) } }" }""", "text/event-stream")]
    [InlineData("""{ "query": "{ greeting ... @defer { items(count: 2) } }" }""", "application/jsonl")]
    [InlineData("""{ "query": "{ greeting }" }""", "multipart/mixed")]
    [InlineData("""{ "query": "{ greeting }" }""", "text/event-stream")]
    [InlineData("""[{ "query": "{ greeting }" }, { "query": "{ greeting }" }]""", "application/jsonl")]
    public async Task Post_Should_OmitContentLength_When_ResultIsNotSingleJson(
        string requestBody,
        string accept)
    {
        // arrange
        using var server = CreateServer();
        var client = server.CreateClient();
        using var request = CreateRequest(requestBody, accept);

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var contentLength = response.Content.Headers.ContentLength;
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Null(contentLength);
        Assert.Contains("\"greeting\":\"hello\"", body);
    }

    [Fact]
    public async Task Post_Should_SendCompleteBody_When_MiddlewareReplacesResponseBody()
    {
        // arrange
        using var server = CreateServer(
            configurePipeline: app => app.Use(
                async (context, next) =>
                {
                    var original = context.Response.Body;
                    await using var buffer = new MemoryStream();
                    context.Response.Body = buffer;

                    try
                    {
                        await next(context);
                    }
                    finally
                    {
                        context.Response.Body = original;
                    }

                    buffer.Position = 0;
                    await buffer.CopyToAsync(original, context.RequestAborted);
                }));
        var client = server.CreateClient();
        using var request = CreateRequest(
            """{ "query": "{ items(count: 100000) }" }""",
            "application/graphql-response+json");

        // act
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var contentLength = response.Content.Headers.ContentLength;
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(CreateItemsBody(100000), body);
        Assert.Equal(Encoding.UTF8.GetByteCount(body), contentLength);
    }

    [Fact]
    public async Task Post_Should_AbortRequest_When_FormattingFails()
    {
        // arrange
        using var server = CreateServer(
            configureExecutor: builder => builder.UseRequest(
                next => async context =>
                {
                    await next(context);

                    if (context.Result is OperationResult result)
                    {
                        result.Extensions = result.Extensions.SetItem("broken", new UnformattableValue());
                    }
                },
                key: "BreakResult",
                after: WellKnownRequestMiddleware.ExceptionMiddleware));
        var client = server.CreateClient();
        using var request = CreateRequest(
            """{ "query": "{ greeting }" }""",
            "application/graphql-response+json");

        // act
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => client.SendAsync(request, TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("The application aborted the request.", exception.Message);
    }

    private static string CreateItemsBody(int count)
    {
        var body = new StringBuilder("{\"data\":{\"items\":[");

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                body.Append(',');
            }

            body.Append("\"item-").Append(i).Append('"');
        }

        return body.Append("]}}").ToString();
    }

    private static HttpRequestMessage CreateRequest(string body, string accept)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Accept", accept);
        return request;
    }

    private TestServer CreateServer(
        Action<IApplicationBuilder>? configurePipeline = null,
        Action<IRequestExecutorBuilder>? configureExecutor = null)
        => ServerFactory.Create(
            services =>
            {
                var builder = services
                    .AddRouting()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .ModifyOptions(o => o.EnableDefer = true)
                    .ModifyServerOptions(o => o.Batching = AllowedBatching.All);
                configureExecutor?.Invoke(builder);
            },
            app =>
            {
                configurePipeline?.Invoke(app);
                app.UseRouting().UseEndpoints(endpoints => endpoints.MapGraphQL());
            });

    public sealed class Query
    {
        public string Greeting => "hello";

        public IEnumerable<string> GetItems(int count)
            => Enumerable.Range(0, count).Select(i => $"item-{i}");
    }

    private sealed class UnformattableValue
    {
        public override string ToString() => throw new InvalidOperationException("boom");
    }
}
