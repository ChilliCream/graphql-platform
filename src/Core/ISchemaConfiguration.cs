using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchemaConfiguration
    {
        ISchemaConfiguration Resolver(string typeName, string fieldName, AsyncFieldResolverDelegate fieldResolver);
        ISchemaConfiguration Resolver<TResolver, TObjectType>();

        ISchemaConfiguration Name<TObjectType>(string typeName);
        ISchemaConfiguration Name<TObjectType>(string typeName,
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);
        ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);

        ISchemaConfiguration Register<T>(T type)
            where T : INamedType;
    }

    public interface ISchemaConfiguration2
    {
        /// <summary>
        /// Registers a custom scalar type.
        /// </summary>
        /// <param name="type">The custom scalar type object.</param>
        /// <typeparam name="T">The custom scalar type.</typeparam>
        /// <exception cref="ArgumentException">
        /// The specified type was already registered.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> cannot be <c>null</c>.
        /// </exception>
        ISchemaConfiguration2 RegisterScalarType<T>(T type)
           where T : ScalarType;

        /// <summary>
        /// Binds a <see cref="FieldResolverDelegate"/> to field
        /// of an object type defined in the GraphQL schema.
        /// Note: The last binding operation to sepecific field counts.
        /// </summary>
        /// <param name="typeName">
        /// The name of an object type specified in the GraphQL schema.
        /// </param>
        /// <param name="fieldName">
        /// The name of a field of the specified <paramref name="typeName"/>
        /// to which the specified <paramref name="fieldResolver"/>
        /// shall be bound.
        /// </param>
        /// <param name="fieldResolver">
        /// The field resolver.
        /// </param>
        ISchemaConfiguration2 BindResolver(
            string typeName, string fieldName,
            FieldResolverDelegate fieldResolver);

        /// <summary>
        /// Binds a <see cref="AsyncFieldResolverDelegate"/> to field
        /// of an object type defined in the GraphQL schema.
        /// Note: The last binding operation to sepecific field counts.
        /// </summary>
        /// <param name="typeName">
        /// The name of an object type specified in the GraphQL schema.
        /// </param>
        /// <param name="fieldName">
        /// The name of a field of the specified <paramref name="typeName"/>
        /// to which the specified <paramref name="fieldResolver"/>
        /// shall be bound.
        /// </param>
        /// <param name="fieldResolver">
        /// The field resolver.
        /// </param>
        ISchemaConfiguration2 BindResolver(
            string typeName, string fieldName,
            AsyncFieldResolverDelegate fieldResolver);

        /// <summary>
        /// Binds all the public methods and properties that are applicable
        /// to fields of an object type defined in the GraphQL schema.
        /// The relation between the specified <typeparamref name="TResolver"/>
        /// and the object type and its field will inferred from the
        /// type name or if provided from a schema type binding
        /// <seealso cref="ISchemaConfiguration2.BindType{TObjectType}(string)"/>
        /// <seealso cref="ISchemaConfiguration2.BindType{TObjectType}(Action{IFluentFieldMapping{TObjectType}}[])"/>
        /// <seealso cref="ISchemaConfiguration2.BindType{TObjectType}(string, Action{IFluentFieldMapping{TObjectType}}[])"/>
        /// <seealso cref="GraphQLNameAttribute"/>.
        /// </summary>
        /// <typeparam name="TResolver">
        /// A type that provides one or more field resolvers.
        /// </typeparam>
        ISchemaConfiguration2 BindResolver<TResolver>();

        ISchemaConfiguration2 BindResolver<TResolver>(
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping);

        ISchemaConfiguration2 BindResolver<TResolver>(string typeName);

        ISchemaConfiguration2 BindResolver<TResolver>(string typeName,
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping);

        ISchemaConfiguration2 BindResolver<TResolver, TObjectType>();

        ISchemaConfiguration2 BindResolver<TResolver, TObjectType>(
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping);

        // bind "dtos" to input types (input object types, enums) and output types (object type)
        ISchemaConfiguration2 BindType<T>(string typeName);
        ISchemaConfiguration2 BindType<T>(string typeName,
            params Action<IFluentFieldMapping<T>>[] fieldMapping);
        ISchemaConfiguration2 BindType<T>(
            params Action<IFluentFieldMapping<T>>[] fieldMapping);
    }
}
