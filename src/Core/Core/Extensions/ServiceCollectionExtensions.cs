using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Batching;
using System.Linq;

namespace HotChocolate
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLSchema(
            this IServiceCollection services,
            ISchemaBuilder schemaBuilder)
        {
            return services.AddSingleton<ISchema>(sp =>
                schemaBuilder
                    .AddServices(sp)
                    .Create());
        }

        public static IServiceCollection AddQueryExecutor(
            this IServiceCollection services)
        {
            QueryExecutionBuilder.BuildDefault(services);
            return services;
        }

        public static IServiceCollection AddQueryExecutor(
            this IServiceCollection services,
            IQueryExecutionOptionsAccessor options)
        {
            QueryExecutionBuilder.BuildDefault(services, options);
            return services;
        }

        public static IServiceCollection AddQueryExecutor(
            this IServiceCollection services,
            Action<IQueryExecutionBuilder> configure)
        {
            var builder = QueryExecutionBuilder.New();
            configure(builder);
            builder.Populate(services);
            return services;
        }

        public static IServiceCollection AddQueryExecutor(
            this IServiceCollection services,
            Action<IQueryExecutionBuilder> configure,
            bool lazyExecutor)
        {
            var builder = QueryExecutionBuilder.New();
            configure(builder);
            builder.Populate(services, lazyExecutor);
            return services;
        }

        public static IServiceCollection AddBatchQueryExecutor(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();
        }

        public static IServiceCollection AddJsonQueryResultSerializer(
            this IServiceCollection services)
        {
            return services
                .AddQueryResultSerializer<JsonQueryResultSerializer>();
        }

        public static IServiceCollection AddQueryResultSerializer<T>(
            this IServiceCollection services)
            where T : class, IQueryResultSerializer
        {
            return services
                .RemoveService<IQueryResultSerializer>()
                .AddSingleton<IQueryResultSerializer, T>();
        }

        public static IServiceCollection AddQueryResultSerializer<T>(
            this IServiceCollection services,
            Func<IServiceProvider, T> factory)
            where T : IQueryResultSerializer
        {
            return services
                .RemoveService<IQueryResultSerializer>()
                .AddSingleton<IQueryResultSerializer>(sp => factory(sp));
        }

        public static IServiceCollection AddJsonArrayResponseStreamSerializer(
            this IServiceCollection services)
        {
            return services
            .AddResponseStreamSerializer<JsonArrayResponseStreamSerializer>();
        }

        public static IServiceCollection AddResponseStreamSerializer<T>(
            this IServiceCollection services)
            where T : class, IResponseStreamSerializer
        {
            return services
                .RemoveService<IResponseStreamSerializer>()
                .AddSingleton<IResponseStreamSerializer, T>();
        }

        public static IServiceCollection AddResponseStreamSerializer<T>(
            this IServiceCollection services,
            Func<IServiceProvider, T> factory)
            where T : IResponseStreamSerializer
        {
            return services
                .RemoveService<IResponseStreamSerializer>()
                .AddSingleton<IResponseStreamSerializer>(sp => factory(sp));
        }

        private static IServiceCollection RemoveService<TService>(
            this IServiceCollection services)
        {
            foreach (var serviceDescriptor in services.Where(t =>
                t.ServiceType == typeof(TService)).ToArray())
            {
                services.Remove(serviceDescriptor);
            }
            return services;
        }
    }
}
