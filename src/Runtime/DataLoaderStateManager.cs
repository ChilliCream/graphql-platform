using System;
using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    public class DataLoaderStateManager
    {
        private readonly StateObjectCollection<string> _globalStateObjects;
        private readonly Cache<StateObjectCollection<string>> _cache;
        private readonly DataLoaderDescriptorCollection _dataLoaderDescriptors;

        public DataLoaderStateManager(
            IEnumerable<DataLoaderDescriptor> descriptors,
            int size)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            _dataLoaderDescriptors =
                new DataLoaderDescriptorCollection(descriptors);
            _cache = new Cache<StateObjectCollection<string>>(
                size < 10 ? 10 : size);
        }


        public DataLoaderState CreateState(
            IServiceProvider services, string userKey)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (userKey == null)
            {
                throw new ArgumentNullException(nameof(userKey));
            }

            StateObjectCollection<string> userStateObjects =
                GetOrCreateUserState(userKey);

            var requestStateObjects =
                new StateObjectCollection<string>(ExecutionScope.Request);

            var stateObjects = new[]
            {
                _globalStateObjects,
                userStateObjects,
                requestStateObjects
            };

            return new DataLoaderState(
                services, _dataLoaderDescriptors, stateObjects);
        }

        private StateObjectCollection<string> GetOrCreateUserState(
            string userKey)
        {
            return _cache.GetOrCreate(userKey,
                () => new StateObjectCollection<string>(ExecutionScope.User));
        }
    }
}
