using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration;

internal sealed class DefaultRequestExecutorOptionsMonitor
    : IRequestExecutorOptionsMonitor
    , IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IOptionsMonitor<RequestExecutorSetup> _optionsMonitor;
    private readonly IRequestExecutorOptionsProvider[] _optionsProviders;
    private readonly Dictionary<string, List<IConfigureRequestExecutorSetup>> _configs =
        new();
    private readonly List<IDisposable> _disposables = [];
    private readonly List<Action<string>> _listeners = [];
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
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var options = new RequestExecutorSetup();
        _optionsMonitor.Get(schemaName).CopyTo(options);

        if (_configs.TryGetValue(schemaName, out var configurations))
        {
            foreach (var configuration in configurations)
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

                foreach (var provider in _optionsProviders)
                {
                    _disposables.Add(provider.OnChange(OnChange));

                    var allConfigurations =
                        await provider.GetOptionsAsync(cancellationToken)
                            .ConfigureAwait(false);

                    foreach (var configuration in allConfigurations)
                    {
                        if (!_configs.TryGetValue(
                            configuration.SchemaName,
                            out var configurations))
                        {
                            configurations = [];
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

    public IDisposable OnChange(Action<string> listener) =>
        new Session(this, listener);

    private void OnChange(IConfigureRequestExecutorSetup changes)
    {
        _initialized = false;

        lock (_listeners)
        {
            foreach (var listener in _listeners)
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

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            _disposed = true;
        }
    }

    private sealed class Session : IDisposable
    {
        private readonly DefaultRequestExecutorOptionsMonitor _monitor;
        private readonly Action<string> _listener;

        public Session(
            DefaultRequestExecutorOptionsMonitor monitor,
            Action<string> listener)
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
