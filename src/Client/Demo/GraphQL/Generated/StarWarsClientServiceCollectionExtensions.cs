using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using StrawberryShake.Serializers;

namespace StrawberryShake.Client
{
    public static class StarWarsClientServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultValueSerializers(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            foreach (IValueSerializer serializer in ValueSerializers.All)
            {
                serviceCollection.AddSingleton(serializer);
            }

            return serviceCollection;
        }

        public static IServiceCollection AddStarWarsClient(
            this IServiceCollection serviceCollection)
        {
            services.AddSingleton(sp =>
                HttpOperationExecutorBuilder.New()
                    .AddServices(sp)
                    .SetClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(""))
                    .Use<CreateStandardRequestMiddleware>()
                    .Use<SendHttpRequestMiddleware>()
                    .Use<ParseSingleResultMiddleware>()
                    .Build());
        }
    }
}
