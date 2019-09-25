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
            //services.AddSingleton<IValueSerializer, EpisodeValueSerializer>();
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

            IOperationResult<IGetHero> result = await client.GetHeroAsync();
        }

        public class StringValueSerializer
            : IValueSerializer
        {
            public string Name => "String";

            public ValueKind Kind => ValueKind.String;

            public Type ClrType => typeof(string);

            public Type SerializationType => typeof(string);

            public object? Serialize(object? value)
            {
                return value;
            }

            public object? Deserialize(object? serialized)
            {
                return serialized;
            }
        }

        public class FloatValueSerializer
            : IValueSerializer
        {
            public string Name => "Float";

            public ValueKind Kind => ValueKind.Float;

            public Type ClrType => typeof(double);

            public Type SerializationType => typeof(double);

            public object? Serialize(object? value)
            {
                return value;
            }

            public object? Deserialize(object? serialized)
            {
                return serialized;
            }
        }
    }
}
