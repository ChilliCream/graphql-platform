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

        void RegisterType<T>(T type)
            where T : class, INamedType;

        void RegisterType<T>()
            where T : class, INamedType;

        void RegisterQuery<T>()
            where T : ObjectType;

        void RegisterMutation<T>()
            where T : ObjectType;

        void RegisterSubscription<T>()
            where T : ObjectType;
    }
}
