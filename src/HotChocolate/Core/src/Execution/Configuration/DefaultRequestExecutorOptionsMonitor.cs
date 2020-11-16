using System;
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
        private readonly IOptionsMonitor<RequestExecutorSetup> _optionsMonitor;
        private readonly IRequestExecutorOptionsProvider[] _optionsProviders;
        private readonly Dictionary<NameString, List<IConfigureRequestExecutorSetup>> _configs =
            new Dictionary<NameString, List<IConfigureRequestExecutorSetup>>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly List<Action<NameString>> _listeners = new List<Action<NameString>>();
        private bool _initialized;
        private bool _disposed;

        public DefaultRequestExecutorOptionsMonitor(
            IOptionsMonitor<RequestExecutorSetup> optionsMonitor,
            IEnumerable<IRequestExecutorOptionsProvider> optionsProviders)
        {
            _optionsMonitor = optionsMonitor;
            _optionsProviders = optionsProviders.ToArray();
        }

        public async ValueTask<RequestExecutorSetup> GetAsync(
            NameString schemaName,
            CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            var options = new RequestExecutorSetup();
             _optionsMonitor.Get(schemaName).CopyTo(options);

            if (_configs.TryGetValue(
                schemaName,
                out List<IConfigureRequestExecutorSetup>? configurations))
            {
                foreach (IConfigureRequestExecutorSetup configuration in configurations)
                {
                    configuration.Configure(options);
                }
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
                    _configs.Clear();

                    foreach (IRequestExecutorOptionsProvider provider in _optionsProviders)
                    {
                        _disposables.Add(provider.OnChange(OnChange));

                        IEnumerable<IConfigureRequestExecutorSetup> allConfigurations =
                            await provider.GetOptionsAsync(cancellationToken)
                                .ConfigureAwait(false);

                        foreach (IConfigureRequestExecutorSetup configuration in allConfigurations)
                        {
                            if (!_configs.TryGetValue(
                                configuration.SchemaName,
                                out List<IConfigureRequestExecutorSetup>? configurations))
                            {
                                configurations = new List<IConfigureRequestExecutorSetup>();
                                _configs.Add(configuration.SchemaName, configurations);
                            }

                            configurations.Add(configuration);
                        }
                    }

                    _initialized = true;
                }

                _semaphore.Release();
            }
        }

        public IDisposable OnChange(Action<NameString> listener) =>
            new Session(this, listener);

        private void OnChange(IConfigureRequestExecutorSetup changes)
        {
            _initialized = false;

            lock (_listeners)
            {
                foreach (Action<NameString> listener in _listeners)
                {
                    listener.Invoke(changes.SchemaName);
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
            private readonly Action<NameString> _listener;

            public Session(
                DefaultRequestExecutorOptionsMonitor monitor,
                Action<NameString> listener)
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
