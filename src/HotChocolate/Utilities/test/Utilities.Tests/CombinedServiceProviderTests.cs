using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Utilities;

public class CombinedServiceProviderTests
{
    [Fact]
    public void CombineServiceCollections()
    {
        var servicesA = new ServiceCollection()
            .AddSingleton<IService, ServiceA>()
            .BuildServiceProvider();

        var servicesB = new ServiceCollection()
            .AddSingleton<IService, ServiceB>()
            .AddSingleton<IService, ServiceC>()
            .BuildServiceProvider();

        var combinedServices = new CombinedServiceProvider(servicesA, servicesB);

        Assert.Collection(
            combinedServices.GetServices<IService>(),
            t => Assert.IsType<ServiceA>(t),
            t => Assert.IsType<ServiceB>(t),
            t => Assert.IsType<ServiceC>(t));
    }

    [Fact]
    public void CombineServiceCollections_2()
    {
        var servicesA = new ServiceCollection()
            .AddSingleton<IService, ServiceA>()
            .BuildServiceProvider();

        var servicesB = new ServiceCollection()
            .AddSingleton<IService, ServiceB>()
            .AddSingleton<IService, ServiceC>()
            .BuildServiceProvider();

        var combinedServices = new CombinedServiceProvider(servicesB, servicesA);

        Assert.Collection(
            combinedServices.GetServices<IService>(),
            t => Assert.IsType<ServiceB>(t),
            t => Assert.IsType<ServiceC>(t),
            t => Assert.IsType<ServiceA>(t));
    }

    [Fact]
    public void GetServiceWithoutError()
    {
        /***
        Note
        ==========
        This code is adapted from `HotChocolate.SchemaBuilder.Setup.InitializeInterceptors<T>`,
        which was the next relevant call down "down the stack" in the error traces which
        motivate changes to the subject-under-test (i.e. CombinedServiceProvider).
        ***/
        IServiceProvider stringServices = new DictionaryServiceProvider(
            (typeof(IEnumerable<string>), new List<string>(new[] { "one", "two", })));

        IServiceProvider numberServices = new DictionaryServiceProvider(
            (typeof(IEnumerable<int>), new List<int>(new[] { 1, 2, 3, 4, 5, })));

        IServiceProvider services = new CombinedServiceProvider(stringServices, numberServices);

        switch (services.GetService<IEnumerable<int>>())
        {
            case null:
                throw new Exception("Could not locate service!");

            case var target:
                Assert.Equal(15, target.Sum());
                break;
        }
    }

    private interface IService;

    private sealed class ServiceA : IService;

    private sealed class ServiceB : IService;

    private sealed class ServiceC : IService;
}
