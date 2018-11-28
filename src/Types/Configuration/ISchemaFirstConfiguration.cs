using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface ISchemaFirstConfiguration
        : IFluent
    {
        IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver);

        IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class;

        IBindType<T> BindType<T>(BindingBehavior bindingBehavior)
            where T : class;
    }
}
