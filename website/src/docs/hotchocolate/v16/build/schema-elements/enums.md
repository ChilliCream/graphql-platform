---
title: "Enums"
---

A GraphQL enum is a named type that defines a fixed set of named values. Enums are ideal when clients must select from a closed list, such as order status, payment card type, user role, or chat message role.

```graphql
enum OrderStatus {
  SUBMITTED
  AWAITING_VALIDATION
  PAID
  SHIPPED
  CANCELLED
}

type Order {
  id: ID!
  status: OrderStatus!
}

type Query {
  orders(status: OrderStatus): [Order!]!
}
```

In Hot Chocolate, a C# `enum` maps directly to a GraphQL enum type. The C# member represents the runtime value, while the GraphQL enum value name forms the public schema contract that clients use for input and output.

# When to Use an Enum

Choose an enum when the set of values is small, stable, and meaningful within your schema. Good enum values are easily discoverable by client tooling through introspection and are governed as part of your schema contract.

Avoid enums for open-ended values, frequently changing external codes, free-form user input, or data that requires additional fields. If a value needs a label, description, display order, color, permissions, or behavior that clients must query, model it as an object type instead.

| Need                             | Prefer      | Example                               |
| -------------------------------- | ----------- | ------------------------------------- |
| A closed set of public choices   | Enum        | `OrderStatus`, `CardType`, `UserRole` |
| Free-form text or external codes | Scalar      | SKU, search term, vendor status code  |
| A true or false choice           | Boolean     | `includeArchived: Boolean`            |
| A value with fields or behavior  | Object type | `Status { code label color }`         |

For custom primitive values, see [Scalars](/docs/hotchocolate/v16/build/schema-elements/scalars). For output shapes with fields, see [Object Types](./object-types).

# Defining an Enum from a C# Enum

Hot Chocolate infers enum types from resolver return values, resolver parameters, object type properties, and input object properties. No extra registration is required when the enum is discovered through these signatures.

```csharp
// Types/OrderStatus.cs
public enum OrderStatus
{
    Submitted,
    AwaitingValidation,
    Paid,
    Shipped,
    Cancelled
}

// Types/Order.cs
public sealed class Order
{
    public int Id { get; init; }
    public OrderStatus Status { get; init; }
}

// Types/OrderQueries.cs
[QueryType]
public static partial class OrderQueries
{
    public static IEnumerable<Order> GetOrders(OrderStatus? status)
    {
        var orders = new[]
        {
            new Order { Id = 1, Status = OrderStatus.AwaitingValidation },
            new Order { Id = 2, Status = OrderStatus.Paid }
        };

        if (status is null)
        {
            return orders;
        }

        return orders.Where(order => order.Status == status);
    }
}
```

The generated schema exposes the enum as both an input type for arguments and an output type for fields.

```graphql
enum OrderStatus {
  SUBMITTED
  AWAITING_VALIDATION
  PAID
  SHIPPED
  CANCELLED
}

type Order {
  id: Int!
  status: OrderStatus!
}

type Query {
  orders(status: OrderStatus): [Order!]!
}
```

Enums can also be used inside input objects.

```csharp
// Types/OrderFilterInput.cs
public sealed class OrderFilterInput
{
    public OrderStatus? Status { get; init; }
}

// Types/OrderQueries.cs
[QueryType]
public static partial class OrderQueries
{
    public static IEnumerable<Order> GetOrdersByFilter(OrderFilterInput? filter)
    {
        // Use filter?.Status in your data access layer.
        return [];
    }
}
```

```graphql
input OrderFilterInput {
  status: OrderStatus
}

type Query {
  ordersByFilter(filter: OrderFilterInput): [Order!]!
}
```

For more on input modeling, see [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments) and [Input Object Types](./input-object-types).

# Using Enum Values in Operations and JSON

GraphQL documents use unquoted enum literals. In contrast, JSON variables and responses use strings, since JSON does not have an enum literal type.

