using System;

namespace HotChocolate.Runtime
{
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
