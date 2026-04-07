---
title: Attribute Reference
description: Comprehensive reference of all built-in attributes in Hot Chocolate v16 for configuring your GraphQL schema.
---

Hot Chocolate provides a wide range of attributes that you can apply to your .NET types, properties, methods, and parameters to configure the GraphQL schema. This page serves as a comprehensive reference.

# Root Type Attributes

These attributes mark a class as a root operation type for your schema.

| Attribute            | Namespace      | Description                                         |
| -------------------- | -------------- | --------------------------------------------------- |
| `[QueryType]`        | `HotChocolate` | Marks a static class as the root query type.        |
| `[MutationType]`     | `HotChocolate` | Marks a static class as the root mutation type.     |
| `[SubscriptionType]` | `HotChocolate` | Marks a static class as the root subscription type. |

# Type Configuration Attributes

These attributes control how .NET types map to GraphQL types.

| Attribute                       | Namespace      | Description                                                                                   |
| ------------------------------- | -------------- | --------------------------------------------------------------------------------------------- |
| `[ObjectType<T>]`               | `HotChocolate` | Marks a class as a GraphQL object type bound to the specified runtime type `T`.               |
| `[ExtendObjectType<T>]`         | `HotChocolate` | Extends an existing GraphQL object type bound to the runtime type `T` with additional fields. |
| `[GraphQLName("name")]`         | `HotChocolate` | Overrides the inferred GraphQL name for a type, field, or argument.                           |
| `[GraphQLType<T>]`              | `HotChocolate` | Overrides the inferred GraphQL type for a field or argument to the specified type `T`.        |
| `[GraphQLDescription("...")]`   | `HotChocolate` | Sets the description for a type, field, or argument in the GraphQL schema.                    |
| `[GraphQLDeprecated("reason")]` | `HotChocolate` | Marks a field or enum value as deprecated with the specified reason.                          |
| `[GraphQLIgnore]`               | `HotChocolate` | Excludes a property or method from the GraphQL schema.                                        |

# Data Fetching Attributes

These attributes configure data loading and resolver behavior.

| Attribute      | Namespace      | Description                                                                                           |
| -------------- | -------------- | ----------------------------------------------------------------------------------------------------- |
| `[DataLoader]` | `GreenDonut`   | Marks a static method as a DataLoader resolver. The source generator creates the DataLoader type.     |
| `[IsSelected]` | `HotChocolate` | Indicates that a resolver parameter should receive whether a specific field is selected in the query. |

# Authorization Attributes

| Attribute     | Namespace                    | Description                                                                                            |
| ------------- | ---------------------------- | ------------------------------------------------------------------------------------------------------ |
| `[Authorize]` | `HotChocolate.Authorization` | Applies authorization policies to a type or field. Supports `Policy`, `Roles`, and `Apply` parameters. |

# Error Handling Attributes

| Attribute    | Namespace      | Description                                                                       |
| ------------ | -------------- | --------------------------------------------------------------------------------- |
| `[Error<T>]` | `HotChocolate` | Registers an error type `T` for a mutation field when using mutation conventions. |

# Cost Analysis Attributes

| Attribute    | Namespace                   | Description                                                                                |
| ------------ | --------------------------- | ------------------------------------------------------------------------------------------ |
| `[Cost]`     | `HotChocolate.CostAnalysis` | Sets the cost weight for a field, used by the cost analyzer to calculate query complexity. |
| `[ListSize]` | `HotChocolate.CostAnalysis` | Declares the expected list size for a field, used by the cost analyzer.                    |

# Caching Attributes

| Attribute             | Namespace              | Description                                                                                |
| --------------------- | ---------------------- | ------------------------------------------------------------------------------------------ |
| `[CacheControl(...)]` | `HotChocolate.Caching` | Declares HTTP cache policy hints (`@cacheControl`) for `Cache-Control` and `Vary` headers. |

# Identity Attributes

| Attribute | Namespace      | Description                                                                                          |
| --------- | -------------- | ---------------------------------------------------------------------------------------------------- |
| `[ID]`    | `HotChocolate` | Marks a field as a global object identifier. The value is encoded as a base64 ID with the type name. |
| `[ID<T>]` | `HotChocolate` | Marks a field as a global object identifier associated with a specific GraphQL type `T`.             |

# Resolver Parameter Attributes

These attributes control how parameters are injected into resolvers.

| Attribute              | Namespace      | Description                                                                   |
| ---------------------- | -------------- | ----------------------------------------------------------------------------- |
| `[Parent]`             | `HotChocolate` | Injects the parent (resolved value of the parent field) into the resolver.    |
| `[Service]`            | `HotChocolate` | Injects a service from the dependency injection container.                    |
| `[ScopedService]`      | `HotChocolate` | Injects a scoped service. Equivalent to `[Service]` for the current DI scope. |
| `[GlobalState("key")]` | `HotChocolate` | Injects a value from the global request state by key.                         |
| `[ScopedState("key")]` | `HotChocolate` | Injects a value from the scoped state by key.                                 |
| `[LocalState("key")]`  | `HotChocolate` | Injects a value from the local (field-scoped) state by key.                   |

