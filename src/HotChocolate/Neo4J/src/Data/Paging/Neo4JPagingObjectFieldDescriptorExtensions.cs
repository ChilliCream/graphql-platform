using System;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Neo4J.Paging
{
    public static class Neo4JPagingObjectFieldDescriptorExtensions
    {
        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JPaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseNeo4JPaging<TSchemaType>(descriptor, typeof(TEntity), options);

        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseNeo4JPaging(descriptor, typeof(TSchemaType), entityType, options);

        /// <summary>
        /// Adds cursor pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="type">
        /// The schema type of the entity. Not a connection type
        /// </param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JPaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            PagingOptions options = default) =>
            descriptor.UsePaging(
                type,
                entityType,
                (services, sourceType) => services.GetService<Neo4JCursorPagingProvider>() ??
                    new Neo4JCursorPagingProvider(),
                options);

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="type">
        /// The schema type of the entity. Not a connection type
        /// </param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JOffsetPaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            PagingOptions options = default) =>
            descriptor.UseOffsetPaging(
                type,
                entityType,
                (services, sourceType) => services.GetService<Neo4JOffsetPagingProvider>() ??
                    new Neo4JOffsetPagingProvider(),
                options);

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="entityType">The type of the entity</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JOffsetPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            PagingOptions options = default)
            where TSchemaType : IOutputType =>
            UseNeo4JOffsetPaging(
                descriptor,
                typeof(TSchemaType),
                entityType,
                options);

        /// <summary>
        /// Adds offset pagination support to the field. Rewrites the type to a connection type and
        /// registers the neo4j pagination handler
        /// </summary>
        /// <param name="descriptor">The descriptor of the field</param>
        /// <param name="options">The options for pagination</param>
        /// <typeparam name="TSchemaType">
        /// The schema type of the entity. Not a connection type
        /// </typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IObjectFieldDescriptor UseNeo4JOffsetPaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UseNeo4JOffsetPaging<TSchemaType>(descriptor, typeof(TEntity), options);
    }
}
