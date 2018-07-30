using System;
using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    public interface ISession
        : IDisposable
    {
        IDataLoaderProvider DataLoaders { get; }
        ICustomContextProvider CustomContexts { get; }
    }

    public interface ISessionManager
        : IDisposable
    {
        ISession CreateSession(IServiceProvider requestServices);
    }

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
                null);
        }

        private IDataLoaderProvider CreateDataLoaderProvider(
            IServiceProvider requestServices)
        {
            return new DataLoaderProvider(
                _globalServices, requestServices,
                _dataLoaderDescriptors, _globalDataLoaders);
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

    public sealed class Session
        : ISession
    {
        private readonly object _sync = new object();
        private readonly Func<IDataLoaderProvider> _dataLoadersFactory;
        private readonly Func<ICustomContextProvider> _customContextsFactory;

        private IDataLoaderProvider _dataLoaders;
        private ICustomContextProvider _customContexts;
        private bool _disposed;

        public Session(
            Func<IDataLoaderProvider> dataLoadersFactory,
            Func<ICustomContextProvider> customContextsFactory)
        {
            _dataLoadersFactory = dataLoadersFactory
                ?? throw new ArgumentNullException(nameof(dataLoadersFactory));
            _customContextsFactory = customContextsFactory
                ?? throw new ArgumentNullException(nameof(customContextsFactory));
        }

        public IDataLoaderProvider DataLoaders
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("Session");
                }

                if (_dataLoaders == null)
                {
                    lock (_sync)
                    {
                        if (_dataLoaders == null)
                        {
                            _dataLoaders = _dataLoadersFactory();
                        }
                    }
                }
                return _dataLoaders;
            }
        }

        public ICustomContextProvider CustomContexts
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("Session");
                }

                if (_customContexts == null)
                {
                    lock (_sync)
                    {
                        if (_customContexts == null)
                        {
                            _customContexts = _customContextsFactory();
                        }
                    }
                }
                return _customContexts;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dataLoaders?.Dispose();
                _customContexts?.Dispose();
                _disposed = true;
            }
        }
    }
}
