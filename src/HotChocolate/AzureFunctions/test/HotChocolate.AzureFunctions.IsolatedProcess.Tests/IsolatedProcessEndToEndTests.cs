using System.Threading.Tasks;
using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var httpRequestData = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(host.Services, @"
            query {
                person
            }
        ");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension
        // to execute via HttpRequestData...
        var httpResponseData =
            await requestExecutor.ExecuteAsync(httpRequestData).ConfigureAwait(false);

        // Read, Parse & Validate the response...
        var resultContent = await httpResponseData.ReadResponseContentAsync().ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_HttpContextAccessorTestAsync()
    {
        var host = new MockIsolatedProcessHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
            })
            .AddGraphQLFunction(graphQL =>
            {
                graphQL.AddQueryType(
                    d => d.Name("Query")
                        .Field("isHttpContextInjected")
                        .Resolve(context =>
                        {
                            var httpContext = context.Services.GetService<IHttpContextAccessor>()?
                                .HttpContext;
                            return httpContext != null;
                        }));
            })
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var httpRequestData = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(
            host.Services,
            @"query {
                isHttpContextInjected
            }");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var httpResponseData =
            await requestExecutor.ExecuteAsync(httpRequestData).ConfigureAwait(false);

        // Read, Parse & Validate the response...
        var resultContent =
            await httpResponseData.ReadResponseContentAsync().ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.True((bool)json.data.isHttpContextInjected);
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_BananaCakePopTestAsync()
    {
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var httpRequestData =
            TestHttpRequestDataHelper.NewBcpHttpRequestData(host.Services, "index.html");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var httpResponseData =
            await requestExecutor.ExecuteAsync(httpRequestData).ConfigureAwait(false);

        // Read, Parse & Validate the response...
        var resultContent = await httpResponseData.ReadResponseContentAsync().ConfigureAwait(false);
        Assert.NotNull(resultContent);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));
        Assert.True(resultContent!.Contains("<html") && resultContent.Contains("</html>"));
    }
}

