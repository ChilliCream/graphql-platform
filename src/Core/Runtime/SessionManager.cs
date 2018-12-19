using System;
using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    public sealed class SessionManager
        : ISessionManager
    {
        private readonly IServiceProvider _globalServices;
        private readonly StateObjectDescriptorCollection<string> _dataLoaderDescriptors;
        private readonly StateObjectCollection<string> _globalDataLoaders;
        private readonly StateObjectDescriptorCollection<Type> _customContextDescriptors;
        private readonly StateObjectCollection<Type> _globalCustomContexts;
        private bool _disposed;

        public SessionManager(
            IServiceProvider globalServices,
            IEnumerable<DataLoaderDescriptor> dataLoaderDescriptors,
            IEnumerable<CustomContextDescriptor> customContextDescriptors)
        {
            if (dataLoaderDescriptors == null)
            {
                throw new ArgumentNullException(nameof(dataLoaderDescriptors));
            }

            if (customContextDescriptors == null)
            {
                throw new ArgumentNullException(nameof(customContextDescriptors));
            }

            _globalServices = globalServices
                ?? throw new ArgumentNullException(nameof(globalServices));
            _dataLoaderDescriptors =
                new StateObjectDescriptorCollection<string>(
                        dataLoaderDescriptors);
            _customContextDescriptors =
                new StateObjectDescriptorCollection<Type>(
                        customContextDescriptors);
            _globalDataLoaders = new StateObjectCollection<string>(
                ExecutionScope.Global);
            _globalCustomContexts = new StateObjectCollection<Type>(
                ExecutionScope.Global);
        }

        public ISession CreateSession(IServiceProvider requestServices)
        {
            return new Session(
                () => CreateDataLoaderProvider(requestServices),
                () => CreateCustomContextProvider(requestServices));
        }

        private IDataLoaderProvider CreateDataLoaderProvider(
            IServiceProvider requestServices)
        {
            return new DataLoaderProvider(
                _globalServices, requestServices,
                _dataLoaderDescriptors, _globalDataLoaders);
        }

        private ICustomContextProvider CreateCustomContextProvider(
            IServiceProvider requestServices)
        {
            return new CustomContextProvider(
                _globalServices, requestServices,
                _customContextDescriptors, _globalCustomContexts);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _globalDataLoaders.Dispose();
                _globalCustomContexts.Dispose();
                _disposed = true;
            }
        }
    }
}
