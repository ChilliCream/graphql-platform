using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Internal
{
    internal interface IServiceDescriptor<TKey>
    {
        TKey Key { get; }

        Type ServiceType { get; }

        Type InstanceType { get; }

        Func<IServiceProvider, object> Factory { get; }

        ExecutionScope Scope { get; }
    }

    internal class ServiceScope<TKey>
        : IDisposable
    {
        private readonly object _sync = new object();

        private ImmutableDictionary<TKey, object> _instances =
            ImmutableDictionary<TKey, object>.Empty;

        public ServiceScope(ExecutionScope scope)
        {
            Scope = scope;
        }

        public ExecutionScope Scope { get; }

        public object GetService(
            IServiceDescriptor<TKey> descriptor,
            Func<object> serviceFactory)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }

            if (!_instances.TryGetValue(descriptor.Key, out object instance))
            {
                lock (_sync)
                {
                    if (!_instances.TryGetValue(descriptor.Key, out instance))
                    {
                        instance = serviceFactory();
                        _instances = _instances
                            .SetItem(descriptor.Key, instance);
                    }
                }
            }

            return instance;
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in
                _instances.Values.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }
    }

    internal class ServiceContainer<TKey>
    {
        private readonly IServiceDescriptor<TKey>[] _services;
        private readonly Dictionary<ExecutionScope, ServiceScope<TKey>> _scopes;
        private readonly ILookup<Type, IServiceDescriptor<TKey>> _serviceLookUp;

        public ServiceContainer(
            IServiceProvider root,
            IEnumerable<IServiceDescriptor<TKey>> services,
            ISet<ExecutionScope> scopes)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            _services = services.ToArray();
            _scopes = new Dictionary<ExecutionScope, ServiceScope<TKey>>();
            foreach (ExecutionScope scope in scopes)
            {
                _scopes[scope] = new ServiceScope<TKey>(scope);
            }
            _serviceLookUp = _services.ToLookup(t => t.ServiceType);
        }

        public object GetService(TKey key)
        {

        }

        private ConstructorInfo GetConstructor(Type instanceType)
        {
            ConstructorInfo[] constructors = instanceType
                .GetConstructors(BindingFlags.Public);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"The instance type `{instanceType.FullName}` " +
                    "must have at least on constructor.");
            }

            if (constructors.Length == 1)
            {
                return constructors[0];
            }


        }

        private ConstructorInfo GetBestMatchingConstructor(
            ConstructorInfo[] constructors)
        {
            foreach (ConstructorInfo constructor in constructors
                .OrderByDescending(t => t.GetParameters().Length))
            {

            }
        }


    }
}