```graphql
query GetAwaitingOrders {
  orders(status: AWAITING_VALIDATION) {
    id
    status
  }
}
```

When using variables, the value is a JSON string.

```graphql
query GetOrders($status: OrderStatus) {
  orders(status: $status) {
    id
    status
  }
}
```

```json
{
  "status": "AWAITING_VALIDATION"
}
```

Responses also represent enum values as JSON strings.

```json
{
  "data": {
    "orders": [
      {
        "id": 1,
        "status": "AWAITING_VALIDATION"
      }
    ]
  }
}
```

Common mistakes:

```graphql
# Invalid: this is a string literal, not an enum literal.
{
  orders(status: "AWAITING_VALIDATION") {
    id
  }
}
```

```json
{
  "status": "AwaitingValidation"
}
```

The second example is only valid if you have renamed the GraphQL enum value to `AwaitingValidation`. GraphQL names are case-sensitive.

# Naming and Casing

By default, the enum type name matches the C# enum type name. Enum member names are converted to `UPPER_SNAKE_CASE`.

| C# member            | GraphQL enum value    |
| -------------------- | --------------------- |
| `Submitted`          | `SUBMITTED`           |
| `AwaitingValidation` | `AWAITING_VALIDATION` |
| `StockConfirmed`     | `STOCK_CONFIRMED`     |
| `MasterCard`         | `MASTER_CARD`         |
| `HeadOfDepartment`   | `HEAD_OF_DEPARTMENT`  |

GraphQL enum value names are part of your public schema. Renaming a value is a breaking change for clients that send it as input or handle it in responses.

For more on naming, see the [custom attributes reference](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes).

# Renaming Enum Types and Values

Use explicit names when you need to preserve an existing schema, match product vocabulary, or prevent C# or persistence naming from leaking into the schema.

<ExampleTabs>
<Implementation>

```csharp
// Types/OrderStatus.cs
[GraphQLName("OrderState")]
public enum OrderStatus
{
    Submitted,

    [GraphQLName("IN_PROGRESS")]
    AwaitingValidation,

    Paid,
    Shipped,
    Cancelled
}
```

</Implementation>
<Code>

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor.Name("OrderState");
        descriptor
            .Value(OrderStatus.AwaitingValidation)
            .Name("IN_PROGRESS");
    }
}

// Program.cs
builder
    .AddGraphQL()
    .AddType<OrderStatusType>();
```

</Code>
</ExampleTabs>

Both approaches produce the following public shape:

```graphql
enum OrderState {
  SUBMITTED
  IN_PROGRESS
  PAID
  SHIPPED
  CANCELLED
}
```

# Adding Descriptions

Descriptions appear in introspection and GraphQL IDEs. Use them to explain the product meaning of the enum and each value.

<ExampleTabs>
<Implementation>

```csharp
// Types/OrderStatus.cs
[GraphQLDescription("The public fulfillment state of an order.")]
public enum OrderStatus
{
    [GraphQLDescription("The order was submitted but has not been validated.")]
    Submitted,

    [GraphQLDescription("The order is being validated before payment capture.")]
    AwaitingValidation,

    [GraphQLDescription("Payment was captured for the order.")]
    Paid,

    [GraphQLDescription("The order left the warehouse.")]
    Shipped,

    [GraphQLDescription("The order will not be fulfilled.")]
    Cancelled
}
```

</Implementation>
<Code>

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor.Description("The public fulfillment state of an order.");
        descriptor
            .Value(OrderStatus.Paid)
            .Description("Payment was captured for the order.");
    }
}
```

</Code>
</ExampleTabs>

For XML documentation comments and precedence rules, see [Documentation](/docs/hotchocolate/v16/build/schema-elements/documentation-comments).

# Safely Deprecating Enum Values

Deprecated enum values remain valid, but introspection marks them as deprecated so clients can migrate.

