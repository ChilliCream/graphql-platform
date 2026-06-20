# Error / Skip Queue Handling in Mocha: Integration Map, Coupling, and Transport-Feature Feasibility

READ-ONLY analysis. No code was changed. Every claim is cited as `file:line`. Paths are absolute under `/home/pascal/code/hc2`.

Scope note up front, because the scouts disagreed:
- The TODO quoted in the question ("could be a simple extension ... we JUST need an endpoint interceptor and a middleware") **does not exist anywhere in the current source**. Grepping the whole `src/Mocha` tree for `could be a simple extension`, `endpoint interceptor`, `interceptor and a middleware`, `simple extension` returns nothing. The only fault/skip-adjacent TODOs are two in `ReceiveFaultMiddleware.cs:64` and `:114` (poison-pill / error-endpoint-can-also-fail), unrelated to extraction.
- There is **no "interceptor" seam in Mocha core at all**. Every `interceptor` hit is either a test or EF Core `DbContext` interceptors in the persistence packages (outbox/scheduling). So the TODO's premise ("endpoint interceptor + middleware") references a seam that is not present. The real plug-in seam is the **convention registry** (`IReceiveEndpointConvention`) plus the per-transport `RoutingStrategy` override, not an interceptor.
- Scout B/C claimed `ReceiveDeadLetterMiddleware` uses `ErrorEndpoint` "instead of `SkippedEndpoint`". **Confirmed true** (`ReceiveDeadLetterMiddleware.cs:66` reads `context.Endpoint.ErrorEndpoint`), but the parameter is even named `errorEndpoint` and the class docs say "forwarded to the error endpoint". So unconsumed/unroutable messages go to the **error** endpoint, and `SkippedEndpoint` is currently dead at runtime (resolved at `ReceiveEndpoint.Complete()` but never read by any middleware). This is load-bearing for the feasibility verdict.

---

## 1. INTEGRATION-POINT MAP

Grouped by how hard the coupling is to remove. "core-structural" = the rest of the framework genuinely depends on the symbol existing. "core-incidental" = lives in core but only by habit; nothing structural needs it there. "per-transport" = duplicated in transport routing strategies. "runtime" = executes on the hot path.

### 1a. CORE-STRUCTURAL (load-bearing; framework cannot not-know about these)

