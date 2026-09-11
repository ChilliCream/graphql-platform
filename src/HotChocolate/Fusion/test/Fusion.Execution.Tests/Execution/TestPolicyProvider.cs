using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class TestPolicyProvider : IPolicyProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly List<IObserver<ImmutableArray<IPolicy>>> _observers = [];
    private readonly bool _disposePolicies;
    private ImmutableArray<IPolicy> _current;

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
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            if (!names.Add(policy.Name))
            {
                throw ThrowHelper.PolicyNameDuplicate(policy.Name);
            }
        }

        _current = [.. policies];
    }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Pushes a complete replacement snapshot to subscribers.
    /// </summary>
    public void Emit(params IPolicy[] policies)
    {
        IObserver<ImmutableArray<IPolicy>>[] observers;
        ImmutableArray<IPolicy> snapshot = [.. policies];

        lock (_sync)
        {
            _current = snapshot;
            observers = [.. _observers];
        }

        foreach (var observer in observers)
        {
            observer.OnNext(snapshot);
        }
    }

    public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        ImmutableArray<IPolicy> current;

        lock (_sync)
        {
            _observers.Add(observer);
            current = [.. _current];
        }

        observer.OnNext(current);

        return new Subscription(this, observer);
    }

    private void Unsubscribe(IObserver<ImmutableArray<IPolicy>> observer)
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
            policies = [.. _current];
        }

        foreach (var policy in policies)
        {
            if (_disposePolicies && policy is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return ValueTask.CompletedTask;
    }

    private sealed class Subscription(
        TestPolicyProvider provider,
        IObserver<ImmutableArray<IPolicy>> observer)
        : IDisposable
    {
        public void Dispose() => provider.Unsubscribe(observer);
    }
}

internal sealed class TestPolicy : IPolicy
{
    public TestPolicy(string name)
        : this(name, PolicyRequirements.Empty)
    {
    }

    public TestPolicy(
        string name,
        SelectionSetNode? resource)
        : this(
            name,
            resource is null
                ? PolicyRequirements.Empty
                : new PolicyRequirements { Resource = resource })
    {
    }

    public TestPolicy(
        string name,
        PolicyRequirements requirements)
    {
        Name = name;
        Requirements = requirements;
    }

    public string Name { get; }

    public PolicyRequirements Requirements { get; }

    public ValueTask EvaluateAsync(
        IPolicyContext context,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