<ExampleTabs>
<Implementation>

```csharp
// Types/OrderStatus.cs
public enum OrderStatus
{
    [GraphQLDeprecated("Use AWAITING_VALIDATION instead.")]
    Submitted,

    AwaitingValidation,

    [Obsolete("Use Shipped instead.")]
    Dispatched,

    Paid,
    Shipped,
    Cancelled
}
```

</Implementation>
<Code>

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor
            .Value(OrderStatus.Submitted)
            .Deprecated("Use AWAITING_VALIDATION instead.");
    }
}
```

</Code>
</ExampleTabs>

The generated SDL includes the `@deprecated` directive:

```graphql
enum OrderStatus {
  SUBMITTED @deprecated(reason: "Use AWAITING_VALIDATION instead.")
  AWAITING_VALIDATION
  DISPATCHED @deprecated(reason: "Use Shipped instead.")
  PAID
  SHIPPED
  CANCELLED
}
```

A safe migration sequence:

1. Add the replacement value if the schema needs one.
2. Deprecate the old value with a clear reason.
3. Monitor client usage.
4. Remove the old value only in a breaking-change release or a new schema version.

See [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning) and [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution) for more on compatibility.

# Hide Internal Enum Members

Hot Chocolate exposes discovered C# enum members by default. Hide persistence-only, workflow-only, or legacy values that clients must not use.

<ExampleTabs>
<Implementation>

```csharp
// Types/OrderStatus.cs
public enum OrderStatus
{
    Submitted,
    AwaitingValidation,
    Paid,
    Shipped,
    Cancelled,

    [GraphQLIgnore]
    InternalFraudReview
}
```

</Implementation>
<Code>

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor.Ignore(OrderStatus.InternalFraudReview);

        // Equivalent value-level form:
        descriptor
            .Value(OrderStatus.InternalFraudReview)
            .Ignore();
    }
}
```

</Code>
</ExampleTabs>

If a resolver returns an ignored runtime value, Hot Chocolate cannot serialize it as one of the exposed GraphQL enum values. Map internal states to public states before returning data, or expose a deliberate public value.

# Configure Enums with `EnumType<T>`

Use `EnumType<T>` when attributes are not enough, when you want schema configuration outside the runtime enum, or when you need a reusable configured enum type.

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor.Name("OrderStatus");
        descriptor.Description("The public fulfillment state of an order.");

        descriptor
            .Value(OrderStatus.AwaitingValidation)
            .Name("IN_PROGRESS")
            .Description("The order is being validated before payment capture.");
    }
}
```

Register the configured type with the schema.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddType<OrderStatusType>();
```

You can also configure the enum inline.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddEnumType<OrderStatus>(descriptor =>
    {
        descriptor.Name("OrderStatus");
        descriptor
            .Value(OrderStatus.AwaitingValidation)
            .Name("IN_PROGRESS");
    });
```

Configured enum types are not always inferred automatically because the same runtime enum can have multiple GraphQL configurations. Register the type or select it where it is used.

```csharp
// Types/OrderType.cs
public sealed class OrderType : ObjectType<Order>
{
    protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor
            .Field(order => order.Status)
            .Type<OrderStatusType>();
    }
}

// Types/OrderQueriesType.cs
public sealed class OrderQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("orders")
            .Argument("status", argument => argument.Type<OrderStatusType>())
            .Resolve(context => Array.Empty<Order>());
    }
}
```

# Choose Implicit or Explicit Value Binding

Implicit binding is the default. It includes discovered C# enum members unless you ignore them. This is convenient when the C# enum is already shaped as a public GraphQL contract.

Explicit binding includes only the values that you declare. Use it when the C# enum contains internal members or when you want tighter control over the public schema.

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<OrderStatus>
{
    protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
    {
        descriptor.BindValuesExplicitly();

        descriptor.Value(OrderStatus.Submitted);
        descriptor.Value(OrderStatus.AwaitingValidation);
        descriptor.Value(OrderStatus.Paid);
        descriptor.Value(OrderStatus.Shipped);
        descriptor.Value(OrderStatus.Cancelled);
    }
}
```