| Symbol | file:line | Role |
|---|---|---|
| `ReceiveEndpointKind` enum (`Default/Error/Skipped/Reply`) | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpointKind.cs:6-27` | Classifies endpoints. `Error`/`Skipped` values gate topology discovery (see runtime/per-transport rows) and the satellite-vs-default branch in every routing strategy. |
| `ReceiveEndpoint.ErrorEndpoint` (`DispatchEndpoint?`) | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:83` | Runtime resolved dispatch target for faults; read by both middlewares' factory closures. |
| `ReceiveEndpoint.SkippedEndpoint` (`DispatchEndpoint?`) | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:92` | Resolved but currently never read at runtime (see 2 / Coupling). |
| `ReceiveEndpoint.Complete()` URI -> DispatchEndpoint resolution | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:237-245` | Turns the config URIs into live `DispatchEndpoint`s via `context.Endpoints.GetOrCreate`. |
| `ReceiveEndpoint.Initialize()` disable-flag application | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:169-177` | Nulls out `ErrorEndpoint`/`SkippedEndpoint` config URIs when the disabled flags are set. Core-level knowledge of the two fields. |

### 1b. CORE-INCIDENTAL (in core, but only by habit; nothing structural pins them here)

| Symbol | file:line | Role |
|---|---|---|
| `ReceiveEndpointConfiguration.ErrorEndpoint` / `IsErrorEndpointDisabled` | `src/Mocha/src/Mocha/Endpoints/Configurations/ReceiveEndpointConfiguration.cs:21,26` | Carries the error-queue URI + disable flag through config. Used only by error/skip code paths. |
| `ReceiveEndpointConfiguration.SkippedEndpoint` / `IsSkippedEndpointDisabled` | `src/Mocha/src/Mocha/Endpoints/Configurations/ReceiveEndpointConfiguration.cs:31,36` | Same, for skip. |
| `DefaultNamingConventions.ApplyEndpointKindSuffix` (`_error`, `_skipped`) | `src/Mocha/src/Mocha/Naming/DefaultNamingConventions.cs:215-225` (suffixes at `:220-221`) | Generates satellite queue names. Reachable via the three public `GetReceiveEndpointName` overloads (`:22,:51,:67`). |
| `MessageBusBuilderExtensions.AddDefaults` registers `DeadLetter` + `Fault` | `src/Mocha/src/Mocha/Builder/MessageBusBuilderExtensions.cs:39-40` | Unconditionally adds both middlewares to every receive pipeline, for every transport. |
| `ReceiveMiddlewares.DeadLetter` / `.Fault` registry entries | `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveMiddlewares.cs` (`DeadLetter`/`Fault` factory hooks) | Named middleware factory references. |
| Public fluent API: `ReceiveEndpointDescriptor.FaultEndpoint(address)` / `.SkippedEndpoint(address)` | `src/Mocha/src/Mocha/Endpoints/Descriptors/ReceiveEndpointDescriptor.cs:89-101` | Lets users set error/skip URIs by hand. Sets config fields directly. **Public surface** (scouts missed this). |

### 1c. PER-TRANSPORT (duplicated materialization)

| Symbol | file:line | Role |
|---|---|---|
| `RabbitMQRoutingStrategy.ConfigureEndpoint` | `src/Mocha/src/Mocha.Transport.RabbitMQ/Topology/RabbitMQRoutingStrategy.cs:182-209` | On `Kind == Default`, calls `ConfigureFaultOrSkippedEndpoint` for Error and Skipped, assigning config URIs. |
| `RabbitMQRoutingStrategy.ConfigureFaultOrSkippedEndpoint` | same file `:490-505` | Builds `rabbitmq:q/{name}_error|_skipped` via `context.Naming.GetReceiveEndpointName`. |
| `RabbitMQRoutingStrategy.DiscoverTopology` -> `EnsureFaultOrSkippedQueue` | same file `:237-245`, helper `:507-523` | Materializes the satellite queues in topology; inherits `AutoProvision` from parent (`:239`, `GetInheritedQueueAutoProvision` `:525`). |
| `PostgresRoutingStrategy.ConfigureEndpoint` | `src/Mocha/src/Mocha.Transport.Postgres/Topology/PostgresRoutingStrategy.cs:167-199` | Identical pattern to RabbitMQ (`:178-193`). |
| `PostgresRoutingStrategy.ConfigureFaultOrSkippedEndpoint` / `EnsureFaultOrSkippedQueue` | same file (`ConfigureFaultOrSkippedEndpoint` ~`:380`, `EnsureFaultOrSkippedQueue` ~`:397`) | Same naming, but does **not** propagate `AutoProvision` (divergence from RabbitMQ). |
| `InMemoryRoutingStrategy.ConfigureEndpoint` | `src/Mocha/src/Mocha.Transport.InMemory/Topology/InMemoryRoutingStrategy.cs:167-175` | **Does not** materialize error/skip at all (only sets `QueueName`). |
| Topology skip-guard for satellite kinds | RabbitMQ `:247-250`, InMemory `:199-202` | `if (Kind is Reply or Error or Skipped) return;` short-circuits binding discovery for satellite endpoints. Direct enum dependency. |
| Per-transport `QueueBuilder` error/skip setters | `RabbitMQQueueBuilder.cs:161-199`, `PostgresQueueBuilder.cs:147-185`, `InMemoryQueueBuilder.cs:124` | Public fluent helpers writing the same config fields. |
| Per-transport descriptor validation | `RabbitMQMessagingTransportDescriptor.cs:261-269`, `PostgresMessagingTransportDescriptor.cs:278-285`, `InMemoryMessagingTransportDescriptor.cs:252-257` | Reject an entity-only (no-consumer) endpoint that nonetheless has error/skip set: `ThrowHelper.FaultOrSkippedQueueRequiresConsumingEndpoint`. A real semantic rule tied to the fields. |

### 1d. RUNTIME (hot path)

| Symbol | file:line | Role |
|---|---|---|
| `ReceiveFaultMiddleware.InvokeAsync` | `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveFaultMiddleware.cs:26-53` | Catches exceptions from downstream; if `ResponseAddress` present -> negative-ack reply (`:44`), else -> `SendToErrorEndpointAsync` (`:48`); sets `MessageConsumed = true` (`:51`). |
| `ReceiveFaultMiddleware.SendToErrorEndpointAsync` | same file `:104-134` | Dispatches faulted envelope to `errorEndpoint`; no-op if `errorEndpoint is null` (`:109`); adds fault headers (`:126`). |
| `ReceiveFaultMiddleware.Create` (factory) | same file `:136-147` | Reads `context.Endpoint.ErrorEndpoint` (`:140`) at pipeline-compile time. |
| `ReceiveDeadLetterMiddleware.InvokeAsync` | `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveDeadLetterMiddleware.cs:22-60` | After `next`, if `!feature.MessageConsumed` -> dispatch to `errorEndpoint` (`:35-58`), then mark consumed. |
| `ReceiveDeadLetterMiddleware.Create` (factory) | same file `:62-77` | Reads `context.Endpoint.ErrorEndpoint` (`:66`); if null, returns `next` (skips itself, `:67-70`). **Uses ErrorEndpoint, not SkippedEndpoint.** |
| `ReceiveConsumerFeature.MessageConsumed` | `src/Mocha/src/Mocha/Features/ReceiveConsumerFeature.cs` (`MessageConsumed` flag) | The shared signal both middlewares branch on. Set true by `DefaultPipeline` when a consumer runs (`ReceiveEndpoint.cs:386`), by Expiry, and by the two error/skip middlewares themselves. |
| `RoutingMiddleware.InvokeAsync` | `src/Mocha/src/Mocha/Middlewares/Receive/RoutingMiddleware.cs:17-44` | Populates `feature.Consumers`. Empty set -> `MessageConsumed` stays false -> DeadLetter fires. |
| `DefaultPipeline` consumer loop | `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:375-393` | Runs consumers; sets `MessageConsumed = true` on success (`:386`). |

### 1e. ASCII flow (current behavior)

```
CONFIGURATION (build time)
  user fluent API: .FaultEndpoint(addr) / .SkippedEndpoint(addr)        (ReceiveEndpointDescriptor.cs:89-101)
        |  (or auto, when not set)
        v
  RoutingStrategy.ConfigureEndpoint  [Kind==Default only]              (RabbitMQ :182-209 / Postgres :167-199)
        |   -> ConfigureFaultOrSkippedEndpoint
        |        name = Naming.GetReceiveEndpointName(q, Error|Skipped)  (DefaultNamingConventions ApplyEndpointKindSuffix :215-225)
        |        cfg.ErrorEndpoint   ??= "{schema}:q/{name}_error"
        |        cfg.SkippedEndpoint ??= "{schema}:q/{name}_skipped"
        v
  ReceiveEndpoint.Initialize: apply IsErrorEndpointDisabled/IsSkippedEndpointDisabled  (ReceiveEndpoint.cs:169-177)

