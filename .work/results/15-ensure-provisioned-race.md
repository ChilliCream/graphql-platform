# 15 — `EnsureProvisionedAsync` Race Condition

## TL;DR

`AzureServiceBusDispatchEndpoint.EnsureProvisionedAsync` flips the
`_isProvisioned` flag to `1` **before** the asynchronous provisioning work
completes. Concurrent dispatchers observe `1`, fall through, and start
calling `SendMessageAsync` against an entity that may not yet exist (or that
will never exist if the leader's `ProvisionAsync` faults). The fix is the
classic single-flight pattern: cache the in-flight `Task`, have everyone
await the same task, and *replace* (not just clear) the cached task on
failure so the next caller can retry without poisoning concurrent waiters.

---

## 1. The bug

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs`, lines 198–232.

```csharp
private int _isProvisioned; // 0 = false, 1 = true

private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
{
    if (Volatile.Read(ref _isProvisioned) == 1)
    {
        return;                                      // (A) fast path
    }

    // Only one thread provisions
    if (Interlocked.CompareExchange(ref _isProvisioned, 1, 0) != 0)
    {
        return;                                      // (B) "someone else is doing it" — but they may not be done!
    }

    try
    {
        var autoProvision = ((AzureServiceBusMessagingTopology)transport.Topology).AutoProvision;

        if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
        {
            await Queue.ProvisionAsync(transport.ClientManager, cancellationToken);
        }

        if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
        {
            await Topic.ProvisionAsync(transport.ClientManager, cancellationToken);
        }
    }
    catch
    {
        Volatile.Write(ref _isProvisioned, 0); // Reset on failure
        throw;
    }
}
```

### Why it is wrong

Two interleavings, both observable:

**Interleaving 1 — "leader still working, follower sends to non-existent entity"**

```
T1: CAS(_isProvisioned, 1, 0) -> wins, value is now 1
T1: starts await Queue.ProvisionAsync(...)        // network I/O, multiple seconds
T2: Volatile.Read(_isProvisioned) == 1 -> returns from (A)
T2: clientManager.GetSender(entityPath).SendMessageAsync(...)
     -> 404 / MessagingEntityNotFound, depending on the ASB SDK and broker timing
```

**Interleaving 2 — "leader fails, in-flight follower is already past the gate"**

```
T1: CAS wins, starts ProvisionAsync, faults (transient ARM throttling, perms, ...)
T2: enters between T1's CAS and T1's catch, sees 1, returns from (A) or (B)
T2: SendMessageAsync proceeds against the missing entity
T1: catch -> Volatile.Write(_isProvisioned, 0)
T3: arrives, sees 0, becomes the new leader, retries provisioning
    -- meanwhile T2 has already failed for the wrong reason
```

The Postel-grade summary: the flag is being used as both "provisioning has
*started*" and "provisioning has *finished*", but those are two distinct
states. The current code conflates them. The downstream `ServiceBusSender`
call has no idempotency back-stop, so the race surfaces as intermittent
`MessagingEntityNotFoundException` under burst load — exactly the kind of
heisenbug that survives unit tests.

(Side note: `RabbitMQDispatchEndpoint.EnsureProvisionedAsync` at lines
146–168 has the same bug *without* even the half-hearted CAS guard. It uses
a plain unsynchronized `bool _isProvisioned`. Same fix applies — out of
scope for this ticket but worth a follow-up.)

---

## 2. Options considered

| Option | Single-flight? | Retry on failure? | Cancellation-safe? | Verdict |
|---|---|---|---|---|
| `Lazy<Task>` (`ExecutionAndPublication`) | Yes | **No** — faulted Task stays cached forever | Yes | Wrong |
| `Lazy<Task>` + replace on failure | Yes | Yes (with manual nulling) | Yes | Workable but awkward |
| `AsyncLazy<T>` (Stephen Toub) | Yes | No (same caveat as `Lazy<Task>`) | Yes | Same problem as #1 |
| `TaskCompletionSource` + `Interlocked.CompareExchange` | Yes | Yes | Yes (decouple from caller token) | **Recommended** |
| `SemaphoreSlim` + double-check | Yes | Yes | Tricky — first caller's cancel skips work | Heaviest, no benefit here |

`Lazy<Task>` is the wrong tool here precisely because the `Lazy` will
forever return a *faulted* Task once provisioning fails — every subsequent
dispatch would re-throw the original `ServiceBusException` instead of
trying again. Wrapping it in "replace the Lazy on fault" works but is just
a clumsier version of option 4.

The codebase already has a clean precedent for the recommended pattern in
`OperationCacheMiddleware` (uses `Lazy<TaskCompletionSource<T>>` over a
`ConcurrentDictionary`). Here we need only a single slot, not a dictionary,
so the field-level CAS variant is simpler and avoids the dictionary
allocation per endpoint.

---

## 3. Failure & cancellation semantics — the things easy to get wrong

1. **Faulted shared task must be replaced, not awaited forever.** A naive
   "store one `Task`" cache fails closed: every future caller gets a
   re-thrown exception. We swap a fresh leader slot back in *before*
   completing the failed TCS, so the very next caller sees `null` and
   becomes the new leader.
2. **The leader must not honour the per-caller `CancellationToken` when
   running the shared work.** If T1 starts provisioning with `tokenA` and
   T1's caller cancels `tokenA`, the shared task would be cancelled out
   from under T2/T3, who never asked for that. Pass `CancellationToken.None`
   into `ProvisionAsync` and let each caller wait on the shared task with
   its own token via `WaitAsync(cancellationToken)`. Cancelled callers
   simply walk away; the leader keeps working for the others.
3. **Idempotent provisioning.** `AzureServiceBusQueue.ProvisionAsync` and
   friends already swallow "already exists" via
   `ServiceBusFailureReason.MessagingEntityAlreadyExists` (verify before
   merging — see below), so re-running provisioning after a transient
   failure is safe.
4. **Memory model.** The cached field is read on the hot path. The CAS in
   `Interlocked.CompareExchange<TaskCompletionSource<bool>?>` provides the
   needed full fence for both the publish *and* the consume side. No need
   for `Volatile.Read` once we use object-typed CAS.

---

## 4. Recommended pattern (drop-in replacement)

```csharp
// Holds the in-flight provisioning task. Null means "no leader yet"; a
// completed (RanToCompletion) TCS is the steady-state success cache; a
// faulted TCS is replaced atomically by the next caller before being
// observed as a result.
private TaskCompletionSource<bool>? _provisioningTcs;

private ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
{
    // Fast path: already provisioned successfully.
    var existing = Volatile.Read(ref _provisioningTcs);
    if (existing is { Task.IsCompletedSuccessfully: true })
    {
        return ValueTask.CompletedTask;
    }

    return new ValueTask(EnsureProvisionedSlowAsync(existing, cancellationToken));
}

private async Task EnsureProvisionedSlowAsync(
    TaskCompletionSource<bool>? observed,
    CancellationToken cancellationToken)
{
    while (true)
    {
        if (observed is null || observed.Task.IsFaulted || observed.Task.IsCanceled)
        {
            // Try to install a fresh leader slot. If `observed` was a faulted
            // TCS we replace it; if it was null we publish the first one.
            var candidate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var prior = Interlocked.CompareExchange(ref _provisioningTcs, candidate, observed);

            if (ReferenceEquals(prior, observed))
            {
                // We are the leader. Run with NO caller token so a cancelled
                // caller cannot poison the shared task for everyone else.
                try
                {
                    var autoProvision =
                        ((AzureServiceBusMessagingTopology)transport.Topology).AutoProvision;

                    if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
                    {
                        await Queue.ProvisionAsync(transport.ClientManager, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
                    {
                        await Topic.ProvisionAsync(transport.ClientManager, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    candidate.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    // Replace the faulted slot eagerly so the very next caller
                    // does not have to do CAS gymnastics — they'll just see
                    // `_provisioningTcs == null`, take the leader path, and
                    // retry. Doing this BEFORE TrySetException ensures no
                    // follower can observe the faulted task without seeing
                    // that the slot has already been freed for retry.
                    Interlocked.CompareExchange(ref _provisioningTcs, null, candidate);
                    candidate.TrySetException(ex);
                    throw;
                }

                return;
            }

            // Lost the race; fall through and await whoever won.
            observed = prior;
            continue;
        }

        // Wait for the leader, but only with our token. If we're cancelled
        // we throw OperationCanceledException without disturbing the leader.
        await observed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return;
    }
}
```

### Why this shape

- **Single CAS publishes the leader slot.** Followers either CAS-win
  (becoming the new leader after a fault) or read the existing TCS and
  await it. No double-checked locking, no `SemaphoreSlim`.
- **`WaitAsync(cancellationToken)` per follower.** A cancelled follower
  observes its own `OperationCanceledException`; the shared `Task` is
  untouched. The leader runs with `CancellationToken.None`.
- **Failure path replaces the slot atomically.** The
  `Interlocked.CompareExchange(ref _provisioningTcs, null, candidate)`
  step nulls the slot only if it still points at our (about-to-fault) TCS,
  which protects against a hypothetical successor that already moved past
  us — although in this design followers cannot replace a non-faulted,
  non-null leader slot, so this is belt-and-braces. After it returns we
  call `TrySetException`, so any followers already awaiting the TCS get
  the same exception — they will then re-enter the loop on the next
  dispatch attempt and find a `null` slot, becoming the new leader.
- **Hot-path allocation.** Only the slow path allocates the TCS, and only
  once per provisioning round. Steady state hits the
  `IsCompletedSuccessfully` early-out and is allocation-free
  (`ValueTask.CompletedTask`).
- **Same fix applies verbatim to `RabbitMQDispatchEndpoint`.** Different
  bug, identical shape — worth a follow-up.

### One-paragraph semantic summary

After a successful provisioning, the cached TCS stays put forever and the
fast path returns synchronously with zero allocations. While provisioning
is *in flight*, every caller awaits the same `Task`, with their own
cancellation token wired through `WaitAsync` — so a cancelled caller drops
out cleanly without disturbing the leader or other followers. If the
leader's provisioning **faults**, the slot is atomically reset to `null`
*before* the exception is published to followers, so the very next
dispatch attempt installs a fresh leader and retries. The shared work
itself runs under `CancellationToken.None`, which is what makes
"first-caller cancel" safe — without that, T1 cancelling its dispatch
would tear down T2's and T3's provisioning attempts as collateral damage.

---

## 5. Files referenced

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs` — buggy `EnsureProvisionedAsync` (lines 198–232)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQDispatchEndpoint.cs` — same bug, no CAS at all (lines 146–168) — follow-up
- `/Users/pascalsenn/kot/hc2/src/HotChocolate/Core/src/Types/Execution/Pipeline/OperationCacheMiddleware.cs` — existing in-codebase precedent for the single-flight pattern (uses `Lazy<TaskCompletionSource<T>>` over a `ConcurrentDictionary`)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs:124` — `ProvisionAsync` (verify it tolerates concurrent provisioning attempts and returns idempotently on `MessagingEntityAlreadyExists`)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusTopic.cs:95` — same as above for topics
