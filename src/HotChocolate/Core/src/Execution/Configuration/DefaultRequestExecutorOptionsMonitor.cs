using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration;

internal sealed class DefaultRequestExecutorOptionsMonitor(
    IOptionsMonitor<RequestExecutorSetup> optionsMonitor,
    IEnumerable<IRequestExecutorOptionsProvider> optionsProviders)
    : IRequestExecutorOptionsMonitor
    , IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IRequestExecutorOptionsProvider[] _optionsProviders = optionsProviders.ToArray();
    private readonly Dictionary<string, List<IConfigureRequestExecutorSetup>> _configs = [];
    private readonly List<IDisposable> _disposables = [];
    private readonly List<Action<string>> _listeners = [];
    private bool _initialized;
    private bool _disposed;

    public async ValueTask<RequestExecutorSetup> GetAsync(
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        await TryInitializeAsync(cancellationToken).ConfigureAwait(false);

        var options = new RequestExecutorSetup();
        optionsMonitor.Get(schemaName).CopyTo(options);

        if (_configs.TryGetValue(schemaName, out var configurations))
        {
            foreach (var configuration in configurations)
            {
                configuration.Configure(options);
            }
        }

        return options;
    }

    public async ValueTask<ImmutableArray<string>> GetSchemaNamesAsync(
        CancellationToken cancellationToken)
    {
        await TryInitializeAsync(cancellationToken).ConfigureAwait(false);
        return [.. _configs.Keys.Order()];
    }

    private async ValueTask TryInitializeAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await TryInitializeUnsafeAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    private async ValueTask TryInitializeUnsafeAsync(CancellationToken cancellationToken)
    {
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
    }

    public IDisposable OnChange(Action<string> listener)
        => new Session(this, listener);

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