| Binding option   | What it does                                    | Use when                                         |
| ---------------- | ----------------------------------------------- | ------------------------------------------------ |
| Implicit binding | Includes discovered enum members by convention  | Your C# enum is already a public schema contract |
| Explicit binding | Includes only values declared with `Value(...)` | You want strict schema control                   |
| Ignored values   | Excludes selected values from implicit binding  | One or two members are internal                  |

The descriptor also supports `BindValues(BindingBehavior.Explicit)`, `BindValues(BindingBehavior.Implicit)`, and `BindValuesImplicitly()`.

# Bind GraphQL Enum Values to Non-Enum Runtime Values

In code-first schemas, `EnumType<T>` can bind a GraphQL enum to runtime values that are not C# enum members. Use this deliberately when values come from configuration, a database, or an external system, but the GraphQL contract is still closed.

```csharp
// Types/OrderStatusType.cs
public sealed class OrderStatusType : EnumType<string>
{
    protected override void Configure(IEnumTypeDescriptor<string> descriptor)
    {
        descriptor.Name("OrderStatus");

        descriptor
            .Value("submitted")
            .Name("SUBMITTED");

        descriptor
            .Value("awaiting-validation")
            .Name("AWAITING_VALIDATION");

        descriptor
            .Value("paid")
            .Name("PAID");
    }
}
```

Clients still see and send GraphQL enum names.

```graphql
enum OrderStatus {
  SUBMITTED
  AWAITING_VALIDATION
  PAID
}
```

Resolvers receive and return the configured runtime values, such as `"awaiting-validation"`, while clients use `AWAITING_VALIDATION`.

# Understand Runtime Coercion and Serialization

Hot Chocolate coerces input enum names to the configured runtime values and serializes runtime values back to GraphQL enum names.

| Location              | Representation                   |
| --------------------- | -------------------------------- |
| SDL definition        | `AWAITING_VALIDATION`            |
| GraphQL query literal | `AWAITING_VALIDATION`            |
| JSON variable value   | `"AWAITING_VALIDATION"`          |
| JSON response value   | `"AWAITING_VALIDATION"`          |
| C# enum value         | `OrderStatus.AwaitingValidation` |
| Custom runtime string | `"awaiting-validation"`          |

# Use Enums with Filtering and Sorting

Enum arguments and enum fields inside input objects are enough for many APIs. When you enable Hot Chocolate filtering for a model with enum properties, enum filters can expose operations such as `eq`, `neq`, `in`, and `nin`. See [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) for filter input generation and customization.

