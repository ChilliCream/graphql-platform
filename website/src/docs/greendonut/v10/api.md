---
title: IDataLoader<TKey, TValue>
---

**Description:** Represents a minimal set of _DataLoader_ functionality.

# Events

---

## RequestBuffered

**Description:** Raises when an incoming data request is added to the buffer.
Will never be raised if batching is disabled.

---

# Properties

---

## BufferedRequests

**Description:** Gets the current count of buffered data requests waiting for
being dispatched as batches. Will always return `0` if batching is disabled.

**Return Value:** `int`

---

## CachedValues

**Description:** Gets the current count of cached values. Will always return `0`
if caching is disabled.

**Return Value:** `int`

---

# Methods

---

## Clear()

**Description:** Empties the complete cache.

**Return Value:** `void`

---

## DispatchAsync()

**Description:** Dispatches one or more batch requests. In case of auto
dispatching we just trigger an implicit dispatch which could mean to interrupt a
wait delay. Whereas in a manual dispatch scenario it could mean to dispatch
explicitly.

**Return Value:** `Task`

---

## LoadAsync(TKey key)

**Description:** Loads a single value by key. This call may return a cached
value or enqueues this single request for batching if enabled.

**Return Value:** `Task<TValue>`

---

## LoadAsync(params TKey[] keys)

**Description:** Loads multiple values by keys. This call may return cached
values and enqueues requests which were not cached for batching if enabled.

**Return Value:** `Task<IReadOnlyList<TValue>>`

---

## LoadAsync(IReadOnlyCollection<TKey> keys)

**Description:** Loads multiple values by keys. This call may return cached
values and enqueues requests which were not cached for batching if enabled.

**Return Value:** `Task<IReadOnlyList<TValue>>`

---

## Remove(TKey key)

**Description:** Removes a single entry from the cache.

**Return Value:** `void`

---

## Set(TKey key, TValue value)

**Description:** Adds a new entry to the cache if not already exists.

**Return Value:** `void`

---

## Set(TKey key, Task<TValue> value)

**Description:** Adds a new entry to the cache if not already exists.

**Return Value:** `void`
