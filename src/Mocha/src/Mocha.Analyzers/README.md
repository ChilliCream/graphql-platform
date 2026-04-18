# Mocha Messaging Source Generator

Incremental Roslyn source generator that discovers messaging handlers, sagas, and call-sites from a compilation and emits a dependency injection registration method (`Add{Module}()`) for the message bus.

## Table of Contents

- [Module Declaration](#module-declaration)
- [Discovery Pipeline](#discovery-pipeline)
- [Handler Discovery](#handler-discovery)
- [Saga Discovery](#saga-discovery)
- [Call-Site Discovery](#call-site-discovery)
- [Imported Module Discovery](#imported-module-discovery)
- [Code Generation](#code-generation)
- [AOT Mode](#aot-mode)
- [Validation & Diagnostics](#validation--diagnostics)
- [Cross-Module System](#cross-module-system)

---

## Module Declaration

A module is declared via an assembly-level attribute:

```csharp
[assembly: MessagingModule("OrderService")]
```

With AOT support:

```csharp
[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]
```

- **Module name** is required and determines the generated method name (`Add{ModuleName}`).
- **JsonContext** is optional. When set, the generator enters AOT mode and emits pre-built serializer registrations.
- Only one `[MessagingModule]` per assembly is supported.

---

## Discovery Pipeline

The generator runs two parallel incremental pipelines:

### Pipeline 1: Handler & Saga Discovery

Syntax predicate filters classes/records with Mocha base types in their base list, then inspectors run in priority order:

1. **MessagingHandlerInspector** - concrete handlers implementing messaging interfaces
2. **AbstractMessagingHandlerInspector** - abstract handlers (diagnostic-only, MO0013)
3. **MessagingModuleInspector** - `[assembly: MessagingModule(...)]` attributes
4. **SagaInspector** - `Saga<TState>` subclasses

### Pipeline 2: Call-Site & Import Discovery

Syntax predicate filters invocation expressions, then:

1. **CallSiteMessageTypeInspector** - dispatch calls on `IMessageBus`, `ISender`, `IPublisher`
2. **ImportedModuleTypeInspector** - calls to methods annotated with `[MessagingModuleInfo]`

Both pipelines feed into the `Execute` method which validates, augments, and generates code.

---

## Handler Discovery

Concrete (non-abstract, non-generic) classes or records implementing messaging interfaces are discovered. The inspector checks interfaces in a **priority cascade** - first match wins:

| Priority | Interface | Kind | Response |
|----------|-----------|------|----------|
| 1 | `IBatchEventHandler<T>` | Batch | No |
| 2 | `IConsumer<T>` | Consumer | No |
| 3 | `IEventRequestHandler<T, TResponse>` | RequestResponse | Yes |
| 4 | `IEventRequestHandler<T>` | Send | No |
| 5 | `IEventHandler<T>` | Event | No |

For each discovered handler, the full **type hierarchy** of the message type is captured (all base types excluding `object`, plus all interfaces). This hierarchy is used for enclosed type computation in AOT mode.

---

## Saga Discovery

Classes (not records) that inherit from `Saga<TState>` are discovered. The inspector:

1. Walks the base type chain to find `Saga<TState>` and extracts `TState`.
2. Validates that the saga has a **public parameterless constructor** (required for instantiation). Reports MO0014 if missing.

---

## Call-Site Discovery

Invocations on `IMessageBus`, `ISender`, and `IPublisher` are inspected to discover message types used at call sites. These are used for validation only (no code generation).

### IMessageBus Methods

| Method | CallSiteKind | Type Extraction |
|--------|-------------|-----------------|
| `PublishAsync<T>` | Publish | Type argument |
| `SendAsync<T>` | Send | Type argument |
| `SchedulePublishAsync<T>` | SchedulePublish | Type argument |
| `ScheduleSendAsync<T>` | ScheduleSend | Type argument |
| `RequestAsync<TResponse>` | Request | First argument type + type argument for response |
| `RequestAsync` (non-generic) | Request | First argument type (fallback) |

### ISender Methods (Mediator)

| Method | CallSiteKind | Type Extraction |
|--------|-------------|-----------------|
| `SendAsync` | MediatorSend | First argument type |
| `QueryAsync` | MediatorQuery | First argument type |

### IPublisher Methods (Mediator)

| Method | CallSiteKind | Type Extraction |
|--------|-------------|-----------------|
| `PublishAsync<T>` | MediatorPublish | Type argument |

**Mediator call sites are excluded from JSON validation** because mediator dispatch is in-process and does not require serialization.

---

## Imported Module Discovery

When code calls a method annotated with `[MessagingModuleInfo]` (e.g., `builder.AddOrders()`), the inspector reads the `MessageTypes` array from the attribute. These types are treated as "already registered" and:

- Excluded from local serializer registration (no duplicate `AddMessageConfiguration`)
- Counted as "covered" in AOT validation (MO0015, MO0016, MO0018)

---

## Code Generation

The generator emits a single extension method on `IMessageBusHostBuilder`:

```csharp
namespace Microsoft.Extensions.DependencyInjection
{
    public static class {Module}MessageBusBuilderExtensions
    {
        [MessagingModuleInfo(MessageTypes = new Type[] { ... })]
        public static IMessageBusHostBuilder Add{Module}(
            this IMessageBusHostBuilder builder)
        {
            // registrations
            return builder;
        }
    }
}
```

### Registration Order

Registrations are emitted in this order:

1. **AOT Configuration** (if JsonContext specified)
   - `ModifyOptions(builder, o => o.IsAotCompatible = true)`
   - `AddJsonTypeInfoResolver(builder, {JsonContext}.Default)`
2. **Message Type Serializers** - `AddMessageConfiguration` per type
3. **Saga Configuration** - `AddSagaConfiguration<TSaga>` with state serializer
4. **Batch Handlers** - sorted by handler type name
5. **Consumers** - sorted by handler type name
6. **Request Handlers** (RequestResponse + Send) - sorted by handler type name
7. **Event Handlers** - sorted by handler type name
8. **Saga Registrations** - `AddSaga<TSaga>`

### Handler Registration

Each handler emits `AddHandlerConfiguration<THandler>` with a factory:

| Kind | Factory |
|------|---------|
| Event | `ConsumerFactory.Subscribe<THandler, TMessage>()` |
| Send | `ConsumerFactory.Send<THandler, TMessage>()` |
| RequestResponse | `ConsumerFactory.Request<THandler, TMessage, TResponse>()` |
| Consumer | `ConsumerFactory.Consume<THandler, TMessage>()` |
| Batch | `ConsumerFactory.Batch<THandler, TMessage>()` |

### `[MessagingModuleInfo]` Attribute Population

The `MessageTypes` array on the generated method contains **only types that receive `AddMessageConfiguration` calls** in the method body. This means:

- Only types present in the **local** `JsonSerializerContext` (not from referenced assemblies)
- Excluding types already covered by imported modules
- Including context-only types (types in the JsonContext without handlers)
- **Empty** when no JsonContext is specified (non-AOT mode)

This ensures importing modules know exactly which types have serializer registrations from this module.

---

## AOT Mode

AOT mode has two independent aspects controlled by different settings:

- **`JsonContext` on `[MessagingModule]`** - controls **code generation**: serializer registrations, strict mode, and resolver registration are only emitted when a `JsonContext` is specified.
- **`PublishAot` MSBuild property** - controls **validation strictness**: when `true`, diagnostics MO0015/MO0016/MO0018 fire even without a local `JsonContext`.

Validation diagnostics fire when **either** condition is true. Serializer code generation requires `JsonContext`.

### What changes when JsonContext is specified

1. **Serializer registrations are emitted** - `AddMessageConfiguration` with pre-built `JsonMessageSerializer` for each message type in the local JsonContext.

2. **Strict mode is enabled** - `IsAotCompatible = true` on builder options.

3. **JsonTypeInfoResolver is registered** - the specified `JsonSerializerContext` is added as a resolver.

4. **Validation diagnostics fire** - MO0015, MO0016, MO0018 are checked.

### Which types get serializer registrations

A type gets an `AddMessageConfiguration` call if **all** of these are true:
- It is declared as `[JsonSerializable(typeof(T))]` on the **local** `JsonSerializerContext`
- It is NOT already imported from a referenced module
- It is either a handler message/response type OR a context-only type

### Context-Only Types

Types declared in the `JsonSerializerContext` that have no corresponding handler or saga in the current assembly still receive `AddMessageConfiguration` registrations. These are types the module needs to serialize but doesn't consume.

### Enclosed Types

For each message type registration, the generator computes an "enclosed types" array from the type hierarchy. If multiple registered types share a hierarchy (e.g., `OrderUpdated : Order`), enclosed types are sorted by specificity - most specific types first. This supports polymorphic serialization.

---

## Validation & Diagnostics

### MO0011 - Duplicate Request Handler (Error)

**Fires when:** Multiple handlers exist for the same message type with `Send` or `RequestResponse` kind.

**Example:** Two handlers both implement `IEventRequestHandler<CheckInventoryRequest, CheckInventoryResponse>`.

### MO0012 - Open Generic Handler (Info)

**Fires when:** A handler class has unbound type parameters (e.g., `class MyHandler<T> : IEventHandler<T>`).

**Reason:** The generator cannot register open generic handlers - concrete types are required.

### MO0013 - Abstract Handler (Warning)

**Fires when:** An abstract class implements a messaging interface.

**Reason:** Abstract classes cannot be instantiated and thus cannot be registered as handlers.

### MO0014 - Saga Missing Parameterless Constructor (Error)

**Fires when:** A `Saga<TState>` subclass does not have a `public` parameterless constructor.

**Reason:** The saga runtime requires `new()` to instantiate saga instances.

### MO0015 - Missing JsonSerializerContext (Error)

**Fires when (AOT mode):** The module has handlers or sagas with message types not fully covered by imported modules, but no `JsonContext` is specified on `[MessagingModule]`.

**Fix:** Add `JsonContext = typeof(MyJsonContext)` to the attribute.

### MO0016 - Missing JsonSerializable (Error)

**Fires when (AOT mode):** A handler message type, response type, or saga state type is not declared as `[JsonSerializable]` on the local `JsonSerializerContext` and not covered by imported modules.

**Fix:** Add `[JsonSerializable(typeof(MissingType))]` to the JsonContext class.

### MO0018 - Call-Site Type Not in JsonContext (Warning)

**Fires when (AOT mode):** A message type used in a dispatch call (`PublishAsync`, `SendAsync`, etc.) is not found in the local `JsonSerializerContext` or imported module types.

**Scope:** Only messaging dispatch calls - mediator dispatch (`ISender.SendAsync`, `ISender.QueryAsync`, `IPublisher.PublishAsync`) is excluded because it's in-process.

> **Note:** MO0017 is reserved and not currently used.

### When diagnostics fire

| Diagnostic | Condition |
|------------|-----------|
| MO0011 | Always (not AOT-gated) |
| MO0012 | Always (not AOT-gated) |
| MO0013 | Always (not AOT-gated) |
| MO0014 | Always (not AOT-gated) |
| MO0015 | `PublishAot == true` OR `JsonContext` is specified |
| MO0016 | `PublishAot == true` OR `JsonContext` is specified |
| MO0018 | `PublishAot == true` OR `JsonContext` is specified |

Handlers or sagas that carry a diagnostic (e.g., MO0012, MO0013, MO0014) are **excluded from code generation** - no `AddHandlerConfiguration` or `AddSagaConfiguration` is emitted for them. Only entries with zero diagnostics flow to the generator.

---

## Cross-Module System

The module system enables multiple assemblies to register their handlers independently while avoiding duplicate serializer registrations.

### How it works

1. **Module A** declares `[assembly: MessagingModule("Orders", JsonContext = typeof(OrdersJsonContext))]`
2. The generator emits `AddOrders()` with `[MessagingModuleInfo(MessageTypes = new[] { typeof(OrderCreated), ... })]`
3. **Module B** calls `builder.AddOrders()` in its code
4. The `ImportedModuleTypeInspector` reads the `[MessagingModuleInfo]` attribute and extracts the type names
5. Module B's generator skips serializer registration for imported types and excludes them from its own `[MessagingModuleInfo]`

### Validation with imports

- **MO0015:** If all handler types are covered by imports, no local JsonContext is needed
- **MO0016:** Imported types are considered "covered" - no local `[JsonSerializable]` needed
- **MO0018:** Imported types are considered "covered" at call sites

### Key constraint

The `[MessagingModuleInfo]` attribute only advertises types for which the module emits `AddMessageConfiguration` calls. Types that are handled but don't have local serializer support (not in the local JsonContext) are **not** included in the attribute. This prevents downstream modules from incorrectly assuming serialization is covered.
