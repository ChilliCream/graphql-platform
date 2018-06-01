using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly Dictionary<string, INamedType> _types =
            new Dictionary<string, INamedType>();
        private readonly List<ResolverBindingInfo> _resolverBindings =
            new List<ResolverBindingInfo>();
        private readonly List<TypeBindingInfo> _typeBindings =
            new List<TypeBindingInfo>();

        public IBindResolverDelegate BindResolver(
            AsyncFieldResolverDelegate fieldResolver)
        {
            ResolverDelegateBindingInfo bindingInfo =
                new ResolverDelegateBindingInfo
                {
                    AsyncFieldResolver = fieldResolver
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolverDelegate(bindingInfo);
        }

        public IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver)
        {
            ResolverDelegateBindingInfo bindingInfo =
                new ResolverDelegateBindingInfo
                {
                    FieldResolver = fieldResolver
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolverDelegate(bindingInfo);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>()
            where TResolver : class
        {
            return BindResolver<TResolver>(BindingBehavior.Implicit);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class
        {
            ResolverCollectionBindingInfo bindingInfo =
                new ResolverCollectionBindingInfo
                {
                    Behavior = bindingBehavior,
                    ResolverType = typeof(TResolver)
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolver<TResolver>(bindingInfo);
        }

        public IBindType<T> BindType<T>()
            where T : class
        {
            return BindType<T>(BindingBehavior.Implicit);
        }

        public IBindType<T> BindType<T>(BindingBehavior bindingBehavior)
            where T : class
        {
            TypeBindingInfo bindingInfo = new TypeBindingInfo
            {
                Behavior = bindingBehavior,
                Type = typeof(T)
            };
            _typeBindings.Add(bindingInfo);
            return new BindType<T>(bindingInfo);
        }

        public void RegisterType<T>()
            where T : INamedType, new()
        {
            T type = new T();
            _types[type.Name] = type;
        }
    }
}
