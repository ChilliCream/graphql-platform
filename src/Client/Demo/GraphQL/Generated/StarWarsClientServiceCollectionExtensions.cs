using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Pipelines;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Serializers;
using StrawberryShake.Transport;

namespace StrawberryShake.Client.GraphQL
{
    public static class StarWarsClientServiceCollectionExtensions
    {
        private const string _clientName = "StarWarsClient";

        public static IOperationClientBuilder AddStarWarsClient(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddSingleton<IStarWarsClient, StarWarsClient>();
            serviceCollection.AddSingleton<IOperationExecutorFactory>(sp =>
                new HttpOperationExecutorFactory(
                    _clientName,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient,
                    PipelineFactory(sp),
                    sp.GetRequiredService<IClientOptions>().GetResultParsers(_clientName)));

            serviceCollection.AddSingleton<IOperationStreamExecutorFactory>(sp =>
                new SocketOperationStreamExecutorFactory(
                    _clientName,
                    sp.GetRequiredService<ISocketConnectionPool>().RentAsync,
                    sp.GetRequiredService<ISubscriptionManager>(),
                    sp.GetRequiredService<IClientOptions>().GetResultParsers(_clientName)));

            IOperationClientBuilder builder = serviceCollection.AddOperationClientOptions(_clientName)
                .AddValueSerializer(() => new EpisodeValueSerializer())
                .AddValueSerializer(() => new ReviewInputSerializer())
                .AddResultParser(serializers => new GetHeroResultParser(serializers))
                .AddResultParser(serializers => new GetHumanResultParser(serializers))
                .AddResultParser(serializers => new SearchResultParser(serializers))
                .AddResultParser(serializers => new CreateReviewResultParser(serializers))
                .AddResultParser(serializers => new OnReviewResultParser(serializers))
                .AddOperationFormmatter(serializers => new JsonOperationFormatter(serializers));

            serviceCollection.TryAddSingleton<ISubscriptionManager, SubscriptionManager>();
            serviceCollection.TryAddSingleton<IOperationExecutorPool, OperationExecutorPool>();
            serviceCollection.TryAddEnumerable(new ServiceDescriptor(
                typeof(ISocketConnectionInterceptor),
                typeof(MessagePipelineHandler),
                ServiceLifetime.Singleton));
            serviceCollection.TryAddDefaultHttpPipeline();

            return builder;
        }

        private static IServiceCollection TryAddDefaultHttpPipeline(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<HttpOperationDelegate>(
                sp => HttpPipelineBuilder.New()
                    .Use<CreateStandardRequestMiddleware>()
                    .Use<SendHttpRequestMiddleware>()
                    .Use<ParseSingleResultMiddleware>()
                    .Build(sp));
            return serviceCollection;
        }

        private static HttpOperationDelegate PipelineFactory(IServiceProvider services)
        {
            return services.GetRequiredService<HttpOperationDelegate>();
        }
    }
}
