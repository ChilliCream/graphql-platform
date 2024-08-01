using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

#nullable enable

namespace HotChocolate;

public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

public delegate IConvention CreateConvention(IServiceProvider services);

/// <summary>
/// The schema builder provides a configuration API to create a GraphQL schema.
/// </summary>
public interface ISchemaBuilder
{
    /// <summary>
    /// Gets direct access to the schema building context data.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }

    ISchemaBuilder SetSchema(Type type);

    ISchemaBuilder SetSchema(ISchema schema);

    ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure);

    [Obsolete("Use ModifyOptions instead.")]
    ISchemaBuilder SetOptions(IReadOnlySchemaOptions options);

    ISchemaBuilder ModifyOptions(Action<SchemaOptions> configure);

    [Obsolete("Use ModifyPagingOptions instead.")]
    ISchemaBuilder SetPagingOptions(PagingOptions options);

    ISchemaBuilder ModifyPagingOptions(Action<PagingOptions> configure);

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

    /// <summary>
    /// Binds a .NET runtime type to a GraphQL schema type.
    /// </summary>
    /// <param name="runtimeType">
    /// The .NET runtime type.
    /// </param>
    /// <param name="schemaType">
    /// The GraphQL schema type.
    /// </param>
    /// <returns></returns>
    ISchemaBuilder BindRuntimeType(Type runtimeType, Type schemaType);

    /// <summary>
    /// Add a GraphQL root type to the schema.
    /// </summary>
    /// <param name="rootType">
    /// A type representing a GraphQL root type.
    /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
    /// </param>
    /// <param name="operation">
    /// The operation type that <paramref name="rootType"/> represents.
    /// </param>
    /// <returns>
    /// Returns the schema builder to chain in further configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="rootType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="rootType"/> is either not a class or is not inheriting from
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
    /// The operation type that <paramref name="rootType"/> represents.
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

    /// <summary>
    /// Tries to add a GraphQL root type to the schema.
    /// </summary>
    /// <param name="rootType">
    /// An instance of <see cref="ObjectType"/> that represents a root type.
    /// </param>
    /// <param name="operation">
    /// The operation type that <paramref name="rootType"/> represents.
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
    ISchemaBuilder TryAddRootType(Func<ObjectType> rootType, OperationType operation);

    ISchemaBuilder AddDirectiveType(DirectiveType type);

    ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType);

    ISchemaBuilder AddServices(IServiceProvider services);

    ISchemaBuilder SetContextData(string key, object? value);

    ISchemaBuilder SetContextData(string key, Func<object?, object?> update);

    ISchemaBuilder TryAddTypeInterceptor(Type interceptor);

    ISchemaBuilder TryAddTypeInterceptor(TypeInterceptor interceptor);

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
