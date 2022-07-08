using HotChocolate.AspNetCore.Serialization;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public static void AddHttpRequestSerializer_OfT()
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        // act
        serviceCollection.AddHttpResultSerializer<DefaultHttpResultSerializer>();

        // assert
        Assert.Collection(
            serviceCollection,
            t =>
            {
                Assert.Equal(typeof(IHttpResultSerializer), t.ServiceType);
                Assert.Equal(typeof(DefaultHttpResultSerializer), t.ImplementationType);
            });
    }
}
