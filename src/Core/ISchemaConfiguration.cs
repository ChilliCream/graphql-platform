using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchemaConfiguration
        : IFluent
    {
        IBindResolverDelegate BindResolver(AsyncFieldResolverDelegate fieldResolver);
        IBindResolverDelegate BindResolver(FieldResolverDelegate fieldResolver);

        IBindResolver<TResolver> BindResolver<TResolver>()
            where TResolver : class;

        IBindResolver<TResolver> BindResolver<TResolver>(BindingBehavior bindingBehavior)
            where TResolver : class;

        IBindType<T> BindType<T>()
            where T : class;

        IBindType<T> BindType<T>(BindingBehavior bindingBehavior)
            where T : class;

        /// <summary>
        /// Registers a custom scalar type.
        /// </summary>
        /// <param name="scalarType">The custom scalar type object.</param>
        /// <typeparam name="T">The custom scalar type.</typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="scalarType"/> cannot be <c>null</c>.
        /// </exception>
        void RegisterScalar<T>(T scalarType)
            where T : ScalarType;

        /// <summary>
        /// Registers a custom scalar type.
        /// </summary>
        /// <typeparam name="T">The custom scalar type.</typeparam>
        void RegisterScalar<T>()
            where T : ScalarType, new();

        // TODO : rename maybe to newtype?
        void RegisterType<T>(params Func<ISchemaContext, T>[] typeFactory)
            where T : INamedTypeConfig;
    }
}
