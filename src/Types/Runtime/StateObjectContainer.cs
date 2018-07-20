using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;

namespace HotChocolate.Runtime
{
    public class StateObjectContainer<TKey>
    {
        private readonly IServiceProvider _root;
        private readonly StateObjectDescriptorCollection<TKey> _descriptors;
        private readonly Dictionary<ExecutionScope, StateObjectCollection<TKey>> _scopes;

        protected StateObjectContainer(
            IServiceProvider root,
            StateObjectDescriptorCollection<TKey> descriptors,
            ISet<ExecutionScope> scopes,
            IEnumerable<StateObjectCollection<TKey>> objectCollections)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            if (objectCollections == null)
            {
                throw new ArgumentNullException(nameof(objectCollections));
            }

            _root = root
                ?? throw new ArgumentNullException(nameof(root));
            _descriptors = descriptors
                ?? throw new ArgumentNullException(nameof(descriptors));

            _scopes =
                new Dictionary<ExecutionScope, StateObjectCollection<TKey>>();

            foreach (StateObjectCollection<TKey> collection in
                objectCollections)
            {
                _scopes[collection.Scope] = collection;
            }

            foreach (ExecutionScope scope in scopes)
            {
                if (!_scopes.ContainsKey(scope))
                {
                    _scopes[scope] = new StateObjectCollection<TKey>(scope);
                }
            }
        }

        protected StateObjectContainer(
            IServiceProvider root,
            StateObjectDescriptorCollection<TKey> descriptors,
            ISet<ExecutionScope> scopes)
            : this(root, descriptors, scopes,
                Enumerable.Empty<StateObjectCollection<TKey>>())
        {
        }

        public IEnumerable<StateObjectCollection<TKey>> Scopes =>
            _scopes.Values;

        protected object GetStateObject(TKey key)
        {
            if (_descriptors.TryGetDescriptor(key,
                out IScopedStateDescriptor<TKey> descriptor))
            {
                StateObjectCollection<TKey> objects = _scopes[descriptor.Scope];

                if (objects.TryGetObject(key, out object instance))
                {
                    return instance;
                }

                return objects.CreateObject(
                    descriptor, _descriptors.CreateFactory(_root, descriptor));
            }

            return null;
        }

        protected bool TryGetStateObjectDescriptor<T>(TKey key, out T descriptor)
            where T : IScopedStateDescriptor<TKey>
        {
            if (_descriptors.TryGetDescriptor(key,
                out IScopedStateDescriptor<TKey> d)
                && d is T t)
            {
                descriptor = t;
                return true;
            }

            descriptor = default(T);
            return false;
        }
    }
}
