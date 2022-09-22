using HotChocolate.AzureFunctions.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctions.Tests;

public class FunctionsHostBuilderTests
{
    [Fact]
    public void AzFuncInProcess_OriginalHostBuilderSetup()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("test").Resolve("test"));

        AssertFunctionsHostBuilderIsValid(hostBuilder);
    }

    [Fact]
    public void AzFuncInProcess_HostBuilderSetupWithPortableConfigMatchingIsolatedProcess()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        // Register using the config func that matches the Isolated Process
        // configuration so the config code is portable...
        hostBuilder.AddGraphQLFunction(
            b => b.AddQueryType(
                d => d.Name("Query").Field("test").Resolve("test")));

        AssertFunctionsHostBuilderIsValid(hostBuilder);
    }

    private void AssertFunctionsHostBuilderIsValid(MockInProcessFunctionsHostBuilder hostBuilder)
    {
        var serviceProvider = hostBuilder.BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();
    }
}
