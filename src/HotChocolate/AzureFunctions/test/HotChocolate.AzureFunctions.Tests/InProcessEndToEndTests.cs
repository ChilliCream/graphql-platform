using HotChocolate.AzureFunctions.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.Tests;

public class InProcessEndToEndTests
{
    [Fact]
    public async Task AzFuncInProcess_EndToEndTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewGraphQLHttpContext(
            @"query {
                person
            }");

        // Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncInProcess_NitroTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder.Services.AddHttpContextAccessor();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(
                d => d.Name("Query")
                    .Field("NitroTest")
                    .Resolve("This is a test for Nitro File Serving..."));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewNitroHttpContext();

        // Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.NotNull(resultContent);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));
        Assert.True(resultContent!.Contains("<html") && resultContent.Contains("</html>"));
    }
}
