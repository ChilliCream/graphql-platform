using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
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
        private readonly IServiceProvider _services;

        public SchemaConfiguration()
            : this(new DefaultServiceProvider())
        {
        }

        public SchemaConfiguration(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public string QueryTypeName { get; private set; }

        public string MutationTypeName { get; private set; }

        public string SubscriptionTypeName { get; private set; }

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

        #region RegisterType - Type

        public void RegisterType<T>()
            where T : class, INamedType
        {
            CreateAndRegisterType<T>();
        }

        public void RegisterQueryType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            QueryTypeName = type.Name;
        }

        public void RegisterMutationType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            MutationTypeName = type.Name;
        }

        public void RegisterSubscriptionType<T>()
            where T : ObjectType
        {
            T type = CreateAndRegisterType<T>();
            SubscriptionTypeName = type.Name;
        }

        private T CreateAndRegisterType<T>()
            where T : class, INamedType
        {
            if (BaseTypes.IsNonGenericBaseType(typeof(T)))
            {
                throw new SchemaException(new SchemaError(
                    "You cannot add a type without specifing its " +
                    "name and attributes."));
            }

            T type = (T)_services.GetService(typeof(T));
            _types[type.Name] = type;
            return type;
        }

        #endregion

        #region RegisterType - Instance

        public void RegisterType<T>(T namedType)
            where T : class, INamedType
        {
            _types[namedType.Name] = namedType;
        }

        public void RegisterQueryType<T>(T objectType) where T : ObjectType
        {
            QueryTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        public void RegisterMutationType<T>(T objectType) where T : ObjectType
        {
            MutationTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        public void RegisterSubscriptionType<T>(T objectType) where T : ObjectType
        {
            SubscriptionTypeName = objectType.Name;
            _types[objectType.Name] = objectType;
        }

        #endregion
    }
}