Sorting may expose framework sort direction enum types, such as ascending or descending. Keep those separate from application domain enums such as `OrderStatus`. See [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for sort input types.

# Advanced: Flags Enums and Bitwise Values

Normal GraphQL enums represent one named value at a time. `[Flags]` enums represent bitwise combinations, so they need a different schema shape.

Hot Chocolate has opt-in flag enum support:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyOptions(options => options.EnableFlagEnums = true);
```

When enabled, Hot Chocolate uses flag-specific input and output shapes with Boolean fields for individual flags rather than treating every combination as a normal enum literal. If your clients should send multiple choices, first consider a list of normal enum values. Enable flag enum support only when bitwise semantics are part of your contract.

See [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options) for the `EnableFlagEnums` setting.

# Evolve Enum Contracts Without Breaking Clients

Design enum names as stable product vocabulary. Avoid exposing temporary states, persistence details, or internal workflow steps.

| Change                   | Compatibility guidance                                                            |
| ------------------------ | --------------------------------------------------------------------------------- |
| Add an output enum value | Usually additive, but clients with exhaustive switches may need updates           |
| Add an input enum value  | Can change accepted inputs and server behavior, so document the product semantics |
| Rename a value           | Breaking for clients that send or read the old name                               |
| Remove a value           | Breaking                                                                          |
| Change runtime mapping   | Behavior change, even if SDL does not change                                      |
| Deprecate a value        | Preferred migration step before removal                                           |

Prefer additive changes plus deprecation. Remove values only through your breaking-change process.

# Troubleshooting

## My query fails when I quote an enum value

GraphQL documents use enum literals, not strings. Use an unquoted value.

```graphql
# Incorrect
{
  orders(status: "PAID") {
    id
  }
}

# Correct
{
  orders(status: PAID) {
    id
  }
}
```

## My variables fail when I use the C# member name

Variables use GraphQL enum value names. Send `"AWAITING_VALIDATION"`, not `"AwaitingValidation"`, unless you explicitly renamed the GraphQL value.

```json
{
  "status": "AWAITING_VALIDATION"
}
```

## My custom `EnumType<T>` is not used

A configured enum type must be registered or selected for the relevant field or argument. Register it with `.AddType<OrderStatusType>()`, configure it with `.AddEnumType<OrderStatus>(...)`, or select it with `.Type<OrderStatusType>()` on the field or argument.

## An internal enum value appears in the schema

Implicit binding includes discovered enum members by default. Add `[GraphQLIgnore]`, call `descriptor.Ignore(...)`, or use `BindValuesExplicitly()` and declare only the public values.

## A resolver returns a value that is not in the GraphQL enum

The runtime value has no exposed GraphQL enum value. This often happens when a member was ignored, omitted by explicit binding, or mapped incorrectly. Map internal states to public states before returning data.

## A renamed enum value broke clients

GraphQL enum value names are schema contract names. Add a replacement value, deprecate the old value when possible, and remove it only after clients migrate.

## My `[Flags]` enum does not behave like a normal enum

Flag enum support is opt-in and uses flag-specific input and output shapes when enabled. Decide whether you need a normal enum, a list of enum values, or the `EnableFlagEnums` option.

# Quick Reference

| Task                              | Attribute or API                                                                                |
| --------------------------------- | ----------------------------------------------------------------------------------------------- |
| Rename an enum type or value      | `[GraphQLName]`, `descriptor.Name(...)`, `descriptor.Value(...).Name(...)`                      |
| Describe an enum type or value    | `[GraphQLDescription]`, `descriptor.Description(...)`, `descriptor.Value(...).Description(...)` |
| Deprecate an enum value           | `[GraphQLDeprecated]`, `[Obsolete]`, `descriptor.Value(...).Deprecated(...)`                    |
| Hide an enum value                | `[GraphQLIgnore]`, `descriptor.Ignore(...)`, `descriptor.Value(...).Ignore()`                   |
| Configure an enum in code-first   | `EnumType<T>`                                                                                   |
| Include only declared values      | `descriptor.BindValuesExplicitly()`                                                             |
| Return to convention-based values | `descriptor.BindValuesImplicitly()`                                                             |
| Bind values such as strings       | `EnumType<string>` with `descriptor.Value(runtimeValue).Name(schemaName)`                       |

| Context        | Example                     |
| -------------- | --------------------------- |
| SDL definition | `enum OrderStatus { PAID }` |
| Query document | `orders(status: PAID)`      |
| Variables JSON | `{ "status": "PAID" }`      |
| Response JSON  | `{ "status": "PAID" }`      |
| C# value       | `OrderStatus.Paid`          |

# Next Steps

- Use enum fields on [Object Types](./object-types).
- Use enums inside [Input Object Types](./input-object-types).
- Compare enums with [Scalars](/docs/hotchocolate/v16/build/schema-elements/scalars).
- Add descriptions with [Documentation](/docs/hotchocolate/v16/build/schema-elements/documentation-comments).
- Plan enum changes with [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning) and [Schema Evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution).
