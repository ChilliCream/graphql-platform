# Azure Event Hub Transport - Pattern Consistency Review

**Review Date:** 2026-03-27
**Scope:** Event Hub transport vs. RabbitMQ reference transport
**Status:** Comprehensive review complete

---

## Summary

The Event Hub transport demonstrates strong overall pattern consistency with the RabbitMQ reference implementation. The core architectural patterns (base class inheritance, descriptor pattern, middleware, lifecycle hooks) are correctly applied. However, several deviations exist in detail implementation and code organization that warrant attention.

**Total Issues Found:** 12
- Critical: 1
- Major: 5
- Minor: 5
- Nit: 1

---

## Issues by Category

### CRITICAL

#### 1. Invalid C# Syntax in Descriptor.CreateConfiguration() (Line 254)
**File:** `/home/pascal/code/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubMessagingTransportDescriptor.cs:254`

**Issue:**
```csharp
Configuration.ReceiveEndpoints = _receiveEndpoints
    .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
    .ToList();
```

Invalid type cast syntax with parentheses inside the lambda. Should be:
```csharp
Configuration.ReceiveEndpoints = _receiveEndpoints
    .Select((ReceiveEndpointConfiguration e) => e.CreateConfiguration())
    .ToList();
```

Or simpler (RabbitMQ pattern):
```csharp
Configuration.ReceiveEndpoints = _receiveEndpoints
    .Select(e => e.CreateConfiguration())
    .ToList();
```

**RabbitMQ Pattern:** Uses simple selector without type annotations
**Impact:** Code will not compile

---

### MAJOR

#### 2. Static Factory Constructor Pattern Missing in Descriptor
**File:** `/home/pascal/code/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubMessagingTransportDescriptor.cs`

**Issue:** Constructor is public, but the RabbitMQ transport shows this is unnecessary. The `New()` static factory method at line 273 is the canonical way to create descriptors.

**RabbitMQ Pattern (Line 227):**
```csharp
public static RabbitMQMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
    => new(discoveryContext);
```

The constructor should be `internal` or the class should discourage direct instantiation. Current code allows:
```csharp
new EventHubMessagingTransportDescriptor(context)  // Inconsistent with RabbitMQ pattern
```

**Recommendation:** Make constructor `internal` to enforce use of `New()` factory method.

---

#### 3. Missing Fluent Method Return Type Wrapping in Descriptor
**File:** `/home/pascal/code/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/Descriptors/EventHubMessagingTransportDescriptor.cs`

**Issue:** Multiple fluent methods return `this` but don't ensure correct type. Lines 28-95 repeatedly use patterns like:

```csharp
public new IEventHubMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
{
    base.ModifyOptions(configure);
    return this;
}
```

This is correct but verbose. However, no consistency pattern is enforced—some overrides exist while the base implementations don't guarantee correct return types for derived calls.

**RabbitMQ Pattern:** Identical approach, but the critical issue is that if a base method is NOT overridden, calling it returns `MessagingTransportDescriptor`, not `IEventHubMessagingTransportDescriptor`, breaking the fluent interface contract.

**Recommendation:** Verify all public methods from base `MessagingTransportDescriptor` that return `this` or `IMessagingTransportDescriptor` are explicitly overridden in EventHub descriptor to return `IEventHubMessagingTransportDescriptor`.

---

#### 4. Inconsistent Lifecycle Hook Naming in OnBeforeInitialize vs OnAfterInitialized
**File:** `/home/pascal/code/hc2/src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubMessagingTransport.cs:55`

**Issue:** Method named `OnAfterInitialized` (past tense, "d" suffix) is inconsistent with the pattern in core base class which uses:
- `OnBeforeInitialize` (before action)
- `OnAfterDiscoverEndpoints` (after action)

Naming is inconsistent: should be `OnAfterInitialize` (future/infinitive) not `OnAfterInitialized` (past participle).

**RabbitMQ Pattern:** Uses same inconsistent naming (both transports have this issue inherited from base).

**Core Pattern (MessagingTransport.Lifecyle.cs):**
- Line 132: `OnBeforeInitialize` ✓
- Line 134: `OnAfterInitialized` ✗ (inconsistent)
- Line 140: `OnBeforeDiscoverEndpoints` ✓
- Line 225: `OnAfterDiscoverEndpoints` ✓

