using System;
using HotChocolate.Data.MongoDb.Paging;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types
{
    public static class MongoPagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseMongoPaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseMongoPaging<TSchemaType>(descriptor, typeof(TEntity), options);

        public static IObjectFieldDescriptor UseMongoPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseMongoPaging(descriptor, typeof(TSchemaType), entityType, options);

        public static IObjectFieldDescriptor UseMongoPaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            PagingOptions options = default) =>
            descriptor.UsePaging(
                type,
                entityType,
                (services, sourceType) => services.GetService<MongoCursorPagingProvider>() ??
                    new MongoCursorPagingProvider(),
                options);

        public static IObjectFieldDescriptor UseMongoOffsetPaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            PagingOptions options = default) =>
            descriptor.UseOffsetPaging(
                type,
                entityType,
                (services, sourceType) => services.GetService<MongoOffsetPagingProvider>() ??
                    new MongoOffsetPagingProvider(),
                options);

        public static IObjectFieldDescriptor UseMongoOffsetPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? itemType = null,
            PagingOptions options = default)
            where TSchemaType : IOutputType =>
            UseMongoOffsetPaging(
                descriptor,
                typeof(TSchemaType),
                itemType,
                options);
    }
}
