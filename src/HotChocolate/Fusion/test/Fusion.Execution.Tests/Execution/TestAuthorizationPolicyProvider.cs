using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider, IDisposable
{
    private readonly Func<IReadOnlyList<IAuthorizationPolicy>> _factory;
    private IReadOnlyList<IAuthorizationPolicy>? _policies;

    public TestAuthorizationPolicyProvider(params IAuthorizationPolicy[] policies)
        : this(() => policies)
    {
    }

    public TestAuthorizationPolicyProvider(
        Func<IReadOnlyList<IAuthorizationPolicy>> factory)
    {
        _factory = factory;
    }

    public int CreateCalls { get; private set; }

    public bool IsDisposed { get; private set; }

    public IReadOnlyList<IAuthorizationPolicy> CreatePolicies()
    {
        CreateCalls++;
        _policies = _factory();
        return _policies;
    }

    public void Dispose()
    {
        IsDisposed = true;

        if (_policies is null)
        {
            return;
        }

        var disposed = new HashSet<IAuthorizationPolicy>(ReferenceEqualityComparer.Instance);

        foreach (var policy in _policies)
        {
            if (disposed.Add(policy) && policy is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

internal sealed class TestAuthorizationPolicy : IAuthorizationPolicy
{
    public TestAuthorizationPolicy(string name)
        : this(name, null)
    {
    }

    public TestAuthorizationPolicy(
        string name,
        SelectionSetNode? requirements)
    {
        Name = name;
        Requirements = requirements;
    }

    public string Name { get; }

    public SelectionSetNode? Requirements { get; }

    public ValueTask EvaluateAsync(
        IAuthorizationContext context,
        EntityData entities,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
