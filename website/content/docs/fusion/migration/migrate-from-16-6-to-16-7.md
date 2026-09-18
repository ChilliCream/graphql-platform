---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: implement the new WebSocket connection initialization diagnostic event, replace raw condition masks with ConditionFlags, and configure wide operation limits."
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