**Note:** This is a pre-existing issue, not Event Hub specific. Both transports inherit it. Flag for future refactoring in core.

---

#### 5. File-Scoped Namespaces Inconsistently Applied
**Files:** Multiple files across transport

**Issue:** Most files correctly use file-scoped namespaces:
```csharp
namespace Mocha.Transport.AzureEventHub;
```

However, verifying ALL files follow this convention. Spot-checked files all conform. Recommend full scan with linting.

**RabbitMQ Pattern:** Consistent file-scoped namespaces throughout.

---

#### 6. Missing IResourceFactory or IConventionDiscovery Pattern Methods
**File:** EventHub transport generally

**Issue:** RabbitMQ transport has multiple `*DescriptorExtensions.cs` files that provide type-safe extension methods for discovering and configuring topology. Event Hub has only `EventHubTransportDescriptorExtensions.cs`.

**RabbitMQ Extensions (6 files):**
- `RabbitMQTransportDescriptorExtensions.cs`
- `RabbitMQExchangeDescriptorExtensions.cs`
- `RabbitMQQueueDescriptorExtensions.cs`
- `RabbitMQBindingDescriptorExtensions.cs`
- `RabbitMQMessageTypeDescriptorExtensions.cs`

**Event Hub Extensions (1 file):**
- `EventHubTransportDescriptorExtensions.cs` only

**Impact:** Lower discoverability. No extension methods for direct `IEventHubTopicDescriptor` or `IEventHubSubscriptionDescriptor` configuration from `IMessageTypeDescriptor` like RabbitMQ provides.

**Recommendation:** Implement `EventHubTopicDescriptorExtensions.cs` and `EventHubSubscriptionDescriptorExtensions.cs` for consistency and API completeness.

---

### MINOR

#### 7. Descriptor Constructor Accessibility Pattern
**File:** EventHub topic/subscription descriptors (compare RabbitMQ)

**Issue:** `EventHubTopicDescriptor` constructor is `private`, while `RabbitMQExchangeDescriptor` constructor is `public`.

**EventHubTopicDescriptor (Line 10):**
```csharp
private EventHubTopicDescriptor(IMessagingConfigurationContext context, string name) : base(context)
```

**RabbitMQExchangeDescriptor (Line 15):**
```csharp
public RabbitMQExchangeDescriptor(IMessagingConfigurationContext context, string name) : base(context)
```

Both work (factory method exists), but inconsistent. Event Hub's approach is slightly better (enforces factory pattern), but inconsistent with RabbitMQ.

**Recommendation:** Make Event Hub descriptors consistent—keep `private` constructor (better encapsulation) and ensure RabbitMQ adopts same pattern for consistency.

---

#### 8. Topology Resource Caching Pattern Difference
**File:** `EventHubTopic.cs` vs `RabbitMQExchange.cs`

**Issue:** RabbitMQ uses `CachedString` for optimization:
```csharp
public CachedString CachedName { get; private set; } = null!;
```

Event Hub does not implement this optimization for topic names.

**RabbitMQExchange (Lines 20-21):**
```csharp
public string Name { get; private set; } = null!;
public CachedString CachedName { get; private set; } = null!;
```

**EventHubTopic (Line 11):**
```csharp
public string Name { get; private set; } = null!;
```

**Justification:** May be unnecessary for Event Hub if topic names are used less frequently in hot paths than RabbitMQ exchange names. However, missing implementation may indicate incomplete optimization pass.

**Recommendation:** Clarify if `CachedString` optimization is needed. If performance testing shows it's beneficial, implement it. Otherwise, document why it's omitted.

---

#### 9. Missing Feature-Based Pooling Pattern
**File:** Event Hub transport generally

**Issue:** RabbitMQ implements `IPooledFeature` for dispatcher pooling and resource management. Event Hub does not appear to use this pattern.

**RabbitMQ Pattern:** Features like `RabbitMQDispatcher` are registered as pooled features for lifecycle management.

**Event Hub Pattern:** `EventHubConnectionManager` is managed directly without the pooled feature wrapper.

