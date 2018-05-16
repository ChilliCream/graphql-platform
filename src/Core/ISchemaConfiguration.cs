using System;
using HotChocolate.Language;
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

        ISchemaConfiguration2 BindResolver<TResolver, TObjectType>();

        ISchemaConfiguration2 BindResolver<TResolver, TObjectType>(
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping);

        // bind "dtos" to input types (input object types, enums) and output types (object type)
        ISchemaConfiguration2 BindType<T>(string typeName);
        ISchemaConfiguration2 BindType<T>(string typeName,
            params Action<IFluentFieldMapping<T>>[] fieldMapping);
        ISchemaConfiguration2 BindType<T>(
            params Action<IFluentFieldMapping<T>>[] fieldMapping);




        /*
            c.RegisterScalar<StringType>(new StringType());
            c.RegisterScalar(new StringType());

            c.BindType<T>().To("xyz")

            c.BindResolver<A>().To("foo");
            c.BindResolver<A>().To("foo")
                .WithMapping(m => m.From(t => t.y).To("x"));

            c.BindResolver<A>().To<B>()
                .WithMapping(m => m.From(t => t.y).To(t => t.x));

            c.BindResolver<A>().To<B>()
                .WithMapping(
                    m => m.From(t => t.y).To(t => t.x).And()
                        .From(t => t.y).To("x")).And()
             .BindResolver...

            c.BindResolver(delegate).To("foo", "bar");

           c.BindType<X>().ToQuery();
           c.BindType<X>().ToMutation();
           c.BindType<X>().ToSubscription();

         */
    }
}
