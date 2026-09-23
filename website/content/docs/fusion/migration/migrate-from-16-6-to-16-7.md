---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: account for default cost enforcement, implement the new WebSocket connection initialization diagnostic event, replace raw condition masks with ConditionFlags, and configure wide operation limits."
---

Update every Hot Chocolate Fusion package in the application to version 16.7 before applying these changes.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## IServerDiagnosticEvents gained a WebSocket connection initialization event

`IServerDiagnosticEvents` has a new `WebSocketConnectionInitialized` member. The gateway raises it once per WebSocket session, for both the `graphql-transport-ws` and the legacy `graphql-ws` protocol, after the client's connection initialization message has been accepted.

Listeners that derive from `ServerDiagnosticEventListener` need no change, because the base class provides a virtual no-op. Types that implement `IServerDiagnosticEvents` directly have to implement the new member:

```csharp
public void WebSocketConnectionInitialized(
    ISocketSession session,
    IOperationMessagePayload connectionInitMessage)
{
}
```

The payload of `connectionInitMessage` is only valid for the duration of the call. Read out any value that is needed later inside the callback, for example onto `ISocketConnection.Features`.

## Custom request pipelines must add the cost stages

All three predefined Fusion pipelines now coerce variables, check cost, and only then look up or plan the operation. Update custom pipelines to use the same relative order:

```diff
 builder
     // ... document cache and parser ...
     .UseDocumentValidation()
+    .UseOperationVariableCoercion()
+    .UseCostAnalysis()
     .UseOperationPlanCache()
-    .UseOperationPlan()
-    .UseSkipWarmupExecution()
-    .UseOperationVariableCoercion()
+    .UseOperationPlan()
+    .UseSkipWarmupExecution()
     .UseConcurrencyGate()
     .UseOperationExecution();
```

Document normalization, i.e. inlining fragments into the selected operation, is a lazy service, not a pipeline stage; `OperationVariableCoercion` asks for it on every request and `CostAnalysis` asks for it on a cost-plan cache miss, so nothing needs to be added for it. `OperationVariableCoercion` and `CostAnalysis` now both run before `OperationPlanCache`. Coercion errors therefore precede cost and planning errors, and a cost rejection precedes the operation-plan cache lookup: a rejected request never creates an operation-plan cache entry or an in-flight planning entry. `SkipWarmupExecution` is the only stage that checks whether a request is a warmup request; `OperationVariableCoercion` and `CostAnalysis` coerce and analyze a warmup request exactly like any other request before that stage stops it from executing. `GraphQL-Cost: validate` requests coerce variables exactly like `execute` and `report`, and fail with the ordinary coercion error when required variables are missing.

## Fusion diagnostic event interface expanded

Direct implementations of `IFusionExecutionDiagnosticEvents` or `IFusionExecutionDiagnosticEventListener` must implement these members:

```diff
 public interface IFusionExecutionDiagnosticEvents
 {
+    IDisposable AnalyzeOperationCost(RequestContext context);
+    void OperationCost(
+        RequestContext context,
+        double fieldCost,
+        double typeCost);
 }
```

`AnalyzeOperationCost` scopes cost analysis. `OperationCost` reports each evaluated field-cost and type-cost pair inside that scope.

`FusionExecutionDiagnosticEventListener` supplies implementations for both members, so subclasses do not require changes. `FusionActivityScopes.AnalyzeComplexity` now enables the cost-analysis activity span. It is included in `FusionActivityScopes.All`, but not in `FusionActivityScopes.Default`.

## Cost enforcement is enabled by default

Fusion now enforces a maximum field cost of `1,000` and a maximum type cost of `1,000` in every hosting environment. A request that exceeds either limit returns error code `HC0047` before operation planning.

Passing `disableDefaultSecurity: true` disables cost enforcement as part of disabling the gateway's default security. Cost analysis and `GraphQL-Cost` reporting remain available:

```diff
-services.AddGraphQLGatewayServer();
+services.AddGraphQLGatewayServer(disableDefaultSecurity: true);
```

## Default list size is Infinity, and is configured at composition time

The assumed size for a list field that carries no applicable `@listSize` information defaults to unbounded (`Infinity`). With default enforcement enabled, an unannotated, non-paginated composite list is rejected with `HC0047` and a `typeCost` of `"Infinity"`.

This default is a composition setting, not a gateway runtime option: `FusionCostOptions.DefaultListSize` no longer exists. Annotate the source field with `@listSize(assumedSize:)` so composition carries the bound into the composite directive, or set a finite default on the composer's `SourceSchemaMergerOptions.DefaultListSize`. When set, composition writes it onto the execution schema with a schema-level `@fusion__cost_options(defaultListSize:)` directive, and the gateway reads it from there:

```diff
 var options = new SchemaComposerOptions
 {
     Merger =
     {
+        DefaultListSize = 100
     }
 };
```

See [Composition](../composition.md#default-list-size) for details.

# Deprecations

## Raw condition masks replaced by ConditionFlags

Fusion can now compile and execute operations with more than 64 distinct `@skip`/`@include` conditions or `@defer` conditions. `MaxAllowedIncludeConditions` limits the combined `@skip` and `@include` conditions, and `MaxAllowedDeferConditions` limits the `@defer` conditions. Both limits default to **1,024**. An operation that exceeds either limit produces a GraphQL request error during operation compilation.

Configure the limits through `FusionRequestOptions` on the gateway:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyRequestOptions(options =>
    {
        options.MaxAllowedIncludeConditions = 2_048;
        options.MaxAllowedDeferConditions = 2_048;
    });
```

`ConditionFlags` contains the first 64 evaluated conditions and any remaining conditions. Pass the condition carriers from `OperationPlanContext` to the Fusion `Selection` overloads:

```diff
- bool included = selection.IsIncluded(context.IncludeFlags);
- bool deferred = selection.IsDeferred(context.DeferFlags);
+ bool included = selection.IsIncluded(context.IncludeConditionFlags);
+ bool deferred = selection.IsDeferred(context.DeferConditionFlags);
```

Replace every deprecated Fusion `Selection` overload as follows:

| Deprecated 16.6 member                                   | 16.7 replacement                                                  |
| -------------------------------------------------------- | ----------------------------------------------------------------- |
| `Selection.IsIncluded(ulong)`                            | `Selection.IsIncluded(ConditionFlags)`                            |
| `Selection.IsDeferred(ulong)`                            | `Selection.IsDeferred(ConditionFlags)`                            |
| `Selection.GetActiveDeliveryGroups(ulong)`               | `Selection.GetActiveDeliveryGroups(ConditionFlags)`               |
| `Selection.HasActiveDeliveryGroup(ulong, DeliveryGroup)` | `Selection.HasActiveDeliveryGroup(ConditionFlags, DeliveryGroup)` |

The deprecated raw overloads continue to work for operations with at most 64 conditions. When an operation has more than 64 conditions of the corresponding kind, the deprecated raw inclusion overloads throw `InvalidOperationException` for every conditional selection and the deprecated raw defer overloads throw for every deferrable selection, including selections whose own conditions are all among the first 64; raw inclusion evaluation does not throw for an unconditional selection, and raw defer evaluation does not throw for a non-deferrable selection. Releases before 16.7 rejected operations with more than 64 conditions during compilation.
