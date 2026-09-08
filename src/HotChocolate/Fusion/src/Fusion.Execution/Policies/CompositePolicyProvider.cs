using System.Collections.Immutable;
using HotChocolate.Fusion.Configuration;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Combines the schema's built-in policies with a user-registered <see cref="IPolicyProvider"/>,
/// if any, into the single provider the schema services resolve. A user policy with the same
/// name as a built-in one wins: the built-in is omitted from the combined snapshot published to
/// subscribers. Duplicate names within the user provider's own snapshot are left untouched here
/// and rejected downstream exactly as before, since only the built-ins are merged by name.
/// </summary>
/// <remarks>
/// When the wrapped provider also acts as the sink for raw policy content (for example
/// <c>RegoPolicyProvider</c>), this composite forwards that content through unchanged, so pushing
/// content to the schema services' <see cref="IPolicyProvider"/> keeps working exactly as it did
/// before built-in policies existed.
/// </remarks>
internal sealed class CompositePolicyProvider : IPolicyProvider, IObserver<PolicyContentSnapshot?>
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly IPolicyProvider? _inner;
    private readonly ImmutableArray<IPolicy> _builtIns;
    private readonly IDisposable? _innerSubscription;
    private ImmutableArray<IPolicy> _current;
    private ImmutableArray<IObserver<ImmutableArray<IPolicy>>> _observers = [];
    private bool _disposed;

    public CompositePolicyProvider(IPolicyProvider? inner, ImmutableArray<IPolicy> builtIns)
    {
        _inner = inner;
        _builtIns = builtIns;
        _current = Combine([], builtIns);

        // IPolicyProvider.Subscribe synchronously replays the current snapshot, so this call
        // brings _current up to date with the inner provider before the constructor returns.
        if (inner is not null)
        {
            _innerSubscription = inner.Subscribe(new InnerObserver(this));
        }
    }

    public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_sync)
        {
            if (_disposed)
            {
                observer.OnCompleted();
                return EmptySubscription.Instance;
            }

            observer.OnNext(_current);
            _observers = _observers.Add(observer);
        }

        return new Subscription(this, observer);
    }

    public void OnNext(PolicyContentSnapshot? value)
    {
        if (_inner is IObserver<PolicyContentSnapshot?> sink)
        {
            sink.OnNext(value);
        }
    }

    public void OnError(Exception error)
    {
        if (_inner is IObserver<PolicyContentSnapshot?> sink)
        {
            sink.OnError(error);
        }
    }

    public void OnCompleted()
    {
        if (_inner is IObserver<PolicyContentSnapshot?> sink)
        {
            sink.OnCompleted();
        }
    }

    private void ApplyInnerSnapshot(ImmutableArray<IPolicy> userPolicies)
    {
        IObserver<ImmutableArray<IPolicy>>[] observers;
        ImmutableArray<IPolicy> snapshot;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            snapshot = Combine(userPolicies, _builtIns);
            _current = snapshot;
            observers = [.. _observers];
        }

        foreach (var observer in observers)
        {
            observer.OnNext(snapshot);
        }
    }

    private static ImmutableArray<IPolicy> Combine(
        ImmutableArray<IPolicy> userPolicies,
        ImmutableArray<IPolicy> builtIns)
    {
        if (userPolicies.IsDefaultOrEmpty)
        {
            return builtIns;
        }

        var userNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var policy in userPolicies)
        {
            userNames.Add(policy.Name);
        }

        var builder = ImmutableArray.CreateBuilder<IPolicy>(userPolicies.Length + builtIns.Length);
        builder.AddRange(userPolicies);

        foreach (var policy in builtIns)
        {
            if (!userNames.Contains(policy.Name))
            {
                builder.Add(policy);
            }
        }

        return builder.ToImmutable();
    }

    private void Unsubscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        lock (_sync)
        {
            _observers = _observers.Remove(observer);
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _innerSubscription?.Dispose();

        if (_inner is not null)
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class InnerObserver(CompositePolicyProvider owner) : IObserver<ImmutableArray<IPolicy>>
    {
        public void OnNext(ImmutableArray<IPolicy> value) => owner.ApplyInnerSnapshot(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private sealed class Subscription(
        CompositePolicyProvider provider,
        IObserver<ImmutableArray<IPolicy>> observer) : IDisposable
    {
        public void Dispose() => provider.Unsubscribe(observer);
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static readonly EmptySubscription Instance = new();

        public void Dispose()
        {
        }
    }
}
