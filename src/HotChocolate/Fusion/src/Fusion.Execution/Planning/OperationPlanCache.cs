using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Holds the operation plan cache for one schema generation and allows a policy-only update to
/// evict exactly the plans that update affects, in place.
/// </summary>
/// <remarks>
/// A cached plan can become stale when an authorization policy's resource requirements change in a
/// policy-only update, because the same operation document then needs different data fetched.
/// <see cref="EvictPolicies"/> removes only the plans that referenced a changed policy name,
/// keeping every other cached plan warm across the far more common case of a policy source or data
/// change that does not alter what any policy requires. <see cref="Reset"/> remains as a defensive
/// fallback for the case a published policy set's name set unexpectedly differs from the previous
/// one, which cannot be attributed to a specific policy name.
/// </remarks>
internal sealed class OperationPlanCache
{
    // Beyond this multiple of the capacity, the policy-name index is swept to drop ids of plans
    // that the underlying cache has already evicted by capacity (see MaintainIndexSize).
    private const int MaxIndexedIdsMultiplier = 4;

    private readonly int _capacity;
    private readonly CacheDiagnostics? _diagnostics;

    // Even values identify stable cache generations. Mutations bracket their removal or swap with
    // odd values, so Capture never pairs an in-progress cache change with a policy snapshot.
    // Add validates its captured stable value while holding the same lock that protects cache and
    // index publication, so a stale plan is never made reachable.
    private long _version;
    private readonly object _mutationSync = new();

    private Generation _generation;

    // This is deliberately internal and used only by deterministic concurrency tests. It opens
    // the precise capture-to-publication window without changing the production synchronization.
    internal Action? BeforeAddValidation { get; set; }

    public OperationPlanCache(int capacity, CacheDiagnostics? diagnostics)
    {
        _capacity = capacity;
        _diagnostics = diagnostics;
        _generation = new Generation(capacity, diagnostics);
    }

    /// <summary>
    /// Gets the operation plan cache currently in effect.
    /// </summary>
    public Cache<OperationPlan> Current => Volatile.Read(ref _generation).Cache;

    /// <summary>
    /// Test-only accessor for the approximate indexed id count, so tests can assert on index
    /// bookkeeping without relying on internal timing.
    /// </summary>
    internal long IndexedIdCountForTesting => Interlocked.Read(ref Volatile.Read(ref _generation).IndexedIdCount);

    /// <summary>
    /// Captures the cache generation and version currently in effect, for a caller to plan
    /// against and later pass to <see cref="Add"/>.
    /// </summary>
    /// <remarks>
    /// A session is returned only after observing the same stable generation version before and
    /// after the generation reference. A mutation exposes an odd version while it changes the
    /// cache, so a caller never captures an in-progress generation.
    /// </remarks>
    public PlanCacheSession Capture()
    {
        while (true)
        {
            var version = Volatile.Read(ref _version);

            if ((version & 1) != 0)
            {
                continue;
            }

            var generation = Volatile.Read(ref _generation);

            if (version == Volatile.Read(ref _version))
            {
                return new PlanCacheSession(generation, version);
            }
        }
    }

    /// <summary>
    /// Adds a plan to the cache generation captured in <paramref name="session"/> and indexes
    /// the policy names it references, so that a later requirement change on one of them can
    /// find and evict it.
    /// </summary>
    /// <param name="session">
    /// The cache generation and version the plan was resolved against, obtained from
    /// <see cref="Capture"/> before planning. If an eviction affecting this generation ran
    /// between the capture and this call, the plan is not added.
    /// </param>
    /// <param name="id">
    /// The identifier the plan is cached under.
    /// </param>
    /// <param name="plan">
    /// The plan to cache.
    /// </param>
    public void Add(in PlanCacheSession session, string id, OperationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(session.Cache);
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(plan);

        BeforeAddValidation?.Invoke();

        lock (_mutationSync)
        {
            var generation = session.Generation;

            // Validation and publication are one critical section. In particular, never expose
            // a plan before proving it was built against the current policy requirement
            // generation. A stale Add must leave a newer generation's cache and index untouched.
            if (session.Version != Volatile.Read(ref _version)
                || !ReferenceEquals(generation, Volatile.Read(ref _generation)))
            {
                return;
            }

            var cache = generation.Cache;
            var cachedPlan = cache.GetOrCreate(id, static (_, value) => value, plan);

            if (!ReferenceEquals(cachedPlan, plan))
            {
                return;
            }

            if (!plan.Policies.IsEmpty)
            {
                generation.PoliciesByPlan.TryAdd(id, plan.Policies);
            }

            foreach (var policyEntries in plan.Policies.GroupBy(entry => entry.PolicyName, StringComparer.Ordinal))
            {
                var bucket = generation.PlansByPolicy.GetOrAdd(
                    policyEntries.Key,
                    static _ => new ConcurrentDictionary<string, ImmutableHashSet<ulong>>(
                        StringComparer.Ordinal));

                if (bucket.TryAdd(id, policyEntries.Select(entry => entry.RequirementHash).ToImmutableHashSet()))
                {
                    Interlocked.Increment(ref generation.IndexedIdCount);
                }
            }

            MaintainIndexSize(generation);
        }
    }

