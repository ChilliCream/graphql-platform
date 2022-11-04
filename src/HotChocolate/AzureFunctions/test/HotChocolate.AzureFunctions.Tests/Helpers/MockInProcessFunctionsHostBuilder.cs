using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctions.Tests.Helpers;

internal class MockInProcessFunctionsHostBuilder : IFunctionsHostBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    public ServiceProvider BuildServiceProvider() => Services.BuildServiceProvider();
}
