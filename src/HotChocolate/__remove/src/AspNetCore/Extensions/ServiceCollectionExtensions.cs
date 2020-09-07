using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;
using HotChocolate.Configuration;
using HotChocolate.Server;
using HotChocolate.Execution.Batching;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Interceptors;
using HotChocolate.Types.Relay;

namespace HotChocolate
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchemaBuilder schemaBuilder)
        {
            return services
                .AddGraphQLSchema(schemaBuilder)
                .AddGraphQLSubscriptions()
                .AddJsonSerializer()
                .AddQueryExecutor()
                .AddBatchQueryExecutor();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchema schema)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            QueryExecutionBuilder.BuildDefault(services);
            return services.AddSchema(schema)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchema schema,
            Action<IQueryExecutionBuilder> build)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            QueryExecutionBuilder builder = QueryExecutionBuilder.New();
            build(builder);
            builder.Populate(services);

            return services.AddSchema(schema)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Func<IServiceProvider, ISchema> schemaFactory)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            QueryExecutionBuilder.BuildDefault(services);
            return services.AddSchema(schemaFactory)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Func<IServiceProvider, ISchema> schemaFactory,
            Action<IQueryExecutionBuilder> build)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            QueryExecutionBuilder builder = QueryExecutionBuilder.New();
            build(builder);
            builder.Populate(services);

            return services.AddSchema(schemaFactory)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Action<ISchemaConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            QueryExecutionBuilder.BuildDefault(services);
            return services.AddSchema(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Action<ISchemaConfiguration> configure,
            Action<IQueryExecutionBuilder> build)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            QueryExecutionBuilder builder = QueryExecutionBuilder.New();
            build(builder);
            builder.Populate(services);

            return services.AddSchema(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            QueryExecutionBuilder.BuildDefault(services);

            return services.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            string schemaSource,
            Action<ISchemaConfiguration> configure,
            Action<IQueryExecutionBuilder> build)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            QueryExecutionBuilder builder = QueryExecutionBuilder.New();
            build(builder);
            builder.Populate(services);

            return services.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchema schema,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(services, options);
            return services.AddSchema(schema)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Func<IServiceProvider, ISchema> schemaFactory,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(services, options);
            return services.AddSchema(schemaFactory)
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryExecutionBuilder.BuildDefault(services, options);

            return services.AddSchema(s =>
                Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            string schemaSource,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
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

            QueryExecutionBuilder.BuildDefault(services, options);

            return services.AddSchema(s =>
                Schema.Create(schemaSource, c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        [Obsolete("Use different overload.", true)]
        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            IQueryExecutor executor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            return services
                .AddSingleton(executor)
                .AddSingleton(s =>
                    s.GetRequiredService<IQueryExecutor>().Schema)
                .AddJsonSerializer();
        }

        public static IServiceCollection AddWebSocketConnectionInterceptor(
            this IServiceCollection services,
            OnConnectWebSocketAsync interceptor)
        {
            return services
                .AddSingleton<ISocketConnectionInterceptor<HttpContext>>(
                    new SocketConnectionDelegateInterceptor(interceptor));
        }

        public static IServiceCollection AddQueryRequestInterceptor(
            this IServiceCollection services,
            OnCreateRequestAsync interceptor)
        {
            return services
                .AddSingleton<IQueryRequestInterceptor<HttpContext>>(
                    new QueryRequestDelegateInterceptor(interceptor));
        }

        private static IServiceCollection AddSchema(
            this IServiceCollection services,
            ISchema schema)
        {
            return AddSchema(services, sp => schema);
        }

        private static IServiceCollection AddSchema(
            this IServiceCollection services,
            Func<IServiceProvider, ISchema> factory)
        {
            return services
                .AddSingleton<IIdSerializer, IdSerializer>()
                .AddSingleton(factory)
                .AddJsonSerializer()
                .AddGraphQLSubscriptions();
        }

        private static IServiceCollection AddJsonSerializer(
            this IServiceCollection services)
        {
            return services
                .AddJsonQueryResultSerializer()
                .AddJsonArrayResponseStreamSerializer();
        }
    }
}
