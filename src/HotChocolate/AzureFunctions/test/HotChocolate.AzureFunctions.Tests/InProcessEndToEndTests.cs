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

        ServiceProvider serviceProvider = hostBuilder.BuildServiceProvider();

        //The executor should resolve without error as a Required service...
        IGraphQLRequestExecutor requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        HttpContext httpContext = TestHttpContextHelper.NewGraphQLHttpContext(@"
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
        Assert.True(json.errors == null);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }
}
