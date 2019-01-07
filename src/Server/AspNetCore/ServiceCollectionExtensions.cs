using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;

#if ASPNETCLASSIC
using HotChocolate.AspNetClassic;
#else
using HotChocolate.AspNetCore;
#endif

public delegate IQueryExecuter BuildExecuter(
    IServiceProvider services,
    IQueryExecutionBuilder builder);

namespace HotChocolate
{
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
            IQueryExecuter executer)
        {
            if (executer == null)
            {
                throw new ArgumentNullException(nameof(executer));
            }

            return serviceCollection
                .AddSingleton(executer)
                .AddSingleton<ISchema>(s =>
                    s.GetRequiredService<IQueryExecuter>().Schema)
                .AddJsonSerializer();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            BuildExecuter buildExecuter)
        {
            if (buildExecuter == null)
            {
                throw new ArgumentNullException(nameof(buildExecuter));
            }

            return serviceCollection
                .AddSingleton<IQueryExecuter>(s =>
                    buildExecuter(s, QueryExecutionBuilder.New()))
                .AddSingleton(s =>
                    s.GetRequiredService<IQueryExecuter>().Schema)
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
