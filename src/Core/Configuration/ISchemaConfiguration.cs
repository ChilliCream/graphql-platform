using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
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

        void RegisterScalar<T>(T scalarType)
            where T : ScalarType;

        void RegisterScalar<T>()
            where T : ScalarType, new();
    }
}
