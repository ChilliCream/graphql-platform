using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution;

public sealed class SubscriptionResult : ISubscriptionResult
{
    private readonly Func<IAsyncEnumerable<IQueryResult>>? _resultStreamFactory;
    private IAsyncDisposable? _session;
    private bool _isRead;
    private bool _disposed;

    public SubscriptionResult(
        Func<IAsyncEnumerable<IQueryResult>>? resultStreamFactory,
        IReadOnlyList<IError>? errors,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null,
        IAsyncDisposable? session = null)
    {
        if (resultStreamFactory is null && errors is null)
        {
            throw new ArgumentException("Either provide a result stream factory or errors.");
        }

        _resultStreamFactory = resultStreamFactory;
        Errors = errors;
        Extensions = extensions;
        ContextData = contextData;
        _session = session;
    }

    public SubscriptionResult(
        SubscriptionResult subscriptionResult,
        IAsyncDisposable? session = null)
    {
        _resultStreamFactory = subscriptionResult._resultStreamFactory;
        Errors = subscriptionResult.Errors;
        Extensions = subscriptionResult.Extensions;
        ContextData = subscriptionResult.ContextData;
        _session = session is null
            ? subscriptionResult
            : subscriptionResult.Combine(session);
    }

    public SubscriptionResult(
       SubscriptionResult subscriptionResult,
       IDisposable? session = null)
    {
        _resultStreamFactory = subscriptionResult._resultStreamFactory;
        Errors = subscriptionResult.Errors;
        Extensions = subscriptionResult.Extensions;
        ContextData = subscriptionResult.ContextData;
        _session = session is null
            ? subscriptionResult
            : ((IAsyncDisposable)subscriptionResult).Combine(session);
    }

    public IReadOnlyList<IError>? Errors { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    public IReadOnlyDictionary<string, object?>? ContextData { get; }

    public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
    {
        if (_resultStreamFactory is null)
        {
            throw new InvalidOperationException(
                AbstractionResources.SubscriptionResult_ResultHasErrors);
        }

        if (_isRead)
        {
            throw new InvalidOperationException(
                AbstractionResources.SubscriptionResult_ReadOnlyOnce);
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SubscriptionResult));
        }

        _isRead = true;
        return _resultStreamFactory();
    }

    /// <inheritdoc />
    public void RegisterDisposable(IDisposable disposable)
    {
        if (disposable is null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        _session = _session.Combine(disposable);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_session is not null)
            {
                await _session.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        DisposeAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}
