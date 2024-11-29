using System.Text;
using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests;

public class IsolatedProcessEndToEndTests
{
    [Fact]
    public async Task AzFuncIsolatedProcess_EndToEndTestAsync()
    {
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(graphQL =>
            {
                graphQL.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
            })
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var request = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(
            host.Services,
            @"query {
                person
            }");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var response = await requestExecutor.ExecuteAsync(request);

        // Read, Parse & Validate the response...
        var resultContent = await ReadResponseAsStringAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_FunctionsContextItemsTestAsync()
    {
        const string DarkSideLeaderKey = "DarkSideLeader";

        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(graphQL =>
            {
                graphQL.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve(ctx =>
                    {
                        var darkSideLeader = ctx.ContextData.TryGetValue(
                            nameof(HttpContext),
                            out var httpContext)
                            ? (httpContext as HttpContext)?.Items[DarkSideLeaderKey] as string
                            : default;

                        return darkSideLeader;
                    }));
            })
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var request = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(
            host.Services,
            @"query {
                person
            }");

        //Set Up our global Items now available from the Functions Context...
        request.FunctionContext.Items.Add(DarkSideLeaderKey, "Darth Vader");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var response = await requestExecutor.ExecuteAsync(request);

        // Read, Parse & Validate the response...
        var resultContent = await ReadResponseAsStringAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.Equal("Darth Vader", json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_NitroTestAsync()
    {
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var httpRequestData = TestHttpRequestDataHelper.NewNitroHttpRequestData(host.Services, "index.html");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var httpResponseData = await requestExecutor.ExecuteAsync(httpRequestData);

        // Read, Parse & Validate the response...
        var resultContent = await ReadResponseAsStringAsync(httpResponseData);
        Assert.NotNull(resultContent);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));
        Assert.True(resultContent!.Contains("<html") && resultContent.Contains("</html>"));
    }

    private static Task<string> ReadResponseAsStringAsync(HttpResponseData responseData)
    {
        responseData.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(responseData.Body, Encoding.UTF8);
        return reader.ReadToEndAsync();
    }
}
