using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Runtime
{
    public class DataLoaderProvider
        : StateObjectContainer<string>
        , IDataLoaderProvider
    {
        private readonly object _sync = new object();
        private ImmutableDictionary<string, DataLoaderInfo> _touchedDataLoaders =
            ImmutableDictionary<string, DataLoaderInfo>.Empty;

        public DataLoaderProvider(
            IServiceProvider globalServices,
            IServiceProvider requestServices,
            StateObjectDescriptorCollection<string> descriptors,
            StateObjectCollection<string> globalStates)
            : base(globalServices, requestServices, descriptors, globalStates)
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
                _touchedDataLoaders =
                    ImmutableDictionary<string, DataLoaderInfo>.Empty;
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
