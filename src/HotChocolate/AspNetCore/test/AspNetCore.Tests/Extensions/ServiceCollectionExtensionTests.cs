using HotChocolate.AspNetCore.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensionTests
{
    [Fact]
    public static void AddHttpRequestSerializer_OfT()
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        // act
        serviceCollection.AddHttpResponseFormatter<DefaultHttpResponseFormatter>();

        // assert
        Assert.Collection(
            serviceCollection,
            t =>
            {
                Assert.Equal(typeof(IHttpResponseFormatter), t.ServiceType);
                Assert.Equal(typeof(DefaultHttpResponseFormatter), t.ImplementationType);
            });
    }
}
