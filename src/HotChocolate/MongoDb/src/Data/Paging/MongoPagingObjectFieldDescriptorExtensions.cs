using System;
using HotChocolate.Data.MongoDb.Paging;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types
{
    public static class MongoPagingObjectFieldDescriptorExtensions
    {
        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseMongoPaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseMongoPaging<TSchemaType>(descriptor, typeof(TEntity), options);

        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseMongoPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseMongoPaging(descriptor, typeof(TSchemaType), entityType, options);

        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="type">
        /// The schema type of the entity. Not a connection type
        /// </param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <returns>The <paramref name="descriptor"/></returns>
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

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="type">
        /// The schema type of the entity. Not a connection type
        /// </param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <returns>The <paramref name="descriptor"/></returns>
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

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseMongoOffsetPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            PagingOptions options = default)
            where TSchemaType : IOutputType =>
            UseMongoOffsetPaging(
                descriptor,
                typeof(TSchemaType),
                entityType,
                options);

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the mongo pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseMongoOffsetPaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseMongoOffsetPaging<TSchemaType>(descriptor, typeof(TEntity), options);
    }
}
