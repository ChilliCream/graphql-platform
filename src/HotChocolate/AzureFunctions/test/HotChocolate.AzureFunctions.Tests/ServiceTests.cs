using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.AzureFunctions.Tests;

public class ServiceTests
{
    [Fact]
    public void AddGraphQLFunction_RegisterExecutor()
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("test").Resolve("test"));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // the executor should resolve without error
        serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();
    }

    [Fact]
    public void AddGraphQLFunction_ServicesIsNull()
    {
        void Fail() => ((ServiceCollection)default!)
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("test").Resolve("test"));

        Assert.Throws<ArgumentNullException>(Fail);
    }
}