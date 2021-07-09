using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public delegate IConvention CreateConvention(IServiceProvider services);

    public interface ISchemaBuilder
    {
        IDictionary<string, object?> ContextData { get; }

        ISchemaBuilder SetSchema(Type type);

        ISchemaBuilder SetSchema(ISchema schema);

        ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure);

        ISchemaBuilder SetOptions(IReadOnlySchemaOptions options);

        ISchemaBuilder ModifyOptions(Action<ISchemaOptions> configure);

        ISchemaBuilder Use(FieldMiddleware middleware);

        ISchemaBuilder AddDocument(LoadSchemaDocument loadDocument);

        /// <summary>
        /// Adds a GraphQL type to the schema.
        /// </summary>
        /// <param name="type">
        /// The GraphQL type.
        /// </param>
        /// <returns>
        /// Returns the schema builder to chain in further configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> is <c>null</c>
        /// </exception>
        ISchemaBuilder AddType(Type type);

        /// <summary>
        /// Adds a GraphQL type to the schema.
        /// </summary>
        /// <param name="namedType">
        /// The GraphQL type.
        /// </param>
        /// <returns>
        /// Returns the schema builder to chain in further configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namedType"/> is <c>null</c>
        /// </exception>
        ISchemaBuilder AddType(INamedType namedType);

        /// <summary>
        /// Adds a GraphQL type extension to the schema.
        /// </summary>
        /// <param name="typeExtension">
        /// The GraphQL type extension.
        /// </param>
        /// <returns>
        /// Returns the schema builder to chain in further configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeExtension"/> is <c>null</c>
        /// </exception>
        ISchemaBuilder AddType(INamedTypeExtension typeExtension);

        [Obsolete("Use BindRuntimeType")]
        ISchemaBuilder BindClrType(Type clrType, Type schemaType);

        ISchemaBuilder BindRuntimeType(Type runtimeType, Type schemaType);

        /// <summary>
        /// Add a GraphQL root type to the schema.
        /// </summary>
        /// <param name="rootType">
        /// A type representing a GraphQL root type.
        /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
        /// </param>
        /// <param name="operation">
        /// The operation type that <see cref="rootType"/> represents.
        /// </param>
        /// <returns>
        /// Returns the schema builder to chain in further configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="rootType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// - <see cref="rootType"/> is either not a class or is not inheriting from
        /// <see cref="ObjectType{T}"/>.
        ///
        /// - A root type for the specified <paramref name="operation"/> was already set.
        /// </exception>
        ISchemaBuilder AddRootType(Type rootType, OperationType operation);

        /// <summary>
        /// Add a GraphQL root type to the schema.
        /// </summary>
        /// <param name="rootType">
        /// An instance of <see cref="ObjectType"/> that represents a root type.
        /// </param>
        /// <param name="operation">
        /// The operation type that <see cref="rootType"/> represents.
        /// </param>
        /// <returns>
        /// Returns the schema builder to chain in further configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="rootType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// A root type for the specified <paramref name="operation"/> was already set.
        /// </exception>
        ISchemaBuilder AddRootType(ObjectType rootType, OperationType operation);

        ISchemaBuilder AddDirectiveType(DirectiveType type);

        ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType);

        ISchemaBuilder AddServices(IServiceProvider services);

        ISchemaBuilder SetContextData(string key, object? value);

        ISchemaBuilder SetContextData(string key, Func<object?, object?> update);

        ISchemaBuilder TryAddTypeInterceptor(Type interceptor);

        ISchemaBuilder TryAddTypeInterceptor(ITypeInitializationInterceptor interceptor);

        ISchemaBuilder TryAddSchemaInterceptor(Type interceptor);

        ISchemaBuilder TryAddSchemaInterceptor(ISchemaInterceptor interceptor);

        ISchemaBuilder AddConvention(
            Type convention,
            CreateConvention factory,
            string? scope = null);

        ISchemaBuilder TryAddConvention(
            Type convention,
            CreateConvention factory,
            string? scope = null);

        /// <summary>
        /// Creates a new GraphQL Schema.
        /// </summary>
        /// <returns>
        /// Returns a new GraphQL Schema.
        /// </returns>
        ISchema Create();

        /// <summary>
        /// Creates a new GraphQL Schema.
        /// </summary>
        /// <returns>
        /// Returns a new GraphQL Schema.
        /// </returns>
        ISchema Create(IDescriptorContext context);

        IDescriptorContext CreateContext();
    }
}
