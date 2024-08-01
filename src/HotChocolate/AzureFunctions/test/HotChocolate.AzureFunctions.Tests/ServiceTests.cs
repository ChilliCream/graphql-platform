using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using static HotChocolate.AzureFunctions.Tests.Helpers.AzFuncTestHelper;

namespace HotChocolate.AzureFunctions.Tests;

public class ServiceTests
{
    [Fact]
    public void AddGraphQLFunction_RegisterExecutor()
    {
        var serviceProvider =
            CreateTestServiceCollectionWithGraphQLFunction()
                .BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

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