MATERIALIZATION (topology)
  RoutingStrategy.DiscoverTopology -> EnsureFaultOrSkippedQueue         (RabbitMQ :237-245 / :507-523)
        adds {q}_error, {q}_skipped queues   (InMemory: NONE :167-175)

  ReceiveEndpoint.Complete: URI -> DispatchEndpoint                     (ReceiveEndpoint.cs:237-245)
        ErrorEndpoint   = Endpoints.GetOrCreate(errorAddress)
        SkippedEndpoint = Endpoints.GetOrCreate(skippedAddress)   <-- resolved but unused at runtime
        compile pipeline (DeadLetter + Fault baked in)                  (AddDefaults :39-40)

RUNTIME (per message; pipeline order top->down)
  DeadLetter  --try--> Fault --try--> ... -> Routing -> DefaultPipeline(consumers)
       |                  |                                  |
       |                  |                                  +-- consumer ok -> MessageConsumed=true
       |                  +-- consumer throws (no ResponseAddress)
       |                  |       -> SendToErrorEndpointAsync -> ErrorEndpoint.ExecuteAsync  (Fault :104-134)
       |                  +-- consumer throws (ResponseAddress set)
       |                          -> negative-ack reply to ResponseAddress                  (Fault :55-102)
       +-- after next: if !MessageConsumed (no consumer matched / unroutable)
                  -> ErrorEndpoint.ExecuteAsync   (NOT SkippedEndpoint!)                    (DeadLetter :35-58)
