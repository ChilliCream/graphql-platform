using System.Net;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using HotChocolate.AzureFunctions.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests;

public class IsolatedProcessEndToEndTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("application/graphql-response+json, application/json;q=0.9")]
    [InlineData("application/graphql-response+json, text/html;q=0.1")]
    [InlineData("text/html;q=0")]
    public async Task Get_Should_ReturnNotFound_When_AcceptDoesNotPreferHtml(string? accept)
    {
        // arrange
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Get,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri);

        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Vary", out var vary));
        Assert.Equal(["Accept"], vary);
    }

    [Fact]
    public async Task Get_Should_RedirectToTool_When_AcceptPrefersHtml()
    {
        // arrange
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Get,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri);
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Vary", out var vary));
        Assert.Equal(["Accept"], vary);
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_EndToEndTestAsync()
    {
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(c => c.AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();

        // The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        // Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var request = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(
            host.Services,
            """
            query {
                person
            }
            """);

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var response = await requestExecutor.ExecuteAsync(request);

        // Read, Parse & Validate the response...
        var resultContent = await ReadResponseAsStringAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent);
        Assert.Null(json.errors);
        Assert.Equal("Luke Skywalker", json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncIsolatedProcess_FunctionsContextItemsTestAsync()
    {
        const string darkSideLeaderKey = "DarkSideLeader";

        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(graphQL =>
            {
                graphQL.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve(ctx =>
                    {
                        var darkSideLeader = ctx.ContextData.TryGetValue(
                            nameof(HttpContext),
                            out var httpContext)
                            ? (httpContext as HttpContext)?.Items[darkSideLeaderKey] as string
                            : null;

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
        request.FunctionContext.Items.Add(darkSideLeaderKey, "Darth Vader");

        // Execute Query Test for end-to-end validation...
        // NOTE: This uses the new Az Func Isolated Process extension to execute
        // via HttpRequestData...
        var response = await requestExecutor.ExecuteAsync(request);

        // Read, Parse & Validate the response...
        var resultContent = await ReadResponseAsStringAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent);
        Assert.Null(json.errors);
        Assert.Equal("Darth Vader", json.data.person.ToString());
    }

    [Fact]
    public async Task Post_Should_ReturnVaryAccept_When_QueryIsExecuted()
    {
        // arrange
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(
            host.Services,
            "{ person }");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Vary", out var vary));
        Assert.Equal(["Accept"], vary);
    }

    [Fact]
    public async Task Get_Should_ReturnVaryAccept_When_QueryIsExecuted()
    {
        // arrange
        var host = new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b.AddQueryType(
                    d => d.Name("Query").Field("person").Resolve("Luke Skywalker")))
            .Build();
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Get,
            new Uri(
                TestHttpContextHelper.DefaultAzFuncGraphQLUri,
                "?query=%7B%20person%20%7D"));

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Vary", out var vary));
        Assert.Equal(["Accept"], vary);
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
        Assert.True(resultContent.Contains("<html") && resultContent.Contains("</html>"));
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, null)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.MethodNotAllowed,
        "GET, HEAD, OPTIONS, POST")]
    public async Task Put_Should_ReturnMethodNotAllowed_When_MethodIsUnsupported(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string? expectedAllow)
    {
        // arrange
        var host = CreateHost(transportVersion);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Put,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            """{"query":"{ person }"}""");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, GetHeader(response, "Allow"));
        Assert.Equal(0, response.Body.Length);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, null)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.NoContent,
        "GET, HEAD, OPTIONS, POST")]
    public async Task Options_Should_ReturnAllowedMethods_When_EndpointIsRequested(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string? expectedAllow)
    {
        // arrange
        var host = CreateHost(transportVersion);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Options,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri);

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, GetHeader(response, "Allow"));
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20260903, HttpStatusCode.UnsupportedMediaType)]
    public async Task Post_Should_ReturnUnsupportedMediaType_When_ContentTypeIsUnsupported(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        var host = CreateHost(transportVersion);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Post,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            "{ person }",
            "text/plain");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(0, response.Body.Length);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, null)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.MethodNotAllowed,
        "OPTIONS, POST")]
    public async Task Get_Should_ReturnMethodNotAllowed_When_GetRequestsAreDisabled(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string? expectedAllow)
    {
        // arrange
        var host = CreateHost(transportVersion, enableGetRequests: false);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Get,
            new Uri(
                TestHttpContextHelper.DefaultAzFuncGraphQLUri,
                "?query=%7B%20person%20%7D"));

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, GetHeader(response, "Allow"));
    }

    [Fact]
    public async Task Put_Should_ReturnNotFound_When_PathIsBelowTheFunctionRoute()
    {
        // arrange
        var host = CreateHost(HttpTransportVersion.Draft20260903);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            HttpMethods.Put,
            new Uri(TestHttpContextHelper.DefaultAzFuncGraphQLUri, "/api/graphql/other"),
            """{"query":"{ person }"}""");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, null)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.UnsupportedMediaType,
        "application/json")]
    public async Task Query_Should_ReturnUnsupportedMediaType_When_ContentTypeIsUnsupported(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string? expectedAcceptQuery)
    {
        // arrange
        var host = CreateHost(transportVersion, enableQueryRequests: true);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            "QUERY",
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            "{ person }",
            "text/plain");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAcceptQuery, GetHeader(response, "Accept-Query"));
        Assert.Equal(0, response.Body.Length);
    }

    [Fact]
    public async Task Query_Should_ExecuteRequest_When_QueryRequestsAreEnabled()
    {
        // arrange
        var host = CreateHost(HttpTransportVersion.Latest, enableQueryRequests: true);
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
        var request = new MockHttpRequestData(
            new MockFunctionContext(host.Services),
            "QUERY",
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            """{"query":"{ person }"}""");

        // act
        var response = await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            """{"data":{"person":"Luke Skywalker"}}""",
            await ReadResponseAsStringAsync(response));
    }

    private static IHost CreateHost(
        HttpTransportVersion transportVersion,
        bool enableGetRequests = true,
        bool enableQueryRequests = false)
    {
        var formatterOptions = new HttpResponseFormatterOptions
        {
            HttpTransportVersion = transportVersion
        };

        return new MockIsolatedProcessHostBuilder()
            .AddGraphQLFunction(
                b => b
                    .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"))
                    .AddHttpResponseFormatter(formatterOptions)
                    .ModifyFunctionOptions(
                        o =>
                        {
                            o.EnableGetRequests = enableGetRequests;
                            o.EnableQueryRequests = enableQueryRequests;
                        }))
            .Build();
    }

    private static string? GetHeader(HttpResponseData response, string name)
        => response.Headers.TryGetValues(name, out var values)
            ? string.Join(", ", values)
            : null;

    private static async Task<string> ReadResponseAsStringAsync(HttpResponseData responseData)
    {
        responseData.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(responseData.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
