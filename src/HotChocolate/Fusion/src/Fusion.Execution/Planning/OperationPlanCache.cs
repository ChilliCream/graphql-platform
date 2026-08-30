using System.Collections.Concurrent;
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

    // Maps a policy name to the ids of the currently cached plans that reference it, so that a
    // requirement change can evict precisely those plans without enumerating the whole cache.
    // Kept in the same generation as _current: Reset() clears it together with the cache it
    // indexes.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _plansByPolicy =
        new(StringComparer.Ordinal);

    // Approximate count of ids currently indexed across all buckets in _plansByPolicy. Used only
    // to decide when a maintenance sweep is due, never to gate correctness.
    private long _indexedIdCount;

    // Incremented before any removal/swap in EvictPolicies or Reset. Add() reads it before and
    // after writing the cache and the index; a mismatch means an eviction ran concurrently and
    // the just-added plan must be undone instead of staying reachable under stale requirements.
    private long _version;

    private Cache<OperationPlan> _current;

    public OperationPlanCache(int capacity, CacheDiagnostics? diagnostics)
    {
        _capacity = capacity;
        _diagnostics = diagnostics;
        _current = new Cache<OperationPlan>(capacity, diagnostics);
    }

    /// <summary>
    /// Gets the operation plan cache currently in effect.
    /// </summary>
    public Cache<OperationPlan> Current => Volatile.Read(ref _current);

    /// <summary>
    /// Test-only accessor for the approximate indexed id count, so tests can assert on index
    /// bookkeeping without relying on internal timing.
    /// </summary>
    internal long IndexedIdCountForTesting => Interlocked.Read(ref _indexedIdCount);

    /// <summary>
    /// Captures the cache generation and version currently in effect, for a caller to plan
    /// against and later pass to <see cref="Add"/>.
    /// </summary>
    /// <remarks>
    /// The version is read before the cache reference so that an eviction racing with this call
    /// is never missed: if it lands after this read, either it has not yet touched the cache the
    /// caller is about to plan against (an ABA the version bump forces <see cref="Add"/> to
    /// notice), or it already produced the cache reference this call then observes.
    /// </remarks>
    public PlanCacheSession Capture()
    {
        var version = Volatile.Read(ref _version);
        var cache = Current;
        return new PlanCacheSession(cache, version);
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

        var cache = session.Cache;

        cache.TryAdd(id, plan);

        foreach (var entry in plan.Policies)
        {
            var bucket = _plansByPolicy.GetOrAdd(
                entry.PolicyName,
                static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

            if (bucket.TryAdd(id, 0))
            {
                Interlocked.Increment(ref _indexedIdCount);
            }
        }

        // Re-check after writing. An eviction that ran before the writes above already missed
        // this plan's id (it was not indexed yet) and left it reachable under a stale
        // requirement, so it must be undone here. An eviction that runs after this point either
        // has not yet observed the id (harmless, the entry stays and is correct for the new
        // requirements too) or observes it and evicts normally through EvictPolicies.
        if (Volatile.Read(ref _version) != session.Version)
        {
            cache.TryRemove(id);

            foreach (var entry in plan.Policies)
            {
                if (_plansByPolicy.TryGetValue(entry.PolicyName, out var planIds)
                    && planIds.TryRemove(id, out _))
                {
                    Interlocked.Decrement(ref _indexedIdCount);
                }
            }

            return;
        }

        MaintainIndexSize(cache);
    }

    /// <summary>
    /// Evicts every cached plan that references one of the given policy names, leaving plans
    /// that reference none of them in place.
    /// </summary>
    public void EvictPolicies(IReadOnlyCollection<string> policyNames)
    {
        Interlocked.Increment(ref _version);

        var current = Current;

        foreach (var name in policyNames)
        {
            if (_plansByPolicy.TryRemove(name, out var planIds))
            {
                foreach (var id in planIds.Keys)
                {
                    current.TryRemove(id);
                    Interlocked.Decrement(ref _indexedIdCount);
                }
            }
        }
    }

    /// <summary>
    /// Discards every cached plan by replacing the cache with an empty one of the same capacity.
    /// </summary>
    public void Reset()
    {
        Interlocked.Increment(ref _version);

        Volatile.Write(ref _current, new Cache<OperationPlan>(_capacity, _diagnostics));
        _plansByPolicy.Clear();
        Volatile.Write(ref _indexedIdCount, 0);
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
    private void MaintainIndexSize(Cache<OperationPlan> cache)
    {
        if (Interlocked.Read(ref _indexedIdCount) <= (long)_capacity * MaxIndexedIdsMultiplier)
        {
            return;
        }

        foreach (var bucket in _plansByPolicy.Values)
        {
            foreach (var id in bucket.Keys)
            {
                if (!cache.TryGet(id, out _) && bucket.TryRemove(id, out _))
                {
                    Interlocked.Decrement(ref _indexedIdCount);
                }
            }
        }
    }

    /// <summary>
    /// The cache generation and version a plan was resolved against, captured before planning so
    /// that <see cref="Add"/> can detect a concurrent eviction that ran while planning was in
    /// flight.
    /// </summary>
    public readonly struct PlanCacheSession(Cache<OperationPlan> cache, long version)
    {
        public Cache<OperationPlan> Cache { get; } = cache;

        public long Version { get; } = version;
    }
}
