using System.Linq;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionExtensionTests
    {
        [Fact]
        public static void AddHttpRequestSerializer_OfT()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // act
            HotChocolateAspNetCoreServiceCollectionExtensions
                .AddHttpRequestSerializer<DefaultHttpResultSerializer>(serviceCollection);

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
}
