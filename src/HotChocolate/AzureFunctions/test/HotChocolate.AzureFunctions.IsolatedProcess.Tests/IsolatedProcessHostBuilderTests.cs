using System;
using HotChocolate.Types;
using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HotChocolate.AzureFunctions.Tests;

public class IsolatedProcessHostBuilderTests
{
    [Fact]
    public void AzFuncIsolatedProcess_HostBuilderSetupWithPortableConfigMatchingIsolatedProcess()
    {
        var hostBuilder = new MockIsolatedProcessHostBuilder();

        //Register using the config func that matches the Isolated Process configuration so the config code is portable...
        hostBuilder.AddGraphQLFunction(graphQL =>
        {
            graphQL.AddQueryType(d => d.Name("Query").Field("test").Resolve("test"));
        });

        AssertFunctionsHostBuilderIsValid(hostBuilder);
    }

    private void AssertFunctionsHostBuilderIsValid(MockIsolatedProcessHostBuilder hostBuilder)
    {
        IHost? host = hostBuilder.Build();
        Assert.NotNull(host?.Services);

        //The executor should resolve without error as a Required service...
        IGraphQLRequestExecutor requestExecutor = host.Services.GetRequiredService<IGraphQLRequestExecutor>();
    }
}
