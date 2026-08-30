using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Provides access to the authorization policies owned by a Fusion schema, keyed by policy name.
/// </summary>
/// <remarks>
/// The registered provider publishes complete immutable snapshots; the collection replaces its
/// read-only view atomically whenever a new one arrives.
/// </remarks>
public sealed class PolicyCollection
    : IReadOnlyList<IPolicy>
    , IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _applySync = new();
#else
    private readonly object _applySync = new();
#endif
    private readonly IPolicyProvider? _provider;
    private IDisposable? _subscription;
    private OperationPlanCache? _planCache;
    private Snapshot _snapshot = Snapshot.Empty;

    internal PolicyCollection(IPolicyProvider? provider)
    {
        _provider = provider;
    }

    internal static PolicyCollection Empty { get; } = new((IPolicyProvider?)null);

    /// <summary>
    /// Subscribes to the registered policy provider. The provider synchronously replays its
    /// current policies during the subscription.
    /// </summary>
    internal void Connect()
    {
        if (_provider is not null)
        {
            _subscription = _provider.Subscribe(new ProviderObserver(this));
        }
    }

    /// <summary>
    /// Attaches the operation plan cache of the owning schema generation, so that a subsequent
    /// requirement change can evict the plans it affects before the new policies are published.
    /// Attached once, after the schema and its services have finished construction.
    /// </summary>
    internal void AttachPlanCache(OperationPlanCache planCache)
    {
        ArgumentNullException.ThrowIfNull(planCache);
        Volatile.Write(ref _planCache, planCache);
    }

    /// <summary>
    /// Gets the number of policies in the current snapshot.
    /// </summary>
    public int Count => Volatile.Read(ref _snapshot).Policies.Length;

    /// <summary>
    /// Gets the policy at the specified index in the current snapshot.
    /// </summary>
    public IPolicy this[int index] => Volatile.Read(ref _snapshot).Policies[index];

    /// <summary>
    /// Gets the policy with the specified name from the current snapshot.
    /// </summary>
    public IPolicy this[string name] => Get(name);

    /// <summary>
    /// Gets the authorization policy with the specified name from the current snapshot.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// The collection does not contain a policy with the specified name.
    /// </exception>
    public IPolicy Get(string name)
        => Volatile.Read(ref _snapshot).Lookup.TryGetValue(name, out var policy)
            ? policy
            : throw ThrowHelper.PolicyNameNotFound(name);

    /// <summary>
    /// Tries to get the authorization policy with the specified name.
    /// </summary>
    public bool TryGet(string name, [NotNullWhen(true)] out IPolicy? policy)
        => Volatile.Read(ref _snapshot).Lookup.TryGetValue(name, out policy);

    /// <summary>
    /// Gets a point-in-time snapshot of the policies in the collection.
    /// </summary>
    internal ImmutableArray<IPolicy> GetSnapshot()
        => Volatile.Read(ref _snapshot).Policies;

    private void Apply(ImmutableArray<IPolicy> policies)
    {
        lock (_applySync)
        {
            var policyLookup = new Dictionary<string, IPolicy>(StringComparer.Ordinal);
            var policyBuilder = ImmutableArray.CreateBuilder<IPolicy>();

            if (!policies.IsDefault)
            {
                foreach (var policy in policies)
                {
                    ArgumentNullException.ThrowIfNull(policy);

                    if (string.IsNullOrEmpty(policy.Name))
                    {
                        throw ThrowHelper.PolicyNameEmpty();
                    }

                    if (!policyLookup.TryAdd(policy.Name, policy))
                    {
                        throw ThrowHelper.PolicyNameDuplicate(policy.Name);
                    }

                    policyBuilder.Add(policy);
                }
            }

            var previous = _snapshot;
            var snapshot = new Snapshot(
                policyBuilder.ToImmutable(),
                policyLookup.ToFrozenDictionary(StringComparer.Ordinal));

            // A published set can change what a policy requires (for example its Rego resource
            // requirement), and an already-cached operation plan baked the previous requirement
            // into its fetch nodes. Evicting the affected plans before the new snapshot becomes
            // visible keeps the two consistent, but not purely by timing: a plan that was built
            // against the previous snapshot and is still in flight when this eviction runs is
            // either found and evicted by name here, or, if it has not been added yet, rejected
            // at insert time by the plan cache's version check in OperationPlanCache.Add. A plan
            // that never referenced a changed name is left alone either way.
            EvictStalePlans(previous.Lookup, policyLookup);

            Volatile.Write(ref _snapshot, snapshot);
        }
    }

    private void EvictStalePlans(
        FrozenDictionary<string, IPolicy> previous,
        Dictionary<string, IPolicy> current)
    {
        var planCache = Volatile.Read(ref _planCache);

        if (planCache is null)
        {
            return;
        }

        // A count mismatch means a name was added or removed. A name absent from the previous
        // set can already be referenced by cached plans that were planned while the name was
        // missing (the planner skips unknown names, it does not fail closed), so an addition
        // cannot be attributed to a specific policy name and the whole cache is discarded, not
        // just the plans for the added or removed name.
        if (previous.Count != current.Count)
        {
            planCache.Reset();
            return;
        }

        List<string>? changedNames = null;

        foreach (var (name, previousPolicy) in previous)
        {
            if (!current.TryGetValue(name, out var policy))
            {
                // Same count, different names: one name was replaced by another. Falls under the
                // same reasoning as a count mismatch above.
                planCache.Reset();
                return;
            }

            if (!RequirementsEqual(previousPolicy.Requirements, policy.Requirements))
            {
                (changedNames ??= []).Add(name);
            }
        }

        if (changedNames is not null)
        {
            planCache.EvictPolicies(changedNames);
        }
    }

    private static bool RequirementsEqual(PolicyRequirements left, PolicyRequirements right)
    {
        if (left.Resource is null || right.Resource is null)
        {
            return left.Resource is null && right.Resource is null;
        }

        return SyntaxComparer.BySyntax.Equals(left.Resource, right.Resource);
    }

    /// <summary>
    /// Returns an enumerator over the policies in the current snapshot.
    /// </summary>
    public IEnumerator<IPolicy> GetEnumerator()
        => ((IEnumerable<IPolicy>)Volatile.Read(ref _snapshot).Policies).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Unsubscribes from the registered policy provider.
    /// </summary>
    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private sealed class ProviderObserver(PolicyCollection owner)
        : IObserver<ImmutableArray<IPolicy>>
    {
        public void OnNext(ImmutableArray<IPolicy> value) => owner.Apply(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private sealed record Snapshot(
        ImmutableArray<IPolicy> Policies,
        FrozenDictionary<string, IPolicy> Lookup)
    {
        public static Snapshot Empty { get; } = new(
            [],
            new Dictionary<string, IPolicy>(StringComparer.Ordinal)
                .ToFrozenDictionary(StringComparer.Ordinal));
    }
}