    /// <summary>
    /// Evicts every cached plan whose recorded requirement differs from a newly published policy
    /// requirement, leaving plans that reference none of those policies in place.
    /// </summary>
    public void EvictPolicies(IReadOnlyDictionary<string, ulong> requirementHashes)
    {
        lock (_mutationSync)
        {
            var generation = Volatile.Read(ref _generation);
            HashSet<string>? planIds = null;

            Interlocked.Increment(ref _version);

            try
            {
                foreach (var (name, requirementHash) in requirementHashes)
                {
                    if (!generation.PlansByPolicy.TryGetValue(name, out var indexedPlans))
                    {
                        continue;
                    }

                    foreach (var (id, recordedHashes) in indexedPlans)
                    {
                        if (recordedHashes.Any(recordedHash => recordedHash != requirementHash))
                        {
                            (planIds ??= new HashSet<string>(StringComparer.Ordinal)).Add(id);
                        }
                    }
                }

                var current = generation.Cache;

                if (planIds is not null)
                {
                    foreach (var id in planIds)
                    {
                        current.TryRemove(id);
                        RemovePlanFromIndex(generation, id);
                    }
                }
            }
            finally
            {
                Interlocked.Increment(ref _version);
            }
        }
    }

    /// <summary>
    /// Discards every cached plan by replacing the cache with an empty one of the same capacity.
    /// </summary>
    public void Reset()
    {
        lock (_mutationSync)
        {
            Interlocked.Increment(ref _version);

            try
            {
                Volatile.Write(ref _generation, new Generation(_capacity, _diagnostics));
            }
            finally
            {
                Interlocked.Increment(ref _version);
            }
        }
    }

    /// <summary>
    /// Periodically drops index entries for plans the underlying cache has already evicted by
    /// capacity, so that <c>_plansByPolicy</c> stays bounded even though the cache itself never
    /// signals eviction.
    /// </summary>
    /// <remarks>
    /// The <c>TryGet</c> probe used to detect survivors marks each of them as recently accessed
    /// as a side effect, which is an acceptable perturbation for a maintenance pass that only
    /// runs rarely.
    /// </remarks>
    private void MaintainIndexSize(Generation generation)
    {
        if (Interlocked.Read(ref generation.IndexedIdCount) <= (long)_capacity * MaxIndexedIdsMultiplier)
        {
            return;
        }

        foreach (var bucket in generation.PlansByPolicy.Values)
        {
            foreach (var id in bucket.Keys)
            {
                if (!generation.Cache.TryGet(id, out _))
                {
                    RemovePlanFromIndex(generation, id);
                }
            }
        }
    }

    /// <summary>
    /// The cache generation and version a plan was resolved against, captured before planning so
    /// that <see cref="Add"/> can detect a concurrent eviction that ran while planning was in
    /// flight.
    /// </summary>
    private static void DecrementIndexCount(Generation generation)
    {
        while (true)
        {
            var count = Interlocked.Read(ref generation.IndexedIdCount);

            if (count == 0
                || Interlocked.CompareExchange(ref generation.IndexedIdCount, count - 1, count) == count)
            {
                return;
            }
        }
    }

    private static void RemovePlanFromIndex(Generation generation, string id)
    {
        if (!generation.PoliciesByPlan.TryRemove(id, out var entries))
        {
            return;
        }

        foreach (var name in entries.Select(entry => entry.PolicyName).Distinct(StringComparer.Ordinal))
        {
            if (generation.PlansByPolicy.TryGetValue(name, out var indexedPlans)
                && indexedPlans.TryRemove(id, out _))
            {
                DecrementIndexCount(generation);
            }
        }
    }

    internal sealed class Generation
    {
        public Generation(int capacity, CacheDiagnostics? diagnostics)
        {
            Cache = new Cache<OperationPlan>(capacity, diagnostics);
            PlansByPolicy = new ConcurrentDictionary<
                string,
                ConcurrentDictionary<string, ImmutableHashSet<ulong>>>(
                StringComparer.Ordinal);
            PoliciesByPlan = new ConcurrentDictionary<string, ImmutableArray<PolicyPlanEntry>>(
                StringComparer.Ordinal);
        }

        public Cache<OperationPlan> Cache { get; }

        public ConcurrentDictionary<
            string,
            ConcurrentDictionary<string, ImmutableHashSet<ulong>>> PlansByPolicy { get; }

        public ConcurrentDictionary<string, ImmutableArray<PolicyPlanEntry>> PoliciesByPlan { get; }

        public long IndexedIdCount;
    }

    public readonly struct PlanCacheSession
    {
        internal PlanCacheSession(Generation generation, long version)
        {
            Generation = generation;
            Version = version;
        }

        internal Generation Generation { get; }

        public Cache<OperationPlan> Cache => Generation.Cache;

        public long Version { get; }
    }
}
