using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;

namespace HotChocolate
{
    public delegate IQueryExecutor BuildExecutor(
        IServiceProvider services,
        IQueryExecutionBuilder builder);

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return serviceCollection
                .AddSingleton(schema)
                .AddSingleton(schema.MakeExecutable())
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory)
        {
            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            return serviceCollection
                .AddSingleton(schemaFactory)
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable())
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable())
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            if (string.IsNullOrEmpty(schemaSource))
            {
                throw new ArgumentNullException(nameof(schemaSource));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(
                    schemaSource, c =>
                    {
                        c.RegisterServiceProvider(s);
                        configure(c);
                    }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable())
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            ISchema schema,
            IQueryExecutionOptionsAccessor options)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return serviceCollection
                .AddSingleton(schema)
                .AddSingleton(schema.MakeExecutable(options))
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, ISchema> schemaFactory,
            IQueryExecutionOptionsAccessor options)
        {
            if (schemaFactory == null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return serviceCollection
                .AddSingleton(schemaFactory)
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable(options))
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(c =>
                {
                    c.RegisterServiceProvider(s);
                    configure(c);
                }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable(options))
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
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

            return serviceCollection
                .AddSingleton<ISchema>(s => Schema.Create(
                    schemaSource, c =>
                    {
                        c.RegisterServiceProvider(s);
                        configure(c);
                    }))
                .AddSingleton(sp => sp.GetRequiredService<ISchema>()
                    .MakeExecutable(options))
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            IQueryExecutor executor)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            return serviceCollection
                .AddSingleton(executor)
                .AddSingleton<ISchema>(s =>
                    s.GetRequiredService<IQueryExecutor>().Schema)
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            BuildExecutor buildExecutor)
        {
            if (buildExecutor == null)
            {
                throw new ArgumentNullException(nameof(buildExecutor));
            }

            return serviceCollection
                .AddSingleton<IQueryExecutor>(s =>
                    buildExecutor(s, QueryExecutionBuilder.New()))
                .AddSingleton(s =>
                    s.GetRequiredService<IQueryExecutor>().Schema)
                .AddJsonSerializer();
        }

        private static IServiceCollection AddJsonSerializer(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IQueryResultSerializer>(
                new JsonQueryResultSerializer());
        }
    }
}
