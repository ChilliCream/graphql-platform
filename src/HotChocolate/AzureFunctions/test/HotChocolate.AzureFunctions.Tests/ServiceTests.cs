using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Xunit;
using HotChocolate.AzureFunctions.Tests.Helpers;

namespace HotChocolate.AzureFunctions.Tests;

public class ServiceTests
{
    [Fact]
    public void AddGraphQLFunction_RegisterExecutor()
    {
        ServiceProvider? serviceProvider = AzFuncTestHelper.CreateTestServiceCollectionWithGraphQLFunction().BuildServiceProvider();

        //The executor should resolve without error as a Required service...
        IGraphQLRequestExecutor requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        Assert.Equal(nameof(DefaultGraphQLRequestExecutor), requestExecutor.GetType().Name);
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
