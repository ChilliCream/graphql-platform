using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Config;
using static HotChocolate.AzureFunctions.Tests.Helpers.AzFuncTestHelper;

namespace HotChocolate.AzureFunctions.Tests;

public class CustomBindingTests
{
    [Fact]
    public void AzFuncGraphQLCustomBindings_RegisterBindingConfigProvider()
    {
        var serviceProvider =
            CreateTestServiceCollectionWithGraphQLFunction()
                .BuildServiceProvider();

        // the Binding Config Provider should resolve without error and be the expected type...
        var extensionConfigProvider =
            serviceProvider.GetRequiredService<IExtensionConfigProvider>();

        Assert.Equal(nameof(GraphQLExtensions), extensionConfigProvider.GetType().Name);
    }
}
