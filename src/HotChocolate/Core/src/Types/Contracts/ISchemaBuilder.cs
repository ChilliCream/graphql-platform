using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

#nullable enable

namespace HotChocolate;

/// <summary>
/// The schema builder provides a configuration API to create a GraphQL schema.
/// </summary>
public interface ISchemaBuilder : IFeatureProvider
{
    ISchemaBuilder SetSchema(Type type);

    ISchemaBuilder SetSchema(Schema schema);

    ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure);

    ISchemaBuilder ModifyOptions(Action<SchemaOptions> configure);

    ISchemaBuilder Use(FieldMiddleware middleware);

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
    ISchemaBuilder AddType(ITypeDefinition namedType);

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
    ISchemaBuilder AddType(ITypeDefinitionExtension typeExtension);

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

    ISchemaBuilder TryAddTypeInterceptor(Type interceptor);

    ISchemaBuilder TryAddTypeInterceptor(TypeInterceptor interceptor);

    /// <summary>
    /// Creates a new GraphQL Schema.
    /// </summary>
    /// <returns>
    /// Returns a new GraphQL Schema.
    /// </returns>
    Schema Create();

    /// <summary>
    /// Creates a new GraphQL Schema.
    /// </summary>
    /// <returns>
    /// Returns a new GraphQL Schema.
    /// </returns>
    Schema Create(IDescriptorContext context);

    IDescriptorContext CreateContext();
}
