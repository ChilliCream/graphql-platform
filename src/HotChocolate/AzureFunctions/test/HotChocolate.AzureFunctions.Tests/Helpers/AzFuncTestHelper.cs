using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctions.Tests.Helpers;

public static class AzFuncTestHelper
{
    public static ServiceCollection CreateTestServiceCollectionWithGraphQLFunction(
        string resolveValue = "test")
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("test").Resolve(resolveValue));

        return serviceCollection;
    }
}