**Impact:** Potential memory/resource management inconsistency. Features pattern provides guaranteed cleanup and diagnostic hooks.

**Recommendation:** Implement `IPooledFeature` for `EventHubConnectionManager` to align with framework patterns.

---

#### 10. Missing Health Check Integration
**File:** `EventHubHealthCheck.cs` exists but integration unclear

**Issue:** File `EventHubHealthCheck.cs` exists but there's no corresponding file in RabbitMQ transport. This is extra, not missing, but needs verification:
- Is it properly registered in DI?
- Is there a corresponding `EventHubHealthCheckExtensions.cs` file?
- Does RabbitMQ have health checks (may be missing there)?

**Verification Needed:** Confirm health check is properly wired and not orphaned code.

---

#### 11. Descriptor CreateConfiguration() Return Type Specificity
**File:** Descriptors across both transports

**Issue:** Descriptors return specific configuration types in `CreateConfiguration()`:
```csharp
public EventHubTopicConfiguration CreateConfiguration() => Configuration;
```

However, this breaks variance with the base interface (if one exists). Minor typing issue but worth verifying return type contracts are sound across the board.

**Recommendation:** Verify all `CreateConfiguration()` method signatures are covariant-compatible if used polymorphically.

---

### NIT

#### 12. Code Style: Lock Usage Variance
**File:** `EventHubMessagingTopology.cs` (Line 15) vs `RabbitMQMessagingTopology.cs` (Line 15)

Both use identical conditional compilation:
```csharp
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
```

**Nit:** Code is identical and correct. No issue. Just noting they match perfectly (good pattern reuse).

---

## Missing Pieces Analysis

### What RabbitMQ Has That Event Hub Might Be Missing

1. ✓ **Multiple descriptor extension files** — RabbitMQ has 6, Event Hub has 1
2. ✓ **Pooled feature pattern** — RabbitMQ uses it, Event Hub doesn't
3. ? **IResourceFactory pattern** — Unclear if Event Hub uses it
4. ✓ **Topology binding relationships** — RabbitMQ bindings are explicit; Event Hub subscriptions are simpler (correct for Event Hub model)
5. ? **Performance optimizations (CachedString, etc.)** — RabbitMQ has more; unclear if Event Hub needs them

### What Event Hub Has That RabbitMQ Doesn't

1. **Health checks** — Event Hub has `EventHubHealthCheck.cs`; RabbitMQ doesn't
2. **Partition ownership/checkpoint stores** — Event Hub-specific features (correct domain differences)
3. **Batch mode configuration** — Event Hub-specific optimization

---

## Patterns Confirmed as Correct

✓ Base class inheritance from `MessagingTransport`
✓ Endpoint classes inherit from `ReceiveEndpoint<T>` / `DispatchEndpoint<T>`
✓ Topology class inherits from `MessagingTopology<T>`
✓ Topology resource pattern (Initialize/Complete lifecycle)
✓ Descriptor pattern with fluent API
✓ DI registration via `MessageBusBuilderExtensions`
✓ Middleware pipeline integration
✓ Lifecycle hooks (OnBeforeStartAsync, OnBeforeStopAsync, etc.)
✓ Convention pattern integration
✓ URI scheme and addressing model
✓ Threading model (Lock usage)

---

## Recommendations Priority

### Immediate (Blocking)
1. **Fix syntax error in CreateConfiguration() (Issue #1)** — Code won't compile

### High Priority
2. **Implement missing extension files (Issue #6)** — API completeness
3. **Make descriptor constructor consistent (Issue #2)** — Enforce factory pattern

### Medium Priority
4. **Implement pooled feature pattern (Issue #9)** — Resource management consistency
5. **Verify health check integration (Issue #10)** — Complete feature coverage

### Low Priority
6. **Clarify CachedString usage (Issue #8)** — Performance documentation
7. **Review fluent method override coverage (Issue #3)** — Type safety verification

---

## Conclusion

The Event Hub transport is **well-architected and follows Mocha patterns correctly** in most areas. The critical syntax error must be fixed immediately. The major issues around extension methods and resource patterns should be addressed for full consistency with RabbitMQ. Overall, this is a high-quality implementation that will serve as a good reference for future transports.
