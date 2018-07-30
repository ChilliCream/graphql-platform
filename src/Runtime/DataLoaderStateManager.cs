using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Runtime
{
    public class DataLoaderStateManager
    {
        private readonly StateObjectCollection<string> _globalStateObjects;
        private readonly UserStateManager<string> _userState;
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
            _userState = new UserStateManager<string>(size);
            _globalStateObjects =
                new StateObjectCollection<string>(ExecutionScope.Global);
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
                _userState.CreateUserState(userKey);

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

        public void FinalizeState(DataLoaderState dataLoaderState)
        {
            if (dataLoaderState == null)
            {
                throw new ArgumentNullException(nameof(dataLoaderState));
            }

            StateObjectCollection<string> userState = dataLoaderState.Scopes
                .FirstOrDefault(t => t.Scope == ExecutionScope.User);
            if (userState != null)
            {
                _userState.FinalizeUserState(userState);
            }
        }
    }
}
