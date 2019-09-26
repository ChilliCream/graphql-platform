using System.Net;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using StrawberryShake;
using StrawberryShake.Client;
using StrawberryShake.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Http.Pipelines;
using Xunit;
using StrawberryShake.Serializers;

namespace Demo.Tests
{
    public class Class1
    {
        [Fact]
        public async Task Foo()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IValueSerializer, StringValueSerializer>();
            services.AddSingleton<IValueSerializer, FloatValueSerializer>();
            services.AddSingleton<IValueSerializer, EpisodeValueSerializer>();
            services.AddSingleton<IResultParser, GetHeroResultParser>();
            services.AddSingleton<IOperationSerializer, JsonOperationSerializer>();
            services.AddSingleton<IStarWarsClient, StarWarsClient>();
            services.AddSingleton<HttpClient>(sp => new HttpClient() { BaseAddress = new Uri("http://localhost:5000/graphql") });
            services.AddSingleton(sp =>
                HttpOperationExecutorBuilder.New()
                    .AddServices(sp)
                    .SetClient(sp.GetRequiredService<HttpClient>())
                    .Use<CreateStandardRequestMiddleware>()
                    .Use<SendHttpRequestMiddleware>()
                    .Use<ParseSingleResultMiddleware>()
                    .Build());
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IStarWarsClient client = serviceProvider.GetService<IStarWarsClient>();

            IOperationResult<IGetHero> result = await client.GetHeroAsync(Episode.Empire);
        }


    }
}
