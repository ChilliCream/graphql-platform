using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Microsoft.Azure.WebJobs.Host.Config;
using Xunit;
using HotChocolate.AzureFunctions.Tests.Helpers;

namespace HotChocolate.AzureFunctions.Tests;

public class CustomBindingTests
{
    [Fact]
    public void AzFuncGraphQLCustomBindings_RegisterBindingConfigProvider()
    {
        ServiceProvider? serviceProvider = AzFuncTestHelper.CreateTestServiceCollectionWithGraphQLFunction().BuildServiceProvider();

        // the Binding Config Provider should resolve without error and be the expected type...
        IExtensionConfigProvider extensionConfigProvider = serviceProvider.GetRequiredService<IExtensionConfigProvider>();

        Assert.Equal(nameof(GraphQLExtensions), extensionConfigProvider.GetType().Name);
    }
}
