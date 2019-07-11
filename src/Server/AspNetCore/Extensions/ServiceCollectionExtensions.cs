using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;
using HotChocolate.Configuration;
using HotChocolate.Server;
#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Interceptors;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Interceptors;
using Microsoft.AspNetCore.Http;
#endif

namespace HotChocolate
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection);
            return serviceCollection.AddSchema(schema);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema,
            Func<IQueryExecutionBuilder, IQueryExecutionBuilder> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(QueryExecutionBuilder.New()).Build(serviceCollection);
            return serviceCollection.AddSchema(schema);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection);
            return serviceCollection.AddSchema(schemaFactory);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory,
            Func<IQueryExecutionBuilder, IQueryExecutionBuilder> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(QueryExecutionBuilder.New()).Build(serviceCollection);
            return serviceCollection.AddSchema(schemaFactory);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection);
            return serviceCollection.AddSchema(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure,
            Func<IQueryExecutionBuilder, IQueryExecutionBuilder>
                configureBuilder)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (configureBuilder == null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }

            configureBuilder(QueryExecutionBuilder.New())
                .Build(serviceCollection);
            return serviceCollection.AddSchema(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            return serviceCollection.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure,
            Func<IQueryExecutionBuilder, IQueryExecutionBuilder>
                configureBuilder)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (configureBuilder == null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }

            configureBuilder(QueryExecutionBuilder.New())
                .Build(serviceCollection);

            return serviceCollection.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection, options);
            return serviceCollection.AddSchema(schema);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection, options);
            return serviceCollection.AddSchema(schemaFactory);
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection, options);

            return serviceCollection.AddSchema(s =>
                Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(serviceCollection, options);

            return serviceCollection.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }));
        }

        [Obsolete("Use different overload.", true)]
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            IQueryExecutor executor)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            return serviceCollection
                .AddSingleton(executor)
                .AddSingleton(s =>
                    s.GetRequiredService<IQueryExecutor>().Schema)
                .AddJsonSerializer();
        }

#if !ASPNETCLASSIC

        public static IServiceCollection AddWebSocketConnectionInterceptor(
            this IServiceCollection serviceCollection,
            OnConnectWebSocketAsync interceptor)
        {
            return serviceCollection
                .AddSingleton<ISocketConnectionInterceptor<HttpContext>>(
                    new SocketConnectionDelegateInterceptor(interceptor));
        }
#endif

        public static IServiceCollection AddQueryRequestInterceptor(
            this IServiceCollection serviceCollection,
            OnCreateRequestAsync interceptor)
        {
            return serviceCollection
                .AddSingleton<IQueryRequestInterceptor<HttpContext>>(
                    new QueryRequestDelegateInterceptor(interceptor));
        }

        private static IServiceCollection AddSchema(
            this IServiceCollection serviceCollection,
            ISchema schema)
        {
            return AddSchema(serviceCollection, sp => schema);
        }

        private static IServiceCollection AddSchema(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> factory)
        {
            serviceCollection.AddSingleton(factory);
            serviceCollection.AddJsonSerializer();
#if !ASPNETCLASSIC
            serviceCollection.AddGraphQLSubscriptions();
#endif
            return serviceCollection;
        }

        private static IServiceCollection AddJsonSerializer(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IQueryResultSerializer>(
                new JsonQueryResultSerializer());
        }
    }
}
