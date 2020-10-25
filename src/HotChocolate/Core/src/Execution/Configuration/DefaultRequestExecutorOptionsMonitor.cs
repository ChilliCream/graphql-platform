using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration
{
    internal sealed class DefaultRequestExecutorOptionsMonitor
        : IRequestExecutorOptionsMonitor
            , IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IOptionsMonitor<RequestExecutorFactoryOptions> _optionsMonitor;
        private readonly IRequestExecutorOptionsProvider[] _optionsProviders;
        private readonly ConcurrentDictionary<NameString, INamedRequestExecutorFactoryOptions>
            _options = new ConcurrentDictionary<NameString, INamedRequestExecutorFactoryOptions>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly List<Action<RequestExecutorFactoryOptions, string>> _listeners =
            new List<Action<RequestExecutorFactoryOptions, string>>();
        private bool _initialized;
        private bool _disposed;

        public DefaultRequestExecutorOptionsMonitor(
            IOptionsMonitor<RequestExecutorFactoryOptions> optionsMonitor,
            IEnumerable<IRequestExecutorOptionsProvider> optionsProviders)
        {
            _optionsMonitor = optionsMonitor;
            _optionsProviders = optionsProviders.ToArray();
        }

        public async ValueTask<RequestExecutorFactoryOptions> GetAsync(
            NameString schemaName,
            CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            RequestExecutorFactoryOptions options = _optionsMonitor.Get(schemaName);

            if (_options.TryGetValue(schemaName, out INamedRequestExecutorFactoryOptions? o))
            {
                o.Configure(options);
            }

            return options;
        }

        private async ValueTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (!_initialized)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_initialized)
                {
                    foreach (IRequestExecutorOptionsProvider provider in _optionsProviders)
                    {
                        _disposables.Add(provider.OnChange(OnChange));

                        IEnumerable<INamedRequestExecutorFactoryOptions> allOptions =
                            await provider.GetOptionsAsync(cancellationToken)
                                .ConfigureAwait(false);

                        foreach (NamedRequestExecutorFactoryOptions options in allOptions)
                        {
                            _options[options.SchemaName] = options;
                        }
                    }

                    _initialized = true;
                }

                _semaphore.Release();
            }
        }

        public IDisposable OnChange(Action<RequestExecutorFactoryOptions, string> listener) =>
            new Session(this, listener);

        private void OnChange(INamedRequestExecutorFactoryOptions changes)
        {
            _options[changes.SchemaName] = changes;

            RequestExecutorFactoryOptions options = _optionsMonitor.Get(changes.SchemaName);
            changes.Configure(options);

            lock (_listeners)
            {
                foreach (Action<RequestExecutorFactoryOptions, string> listener in _listeners)
                {
                    listener.Invoke(options, changes.SchemaName);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Dispose();

                foreach (IDisposable disposable in _disposables)
                {
                    disposable.Dispose();
                }

                _disposed = true;
            }
        }

        private class Session : IDisposable
        {
            private readonly DefaultRequestExecutorOptionsMonitor _monitor;
            private readonly Action<RequestExecutorFactoryOptions, string> _listener;

            public Session(
                DefaultRequestExecutorOptionsMonitor monitor,
                Action<RequestExecutorFactoryOptions, string> listener)
            {
                lock (monitor._listeners)
                {
                    _monitor = monitor;
                    _listener = listener;
                    monitor._listeners.Add(listener);
                }
            }

            public void Dispose()
            {
                lock (_monitor._listeners)
                {
                    _monitor._listeners.Remove(_listener);
                }
            }
        }
    }
}
