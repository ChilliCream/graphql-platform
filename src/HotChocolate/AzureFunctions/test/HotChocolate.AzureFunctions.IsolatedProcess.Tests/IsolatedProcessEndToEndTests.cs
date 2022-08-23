using System.Threading.Tasks;
using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests;
public class IsolatedProcessEndToEndTests
{
    [Fact]
    public async Task AzFuncIsolatedProcess_EndToEndTestAsync()
    {
        var hostBuilder = new MockIsolatedProcessHostBuilder();

        hostBuilder.AddGraphQLFunction(graphQL =>
        {
            graphQL.AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
        });

        var host = hostBuilder.Build();

        //The executor should resolve without error as a Required service...
        var requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();

        //Build an HttpRequestData that is valid for the Isolated Process to execute with...
        var httpRequestData = TestHttpRequestDataHelper.NewGraphQLHttpRequestData(host.Services, @"
            query {
                person
            }
        ");

        //Execute Query Test for end-to-end validation...
        //NOTE: This uses the new Az Func Isolated Process extension to execute via HttpRequestData...
        var httpResponseData = await requestExecutor.ExecuteAsync(httpRequestData).ConfigureAwait(false);

        //Read, Parse & Validate the response...
        var resultContent = await httpResponseData.ReadResponseContentAsync().ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent!);
        Assert.True(json.errors == null);
        Assert.Equal("Luke Skywalker",json.data.person.ToString());
    }
}