# Subscription Attributes

| Attribute         | Namespace      | Description                                                                                                        |
| ----------------- | -------------- | ------------------------------------------------------------------------------------------------------------------ |
| `[Subscribe]`     | `HotChocolate` | Marks a method as a subscription resolver that subscribes to an event stream.                                      |
| `[EventMessage]`  | `HotChocolate` | Injects the event message payload into a subscription resolver.                                                    |
| `[Topic("name")]` | `HotChocolate` | Specifies the topic name for subscription event routing. Can also be applied to parameters to use a dynamic topic. |

# Data Middleware Attributes

These attributes apply middleware to fields for pagination, filtering, sorting, and projection.

> Middleware ordering matters. The correct attribute order from top to bottom is: `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`.

| Attribute         | Namespace            | Description                                                         |
| ----------------- | -------------------- | ------------------------------------------------------------------- |
| `[UsePaging]`     | `HotChocolate.Types` | Applies cursor-based pagination to the field.                       |
| `[UseFiltering]`  | `HotChocolate.Data`  | Applies filtering capabilities to the field.                        |
| `[UseSorting]`    | `HotChocolate.Data`  | Applies sorting capabilities to the field.                          |
| `[UseProjection]` | `HotChocolate.Data`  | Applies field projection to push selection down to the data source. |

# Mutation Convention Attributes

| Attribute                 | Namespace            | Description                                                                                 |
| ------------------------- | -------------------- | ------------------------------------------------------------------------------------------- |
| `[UseMutationConvention]` | `HotChocolate.Types` | Applies mutation conventions (input wrapping, payload type generation) to a mutation field. |

# Relay / Global Object Identification Attributes

| Attribute        | Namespace                  | Description                                                                                |
| ---------------- | -------------------------- | ------------------------------------------------------------------------------------------ |
| `[NodeResolver]` | `HotChocolate.Types.Relay` | Marks a method as the resolver for the `node` field, used in Global Object Identification. |

# Schema Design Attributes

| Attribute                    | Namespace      | Description                                                                  |
| ---------------------------- | -------------- | ---------------------------------------------------------------------------- |
| `[RequiresOptIn("feature")]` | `HotChocolate` | Marks a field as requiring opt-in, used with the `@requiresOptIn` directive. |

# Fusion / Federation Attributes

These attributes are used when building Fusion subgraphs or Apollo Federation subgraphs.

| Attribute        | Namespace                       | Description                                                              |
| ---------------- | ------------------------------- | ------------------------------------------------------------------------ |
| `[Lookup]`       | `HotChocolate.Fusion`           | Marks a resolver as a lookup resolver for entity resolution in Fusion.   |
| `[EntityKey]`    | `HotChocolate.Fusion`           | Marks a property as part of the entity key in Fusion.                    |
| `[Shareable]`    | `HotChocolate.ApolloFederation` | Indicates that a field can be resolved by multiple subgraphs.            |
| `[Inaccessible]` | `HotChocolate.ApolloFederation` | Hides a field from the composed supergraph schema.                       |
| `[Internal]`     | `HotChocolate.Fusion`           | Marks a field as internal, visible only within the subgraph.             |
| `[Is]`           | `HotChocolate.Fusion`           | Provides a mapping expression for entity resolution in Fusion.           |
| `[Require]`      | `HotChocolate.Fusion`           | Specifies that a lookup resolver requires certain fields to be provided. |

# Custom Descriptor Attributes

You can create custom attributes to package descriptor configurations for reuse. Hot Chocolate provides base classes for each descriptor type:

- `ObjectTypeDescriptorAttribute`
- `ObjectFieldDescriptorAttribute`
- `InputObjectTypeDescriptorAttribute`
- `InputFieldDescriptorAttribute`
- `InterfaceTypeDescriptorAttribute`
- `InterfaceFieldDescriptorAttribute`
- `EnumTypeDescriptorAttribute`
- `EnumValueDescriptorAttribute`
- `UnionTypeDescriptorAttribute`
- `ArgumentDescriptorAttribute`

Each base class pre-configures the allowed attribute targets. For example, `ObjectFieldDescriptorAttribute` is valid only on methods and properties.

```csharp
public class UseMyMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    public UseMyMiddlewareAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Use(next => async ctx =>
        {
            // Custom logic
            await next(ctx);
        });
    }
}
```

For attributes that target multiple descriptor types, use the `DescriptorAttribute` base class:

```csharp
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public sealed class MyCustomAttribute : DescriptorAttribute
{
    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (element is MemberInfo member)
        {
            switch (descriptor)
            {
                case IInterfaceFieldDescriptor interfaceField:
                    // Configure interface field
                    break;

                case IObjectFieldDescriptor objectField:
                    // Configure object field
                    break;
            }
        }
    }
}
```

# Next Steps

- [Building a schema](/docs/hotchocolate/v16/building-a-schema) for type system configuration
- [Field middleware](/docs/hotchocolate/v16/execution-engine/field-middleware) for creating custom middleware
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for field resolution patterns
