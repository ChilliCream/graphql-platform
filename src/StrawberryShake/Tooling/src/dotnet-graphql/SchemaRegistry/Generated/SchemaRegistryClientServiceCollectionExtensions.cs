using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Pipelines;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Serializers;
using StrawberryShake.Transport;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class SchemaRegistryClientServiceCollectionExtensions
    {
        private const string _clientName = "SchemaRegistryClient";

        public static IOperationClientBuilder AddSchemaRegistryClient(
            this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddSingleton<ISchemaRegistryClient, SchemaRegistryClient>();

            serviceCollection.AddSingleton<IOperationExecutorFactory>(sp =>
                new HttpOperationExecutorFactory(
                    _clientName,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient,
                    sp.GetRequiredService<IClientOptions>().GetOperationPipeline<IHttpOperationContext>(_clientName),
                    sp.GetRequiredService<IClientOptions>().GetOperationFormatter(_clientName),
                    sp.GetRequiredService<IClientOptions>().GetResultParsers(_clientName)));

            serviceCollection.AddSingleton<IOperationStreamExecutorFactory>(sp =>
                new SocketOperationStreamExecutorFactory(
                    _clientName,
                    sp.GetRequiredService<ISocketConnectionPool>().RentAsync,
                    sp.GetRequiredService<ISubscriptionManager>(),
                    sp.GetRequiredService<IClientOptions>().GetOperationFormatter(_clientName),
                    sp.GetRequiredService<IClientOptions>().GetResultParsers(_clientName)));

            IOperationClientBuilder builder = serviceCollection.AddOperationClientOptions(_clientName)
                .AddValueSerializer(() => new QueryFileFormatValueSerializer())
                .AddValueSerializer(() => new IssueTypeValueSerializer())
                .AddValueSerializer(() => new ResolutionTypeValueSerializer())
                .AddValueSerializer(() => new HashFormatValueSerializer())
                .AddValueSerializer(() => new TagInputSerializer())
                .AddValueSerializer(() => new QueryFileInputSerializer())
                .AddResultParser(serializers => new PublishSchemaResultParser(serializers))
                .AddResultParser(serializers => new MarkSchemaPublishedResultParser(serializers))
                .AddResultParser(serializers => new PublishClientResultParser(serializers))
                .AddResultParser(serializers => new MarkClientPublishedResultParser(serializers))
                .AddResultParser(serializers => new OnPublishDocumentResultParser(serializers))
                .AddOperationFormatter(serializers => new JsonOperationFormatter(serializers))
                .AddHttpOperationPipeline(builder => builder.UseHttpDefaultPipeline());

            serviceCollection.TryAddSingleton<ISubscriptionManager, SubscriptionManager>();
            serviceCollection.TryAddSingleton<IOperationExecutorPool, OperationExecutorPool>();
            serviceCollection.TryAddEnumerable(new ServiceDescriptor(
                typeof(ISocketConnectionInterceptor),
                typeof(MessagePipelineHandler),
                ServiceLifetime.Singleton));
            return builder;
        }

    }
}
