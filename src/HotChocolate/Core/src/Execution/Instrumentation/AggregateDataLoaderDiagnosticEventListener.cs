using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Execution.Instrumentation;

internal class AggregateDataLoaderDiagnosticEventListener : DataLoaderDiagnosticEventListener
{
    private readonly IDataLoaderDiagnosticEventListener[] _listeners;

    public AggregateDataLoaderDiagnosticEventListener(
        IDataLoaderDiagnosticEventListener[] listeners)
    {
        _listeners = listeners ?? throw new ArgumentNullException(nameof(listeners));
    }

    public override void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        TaskCacheKey cacheKey,
        Task task)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].ResolvedTaskFromCache(dataLoader, cacheKey, task);
        }
    }

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        var scopes = new IDisposable[_listeners.Length];

        for (var i = 0; i < _listeners.Length; i++)
        {
            scopes[i] = _listeners[i].ExecuteBatch(dataLoader, keys);
        }

        return new AggregateEventScope(scopes);
    }

    public override void BatchResults<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        ReadOnlySpan<Result<TValue>> values)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].BatchResults(keys, values);
        }
    }

    public override void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].BatchError(keys, error);
        }
    }

    public override void BatchItemError<TKey>(
        TKey key,
        Exception error)
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].BatchItemError(key, error);
        }
    }

    private sealed class AggregateEventScope : IDisposable
    {
        private readonly IDisposable[] _scopes;

        public AggregateEventScope(IDisposable[] scopes)
        {
            _scopes = scopes;
        }

        public void Dispose()
        {
            for (var i = 0; i < _scopes.Length; i++)
            {
                _scopes[i].Dispose();
            }
        }
    }
}
