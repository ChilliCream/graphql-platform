using System;
using System.Threading.Tasks;
using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
using HotChocolate.AzureFunctions.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

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

        //The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewGraphQLHttpContext(serviceProvider, @"
            query {
                person
            }
        ");

        //Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        //Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.NotNull(json.errors);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }

    [Fact]
    public async Task AzFuncInProcess_HttpContextAccessorTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder.Services
            .AddHttpContextAccessor();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("isHttpContextInjected").Resolve(context =>
            {
                var httpContext = context.Services.GetService<IHttpContextAccessor>()?.HttpContext;
                return httpContext != null;
            }));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        //The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewGraphQLHttpContext(serviceProvider, @"
            query {
                isHttpContextInjected
            }
        ");

        //Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        //Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.Null(json.errors);
        Assert.True((bool)json.data.isHttpContextInjected);
    }

    [Fact]
    public async Task AzFuncInProcess_BananaCakePopTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder.Services
            .AddHttpContextAccessor();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("BcpTest").Resolve("This is a test for BCP File Serving..."));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        //The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewBcpHttpContext(serviceProvider, "index.html");

        //Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        //Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.NotNull(resultContent);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));
        Assert.True(resultContent!.Contains("<html") && resultContent.Contains("</html>"));
    }
}
