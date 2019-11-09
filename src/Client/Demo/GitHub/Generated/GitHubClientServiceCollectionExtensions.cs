using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake;
using StrawberryShake.Http;
using StrawberryShake.Http.Pipelines;
using StrawberryShake.Serializers;

namespace StrawberryShake.Client.GitHub
{
    public static class GitHubClientServiceCollectionExtensions
    {
        public static IServiceCollection AddGitHubClient(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddSingleton<IGitHubClient, GitHubClient>();
            serviceCollection.AddSingleton(sp =>
                HttpOperationExecutorBuilder.New()
                    .AddServices(sp)
                    .SetClient(ClientFactory)
                    .SetPipeline(PipelineFactory)
                    .Build());

            serviceCollection.AddDefaultScalarSerializers();
            serviceCollection.AddResultParsers();

            serviceCollection.TryAddDefaultOperationSerializer();
            serviceCollection.TryAddDefaultHttpPipeline();

            return serviceCollection;
        }

        public static IServiceCollection AddDefaultScalarSerializers(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddSingleton<IValueSerializerResolver, ValueSerializerResolver>();

            foreach (IValueSerializer serializer in ValueSerializers.All)
            {
                serviceCollection.AddSingleton(serializer);
            }

            return serviceCollection;
        }



        private static IServiceCollection AddResultParsers(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IResultParserResolver, ResultParserResolver>();
            serviceCollection.AddSingleton<IResultParser, GetUserResultParser>();
            return serviceCollection;
        }

        private static IServiceCollection TryAddDefaultOperationSerializer(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IOperationFormatter, JsonOperationFormatter>();
            return serviceCollection;
        }

        private static IServiceCollection TryAddDefaultHttpPipeline(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<OperationDelegate>(
                sp => HttpPipelineBuilder.New()
                    .Use<CreateStandardRequestMiddleware>()
                    .Use<SendHttpRequestMiddleware>()
                    .Use<ParseSingleResultMiddleware>()
                    .Build(sp));
            return serviceCollection;
        }

        private static Func<HttpClient> ClientFactory(IServiceProvider services)
        {
            var clientFactory = services.GetRequiredService<IHttpClientFactory>();
            return () => clientFactory.CreateClient("GitHubClient");
        }

        private static OperationDelegate PipelineFactory(IServiceProvider services)
        {
            return services.GetRequiredService<OperationDelegate>();
        }
    }
}