```

---

## 2. COUPLING ASSESSMENT

### Genuinely CORE (cannot be removed without the framework noticing)

- **`ReceiveEndpointKind.Error` / `.Skipped` (enum values).** These are read structurally, not just by the error/skip feature: every routing strategy branches on `Kind == Default` to decide whether to even create satellites (`RabbitMQRoutingStrategy.cs:193`, `Postgres :178`), and the topology-discovery short-circuit `if (Kind is Reply or Error or Skipped) return;` (`RabbitMQ :247`, `InMemory :199`) is what stops a satellite queue from recursively spawning its own satellites and from getting consumer bindings. Removing the values breaks that guard. They are load-bearing.
- **The fault signal itself.** "A consumer threw" (caught in `ReceiveFaultMiddleware.cs:34`) and "nothing consumed this" (`ReceiveConsumerFeature.MessageConsumed == false`, branched at `ReceiveDeadLetterMiddleware.cs:35`) are transport-agnostic facts produced by the core pipeline (`DefaultPipeline` `:375-393`, `RoutingMiddleware`). No transport can produce these; they must stay in core. Any pluggable handler still has to consume this core signal.
- **URI -> `DispatchEndpoint` resolution** (`ReceiveEndpoint.Complete():237-245`). Turning a URI into a runnable dispatch target is a core capability (`context.Endpoints.GetOrCreate`). A feature can decide *which* URI, but the resolution machinery is core.

### In core/per-transport by HABIT (not structurally pinned)

- **The four `ReceiveEndpointConfiguration` fields** (`:21,26,31,36`). Nothing in core reads them except the disable-application in `Initialize()` (`:169-177`) and the resolution in `Complete()` (`:237-245`) - both of which are themselves error/skip-specific code, not general machinery. These could live in a feature object instead of on the base config type. Today they are populated by the transport routing strategies and the public descriptor; consumers of the *resolved* `DispatchEndpoint` are only the two middlewares.
- **`DeadLetter` + `Fault` always registered** (`AddDefaults:39-40`). Unconditional, transport-blind. There is no gate that says "only if this transport wants error queues." Making them conditional is purely additive.
- **`ApplyEndpointKindSuffix` `_error`/`_skipped`** (`DefaultNamingConventions.cs:220-221`). A naming detail living in the default naming convention. It is invoked only from the satellite path. Could move with the feature, though it shares the same method as `_reply` (which is structural for request-reply), so the method as a whole is not purely error/skip.
- **`ConfigureFaultOrSkippedEndpoint` duplicated in RabbitMQ and Postgres**, absent in InMemory. This is the clearest "by habit": near-identical code in two transports (only the `Transport.Schema` prefix differs), one transport omitting it, and Postgres silently dropping `AutoProvision` inheritance. That divergence is a bug-surface, not a design choice.

### Dead / inconsistent (relevant to feasibility)

- **`SkippedEndpoint` is resolved but never dispatched to.** `ReceiveEndpoint.Complete()` resolves it (`:242-245`), but no middleware reads `context.Endpoint.SkippedEndpoint`. `ReceiveDeadLetterMiddleware` - the one that handles "skipped/unroutable" - dispatches to `ErrorEndpoint` (`:66`). So the skip queue is materialized in topology and resolved, then unused at runtime. Any extraction work would have to decide whether to honor `SkippedEndpoint` or keep folding skip into error.

---

## 3. TRANSPORT-FEATURE FEASIBILITY

Mapping onto the seams that actually exist (verified):

Real seams present:
- **Convention registry**, type-keyed: `IConventionRegistry.GetConventions<TConvention>()` (`ConventionRegistry.cs:16`), `IReceiveEndpointConvention : IConfigurationConvention<ReceiveEndpointConfiguration>` (`IReceiveEndpointConvention.cs:6`), invoked at `ReceiveEndpoint.Initialize():168` via `Transport.Conventions.Configure(...)`. Conventions are registered per transport (`MessagingTransportConfiguration.Conventions` `:46`).
- **`RoutingStrategy.ConfigureEndpoint`/`DiscoverTopology`** are `virtual` no-ops on the base (`RoutingStrategy.cs:48-58, 73-85`); transports override. This is where materialization lives today.
- **Three middleware registration scopes**: bus (`MessageBusBuilder.UseReceive`), transport (`MessagingTransportDescriptor.UseReceive`), endpoint (`ReceiveEndpointDescriptor.UseReceive:103`), all with before/after ordering, flattened in `MiddlewareCompiler.CompileReceive`.
- **Feature bags** on `MessagingTransport.Features` (`MessagingTransport.cs:59`) and `ReceiveEndpoint.Features` (`ReceiveEndpoint.cs:97`); endpoint features are copied from config at `Initialize():182`.
- There is **no interceptor seam** (the TODO's term). The convention is the closest analog.

### What the feature would OWN

1. **Satellite queue declaration + URI generation** - move `ConfigureFaultOrSkippedEndpoint` + `EnsureFaultOrSkippedQueue` out of each `RoutingStrategy` into one shared `IReceiveEndpointConvention` (the error/skip convention), parameterized by `Transport.Schema` and the `AutoProvision` inheritance rule. This deduplicates RabbitMQ/Postgres and lets InMemory opt in or out by simply registering or not registering the convention.
2. **The move-to-error / move-to-skip middlewares** - `ReceiveFaultMiddleware` and `ReceiveDeadLetterMiddleware` already exist as standalone factories; the feature would register them (transport-scoped `UseReceive`) only when present, instead of `AddDefaults` doing it globally.
3. **The dispatch endpoints** - the resolved `ErrorEndpoint`/`SkippedEndpoint`. These could move from base-class properties into a feature object stored in `ReceiveEndpoint.Features`, read by the middleware factories via `context.Endpoint.Features.Get<ErrorSkipFeature>()` instead of `context.Endpoint.ErrorEndpoint`.

### Where it would PLUG IN

- Declaration/URI generation -> register an `IReceiveEndpointConvention` per transport (existing seam, `Initialize():168`). No new core type needed for this part.
- Topology materialization -> the convention can't reach `DiscoverTopology` (that's `RoutingStrategy`-only). Either keep a thin `DiscoverTopology` hook per transport that calls into shared feature code, or add the satellite queues from the convention by writing to the topology builder. This is the one spot that resists a pure-convention move (see blockers).
- Middleware -> transport-scoped `UseReceive` from the feature's transport descriptor wiring.

### What stays in CORE

- `ReceiveEndpointKind.Error/Skipped` (structural guard, section 2).
- The fault signal: exception capture and `MessageConsumed` (`ReceiveConsumerFeature`, `DefaultPipeline`, `RoutingMiddleware`).
- URI -> `DispatchEndpoint` resolution machinery (`Endpoints.GetOrCreate`).

### What concretely BLOCKS full extraction (no-core-change version)

1. **The two middlewares read base-class properties, not the feature bag.** `ReceiveFaultMiddleware.cs:140` and `ReceiveDeadLetterMiddleware.cs:66` both read `context.Endpoint.ErrorEndpoint`. As long as the resolved endpoints live as `ReceiveEndpoint.ErrorEndpoint/SkippedEndpoint` properties (`:83,92`) populated by core `Complete()` (`:237-245`), the feature is not self-contained: core still owns resolution and storage. A "no core change" feature can't relocate these reads. -> needs a small core seam: have the middlewares read from `Features`, and move resolution into the convention/feature.
2. **Topology materialization is `RoutingStrategy`-bound.** `EnsureFaultOrSkippedQueue` runs inside `DiscoverTopology` (`:206` calls `Transport.Routing.DiscoverTopology`), which has no convention equivalent. To fully extract you either add a topology-discovery convention hook (small core seam) or accept that each transport keeps a 2-line call into shared feature code.
3. **The disable-flag application is in core `Initialize()`** (`:169-177`), reading `IsErrorEndpointDisabled/IsSkippedEndpointDisabled`. If the fields move to a feature, this core logic must move too.
4. **Public API + validation are spread across transports.** `FaultEndpoint`/`SkippedEndpoint` fluent methods (`ReceiveEndpointDescriptor.cs:89-101`), the per-transport `QueueBuilder` setters, and the `FaultOrSkippedQueueRequiresConsumingEndpoint` validation (`RabbitMQMessagingTransportDescriptor.cs:261-269` and peers) all read/write the four config fields. Moving the fields into a feature is a breaking change to these call sites (in-repo, not external, but real).

Distinguishing the two flavors:
- **Could be a feature with NO core change:** only the *materialization* half (dedupe `ConfigureFaultOrSkippedEndpoint` into one shared `IReceiveEndpointConvention`, register per transport). This removes the duplication and lets a transport opt out, without touching core. It does NOT make runtime dispatch pluggable.
- **Needs a small core seam first:** making the *runtime dispatch* pluggable. The single seam is **"middleware reads error/skip target from `Features` instead of from `ReceiveEndpoint.ErrorEndpoint`/`SkippedEndpoint`, and resolution moves out of `Complete()`."** Everything else (conditional middleware registration, conventionized declaration) is additive once that seam exists.

---

## 4. VERDICT

**Feasible: yes, partially today and fully with one small core seam.**

- The **materialization** half can become a pluggable transport feature **with no core change**: collapse the duplicated `ConfigureFaultOrSkippedEndpoint`/`EnsureFaultOrSkippedQueue` (RabbitMQ `:490-523`, Postgres `~:380-405`) into one shared `IReceiveEndpointConvention` registered per transport via the existing convention registry (`Initialize():168`). This is the cheapest win and also fixes the InMemory gap and the Postgres `AutoProvision` divergence.
- The **runtime dispatch** half needs exactly **one core seam**: stop the two middlewares from reading `context.Endpoint.ErrorEndpoint/SkippedEndpoint` (`ReceiveFaultMiddleware.cs:140`, `ReceiveDeadLetterMiddleware.cs:66`) and instead read an error/skip feature from `ReceiveEndpoint.Features`; move the URI->endpoint resolution out of core `Complete():237-245` into that feature, and gate the `DeadLetter`/`Fault` registration (`AddDefaults:39-40`) on the feature being present. After that, the feature owns declaration + dispatch + endpoints, and `ReceiveEndpointKind` + the `MessageConsumed`/exception signal stay in core as the only required contract.

**Cheapest path if pursued:** start with the convention-based materialization dedupe (no core change, immediate duplication + divergence fix), then add the `Features`-based middleware lookup as the single follow-up core seam.

**Top 3 risks / unknowns:**
1. **Skip is already broken/dead.** `SkippedEndpoint` is resolved but never dispatched to; `DeadLetter` sends unconsumed messages to `ErrorEndpoint` (`:66`). Any extraction must decide whether to preserve this (skip == error) or fix it, and that decision is a behavior change, not a refactor.
2. **Breaking the public/config surface.** Moving the four fields off `ReceiveEndpointConfiguration` touches the fluent API (`FaultEndpoint`/`SkippedEndpoint`), all three transports' `QueueBuilder` setters, and the `FaultOrSkippedQueueRequiresConsumingEndpoint` validation. In-repo but wide.
3. **Topology hook gap.** Conventions run in `Initialize`, but satellite queue materialization runs in `RoutingStrategy.DiscoverTopology`, which has no convention equivalent. Full extraction either needs a new topology-discovery convention hook or leaves a thin per-transport shim.

**On the quoted RabbitMQ TODO:** it does not exist in the current code, and its proposed mechanism is off. There is no "endpoint interceptor" seam in Mocha; the analogous seam is the **convention registry**. The "+ middleware" half is already true (the middlewares exist as standalone factories). So the TODO's spirit is right (error/skip can be extracted), but its plan ("interceptor + middleware") should read **"convention + feature-bag-aware middleware + one core seam to relocate the endpoint lookup."**
