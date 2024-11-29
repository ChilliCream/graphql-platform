using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests;

public class IsolatedProcessHostBuilderTests
{
    [Fact]
    public void AzFuncIsolatedProcess_HostBuilderSetupWithPortableConfigMatchingIsolatedProcess()
    {
        var hostBuilder = new MockIsolatedProcessHostBuilder();

        // Register using the config func that matches the Isolated Process configuration
        // so the config code is portable...
        hostBuilder.AddGraphQLFunction(
            b => b.AddQueryType(
                d => d.Name("Query").Field("test").Resolve("test")));

        AssertFunctionsHostBuilderIsValid(hostBuilder);
    }

    private static void AssertFunctionsHostBuilderIsValid(
        MockIsolatedProcessHostBuilder hostBuilder)
    {
        if (hostBuilder is null)
        {
            throw new ArgumentNullException(nameof(hostBuilder));
        }

        var host = hostBuilder.Build();

        // The executor should resolve without error as a Required service...
        host.Services.GetRequiredService<IGraphQLRequestExecutor>();
    }
}
