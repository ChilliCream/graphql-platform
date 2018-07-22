using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Runtime
{
    public class DataLoaderState
        : StateObjectContainer<string>
        , IDataLoaderState
    {
        private static readonly HashSet<ExecutionScope> _scopes =
            new HashSet<ExecutionScope>
            {
                ExecutionScope.Global,
                ExecutionScope.User,
                ExecutionScope.Request
            };

        private readonly object _sync = new object();
        private ImmutableDictionary<string, DataLoaderInfo> _touchedDataLoaders =
            ImmutableDictionary<string, DataLoaderInfo>.Empty;

        public DataLoaderState(
            IServiceProvider root,
            DataLoaderDescriptorCollection descriptors,
            IEnumerable<StateObjectCollection<string>> objectCollections)
            : base(root, descriptors, _scopes, objectCollections)
        {
        }

        public IEnumerable<DataLoaderInfo> Touched =>
            _touchedDataLoaders.Values;

        public T GetDataLoader<T>(string key)
        {
            object instance = GetStateObject(key);
            ToucheDataLoader(key, instance);
            return (T)instance;
        }

        public void Reset()
        {
            lock (_sync)
            {
                _touchedDataLoaders = _touchedDataLoaders.Clear();
            }
        }

        private void ToucheDataLoader(string key, object instance)
        {
            if (!_touchedDataLoaders.ContainsKey(key))
            {
                lock (_sync)
                {
                    if (!_touchedDataLoaders.ContainsKey(key)
                        && TryGetStateObjectDescriptor(
                                key, out DataLoaderDescriptor descriptor))
                    {
                        _touchedDataLoaders = _touchedDataLoaders.SetItem(key,
                            new DataLoaderInfo(descriptor, instance));
                    }
                }
            }
        }
    }
}
