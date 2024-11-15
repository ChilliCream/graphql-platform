using GreenDonut;
using Xunit;
using static GreenDonut.TestHelpers;

namespace Microsoft.Extensions.DependencyInjection;

public class DataLoaderServiceCollectionExtensionsTests
{
    [Fact]
    public void ImplFactoryIsCalledWhenServiceIsResolved()
    {
        // arrange
        var factoryCalled = false;
        var fetch = CreateFetch<string, string>();
        var services = new ServiceCollection()
            .AddScoped<IBatchScheduler, ManualBatchScheduler>()
            .AddDataLoader(sp =>
            {
                factoryCalled = true;
                return new DataLoader<string, string>(fetch, sp.GetRequiredService<IBatchScheduler>());
            });
        var scope = services.BuildServiceProvider().CreateScope();

        // act
        var dataLoader = scope.ServiceProvider.GetRequiredService<DataLoader<string, string>>();

        // assert
        Assert.NotNull(dataLoader);
        Assert.True(factoryCalled);
    }

    [Fact]
    public void InterfaceImplFactoryIsCalledWhenServiceIsResolved()
    {
        // arrange
        var factoryCalled = false;
        var fetch = CreateFetch<string, string>();
        var services = new ServiceCollection()
            .AddScoped<IBatchScheduler, ManualBatchScheduler>()
            .AddDataLoader<IDataLoader<string, string>, DataLoader<string, string>>(sp =>
            {
                factoryCalled = true;
                return new DataLoader<string, string>(fetch, sp.GetRequiredService<IBatchScheduler>());
            });
        var scope = services.BuildServiceProvider().CreateScope();

        // act
        var dataLoader = scope.ServiceProvider.GetRequiredService<DataLoader<string, string>>();
        var asInterface = scope.ServiceProvider.GetRequiredService<IDataLoader<string, string>>();

        // assert
        Assert.NotNull(dataLoader);
        Assert.NotNull(asInterface);
        Assert.True(factoryCalled);
    }
}
