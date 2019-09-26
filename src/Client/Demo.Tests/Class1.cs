using System.Net;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using StrawberryShake;
using StrawberryShake.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Http.Pipelines;
using Xunit;
using StrawberryShake.Serializers;
using StrawberryShake.Client;

namespace Demo.Tests
{
    public class Class1
    {
        [Fact]
        public async Task Foo()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient("StarWarsClient");
            serviceCollection.AddDefaultScalarSerializers();
            serviceCollection.AddStarWarsClient();

            var services = serviceCollection.BuildServiceProvider();

            IStarWarsClient client = services.GetService<IStarWarsClient>();
            IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Empire);

            Console.WriteLine(result.Data.Hero.Name);
        }


    }
}
