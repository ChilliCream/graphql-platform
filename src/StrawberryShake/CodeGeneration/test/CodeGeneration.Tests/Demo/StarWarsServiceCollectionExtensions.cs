using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake;
using StrawberryShake.Serialization;
using StrawberryShake.Transport.Http;

namespace Foo
{
    public static class StarWarsServiceCollectionExtensions
    {
        public static IServiceCollection AddStarWarsClient(
            IServiceCollection services,
            ExecutionStrategy strategy = ExecutionStrategy.NetworkOnly)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // register stores
            services.TryAddSingleton<IEntityStore, EntityStore>();
            services.TryAddSingleton<IOperationStore>(
                sp => new OperationStore(sp.GetRequiredService<IEntityStore>().Watch()));

            // register connections
            services.AddSingleton(sp =>
            {
                var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new HttpConnection(() => clientFactory.CreateClient("Foo"));
            });

            // register mappers
            services.AddSingleton<IEntityMapper<DroidEntity, GetHero_Hero_Droid>, GetHero_Hero_DroidFromDroidEntityMapper>();
            services.AddSingleton<IEntityMapper<HumanEntity, GetHero_Hero_Human>, GetHero_Hero_HumanFromHumanEntityMapper>();

            // register serializers
            services.AddSingleton<ISerializer, StringSerializer>();
            services.AddSingleton<ISerializer, EpisodeParser>();
            services.AddSingleton<ISerializerResolver, SerializerResolver>();

            // register operations
            services.AddSingleton<IOperationResultDataFactory<GetHero>, GetHeroFactory>();
            services.AddSingleton<IOperationResultBuilder<JsonDocument, IGetHero>, GetHeroBuilder>();
            services.AddSingleton<IOperationExecutor<IGetHero>>(
                sp => new OperationExecutor<JsonDocument, IGetHero>(
                    sp.GetRequiredService<IConnection<JsonDocument>>(),
                    () => sp.GetRequiredService<IOperationResultBuilder<JsonDocument, IGetHero>>(),
                    sp.GetRequiredService<IOperationStore>(),
                    strategy));
            services.AddSingleton<GetHeroQuery>();

            // register client
            services.AddSingleton<FooClient>();

            return services;
        }
    }
}