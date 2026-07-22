using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class TestPolicyProvider : IPolicyProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly List<IObserver<PolicyUpdate>> _observers = [];
    private readonly Dictionary<string, IPolicy> _current = new(StringComparer.Ordinal);
    private readonly bool _disposePolicies;

    public TestPolicyProvider(params IPolicy[] policies)
        : this(true, policies)
    {
    }

    public TestPolicyProvider(bool disposePolicies, params IPolicy[] policies)
    {
        _disposePolicies = disposePolicies;
        Add(policies);
    }

    public TestPolicyProvider(Func<IReadOnlyList<IPolicy>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _disposePolicies = true;
        Add(factory());
    }

    private void Add(IReadOnlyList<IPolicy> policies)
    {
        foreach (var policy in policies)
        {
            if (!_current.TryAdd(policy.Name, policy))
            {
                throw ThrowHelper.PolicyNameDuplicate(policy.Name);
            }
        }
    }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Pushes a single per-policy update to the subscribers, replacing or removing the policy with
    /// the same name. A replaced or removed instance that manages its own lifetime is released after
    /// the update has been delivered, mirroring the ownership contract of a real provider.
    /// </summary>
    public void Emit(PolicyUpdate update)
    {
        IObserver<PolicyUpdate>[] observers;
        IPolicy? previous;

        lock (_sync)
        {
            _current.TryGetValue(update.Name, out previous);

            if (update.Policy is null)
            {
                _current.Remove(update.Name);
            }
            else
            {
                _current[update.Name] = update.Policy;
            }

            observers = [.. _observers];
        }

        foreach (var observer in observers)
        {
            observer.OnNext(update);
        }

        if (previous is IPolicyLifetime lifetime && !ReferenceEquals(previous, update.Policy))
        {
            lifetime.Release();
        }
    }

    public IDisposable Subscribe(IObserver<PolicyUpdate> observer)
    {
        KeyValuePair<string, IPolicy>[] current;

        lock (_sync)
        {
            _observers.Add(observer);
            current = [.. _current];
        }

        foreach (var (name, policy) in current)
        {
            observer.OnNext(new PolicyUpdate(name, policy));
        }

        return new Subscription(this, observer);
    }

    private void Unsubscribe(IObserver<PolicyUpdate> observer)
    {
        lock (_sync)
        {
            _observers.Remove(observer);
        }
    }

    public ValueTask DisposeAsync()
    {
        IPolicy[] policies;

        lock (_sync)
        {
            if (IsDisposed)
            {
                return ValueTask.CompletedTask;
            }

            IsDisposed = true;
            policies = [.. _current.Values];
        }

        foreach (var policy in policies)
        {
            if (policy is IPolicyLifetime lifetime)
            {
                lifetime.Release();
            }
            else if (_disposePolicies && policy is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return ValueTask.CompletedTask;
    }

    private sealed class Subscription(TestPolicyProvider provider, IObserver<PolicyUpdate> observer)
        : IDisposable
    {
        public void Dispose() => provider.Unsubscribe(observer);
    }
}

internal sealed class TestPolicy : IPolicy
{
    public TestPolicy(string name)
        : this(name, null)
    {
    }

    public TestPolicy(
        string name,
        SelectionSetNode? requirements)
    {
        Name = name;
        Requirements = requirements;
    }

    public string Name { get; }

    public SelectionSetNode? Requirements { get; }

    public ValueTask EvaluateAsync(
        IPolicyContext context,
        ReadOnlyMemory<CompositeResultElement> entities,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
