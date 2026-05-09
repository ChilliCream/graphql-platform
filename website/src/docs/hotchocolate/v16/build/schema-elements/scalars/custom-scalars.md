---
title: "Custom Scalars"
---

A custom scalar is a named GraphQL leaf type in GraphQL, implemented in Hot Chocolate with custom coercion logic and a .NET runtime representation. Use a custom scalar when your clients need a reusable, atomic value that is not covered by existing scalars.

By the end of this page, you will know how to decide if a scalar is the right fit, implement the v16 coercion methods, register and apply your scalar, bind a dedicated runtime type, and test the behavior that clients depend on.

This guide focuses on authoring custom scalars in Hot Chocolate v16. For built-in scalars, see [Built-in Scalars](./built-in-scalars). For optional package scalars, see [Community Scalars](./community-scalars). For NodaTime support, see [NodaTime Scalars](./nodatime-scalars).

# Decide if your value should be a scalar

Begin by considering the public schema. A scalar should represent a single leaf value with a consistent format. If your value requires fields, choices, or operation-specific rules, another schema element is usually more appropriate.

| Need                                              | Prefer                             | Example                                                                          |
| ------------------------------------------------- | ---------------------------------- | -------------------------------------------------------------------------------- |
| One atomic value with the same format everywhere  | Reuse or create a scalar           | `OrderCode`, `Slug`, `Sku`                                                       |
| Multiple client-selectable fields                 | Object type and input object type  | `Address { street city country }`                                                |
| A fixed set of named values                       | Enum                               | `OrderStatus`                                                                    |
| Existing Hot Chocolate scalar covers the contract | Existing scalar                    | `UUID` for `Guid`, `URI` for `Uri`, `Decimal` for money amounts without currency |
| Existing package scalar covers the contract       | Package scalar                     | Email address, IP address, color, ISBN                                           |
| Context-specific validation                       | Base scalar plus domain validation | A `String` search term with per-field length limits                              |
| Opaque entity identifier                          | `ID`                               | `Order.id` used for cache keys or refetching                                     |

Good candidates for custom scalars are values like `OrderCode` or `Slug`, where the value is a single string and the same parsing rules apply everywhere. `Address` is not a scalar, since clients often need fields such as street, city, and country. Money with both amount and currency is best modeled as an object and input object, because each part has its own meaning.

Avoid creating a custom scalar to replace a built-in or package scalar that already matches your contract. Choose a custom scalar when your domain value requires a schema name, specific validation, and serialization behavior that clients should learn from the schema.

# Understand the v16 scalar coercion flow

A scalar processes four main paths. GraphQL literals and JSON variables are distinct inputs, so be sure to test both.

| Path                     | Source value                   | Target value           | Method                 | Example                               | Common failure                                                                 | Test to write                                                        |
| ------------------------ | ------------------------------ | ---------------------- | ---------------------- | ------------------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------- |
| Inline query literal     | GraphQL AST node               | .NET runtime value     | `OnCoerceInputLiteral` | `order(code: "ORD-123456")`           | The literal node type does not match `TLiteral`, or the text fails validation. | Valid and invalid inline literals.                                   |
| Variable value           | `System.Text.Json.JsonElement` | .NET runtime value     | `OnCoerceInputValue`   | `{ "code": "ORD-123456" }`            | JSON kind or format does not match the scalar.                                 | Valid and invalid variables.                                         |
| Resolver result          | .NET runtime value             | GraphQL response value | `OnCoerceOutputValue`  | `"ORD-123456"` returned by a resolver | Resolver returns the wrong CLR type or an invalid value.                       | Successful output and invalid resolver output.                       |
| Runtime value to literal | .NET runtime value             | `IValueNode`           | `OnValueToLiteral`     | Default values and schema printing    | The value cannot be represented as a GraphQL literal.                          | Literalization when the scalar appears in defaults or schema output. |

Nullability is not part of the scalar format. Use `NonNullType<T>` or nullable annotations to control whether a field or argument accepts null. Keep your scalar focused on the shape of a non-null leaf value.

`OnCoerceOutputValue` writes into a `ResultElement` and does not return the serialized response value. Use setters like `SetStringValue` or `SetNumberValue`.

# Choose the right base class

| Base class                           | Use when                                                              | Notes                                                                                                                                      |
| ------------------------------------ | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `ScalarType<TRuntimeType, TLiteral>` | One primary GraphQL literal family describes the scalar.              | Recommended default. Examples: `StringValueNode`, `IntValueNode`, `FloatValueNode`.                                                        |
| `ScalarType<TRuntimeType>`           | The scalar accepts several literal or JSON shapes.                    | You must override raw input members such as `CoerceInputLiteral`, `CoerceInputValue`, `SerializationType`, and often compatibility checks. |
| `RegexType`                          | A string scalar is fully described by a regular expression.           | The default regex timeout is 200 ms. Override `FormatException` for custom messages.                                                       |
| `IntegerTypeBase<T>`                 | An integer scalar needs min and max validation.                       | Good for values such as TCP ports. Override `FormatError` for range messages.                                                              |
| `FloatTypeBase<T>`                   | A floating-point or decimal-like scalar needs min and max validation. | Float scalars can accept integer literals when that matches GraphQL coercion rules.                                                        |

`BindingBehavior.Explicit` is the recommended default. Use implicit binding only if every schema usage of the CLR runtime type should infer that scalar.

# Build a string-backed custom scalar

The following example shows a scalar that accepts order codes in the format `ORD-123456`.

```csharp
// Types/OrderCodeType.cs
#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

public sealed class OrderCodeType : ScalarType<string, StringValueNode>
{
    private static readonly Regex s_orderCode = new(
        @"^ORD-[0-9]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public OrderCodeType()
        : base("OrderCode")
    {
        Description = "An order code in the format ORD-123456.";
    }

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        var value = valueLiteral.Value;
        AssertOrderCode(value);
        return value;
    }

    protected override string OnCoerceInputValue(
        JsonElement inputValue,
        IFeatureProvider context)
    {
        var value = inputValue.GetString()!;
        AssertOrderCode(value);
        return value;
    }

    protected override void OnCoerceOutputValue(
        string runtimeValue,
        ResultElement resultValue)
    {
        AssertOrderCode(runtimeValue);
        resultValue.SetStringValue(runtimeValue);
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
    {
        AssertOrderCode(runtimeValue);
        return new StringValueNode(runtimeValue);
    }

    private void AssertOrderCode(string value)
    {
        if (!s_orderCode.IsMatch(value))
        {
            throw new LeafCoercionException(
                "Order codes must match the format ORD-123456.",
                this);
        }
    }
}
```

Use `SpecifiedBy` only when the scalar follows a stable public specification:

```csharp
public OrderCodeType()
    : base("OrderCode")
{
    Description = "An order code in the format ORD-123456.";
    // SpecifiedBy = new Uri("https://example.org/scalars/order-code");
}
```

Do not invent a URL for a private domain scalar.

# Register and apply the scalar

Registering the type makes `OrderCode` available to the schema builder.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypes()
    .AddType<OrderCodeType>();
```

Registration does not turn every `string` field into `OrderCode`. A string-backed scalar shares the CLR runtime type with `String`, so apply it where the contract matters.

## Apply the scalar with attributes

```csharp
// Types/Order.cs
using HotChocolate;
using HotChocolate.Types;

public sealed class Order
{
    [GraphQLType<OrderCodeType>]
    public required string Code { get; init; }
}
```

```csharp
// Types/OrderQueries.cs
using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class OrderQueries
{
    public static Order? GetOrder([GraphQLType<OrderCodeType>] string code)
    {
        return code == "ORD-123456"
            ? new Order { Code = code }
            : null;
    }
}
```

Expected SDL:

```graphql
scalar OrderCode

type Query {
  order(code: OrderCode!): Order
}

type Order {
  code: OrderCode!
}
```

## Apply the scalar with descriptors

Use descriptors when you keep schema metadata outside your model types.

```csharp
using HotChocolate.Types;

public sealed class OrderType : ObjectType<Order>
{
    protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor
            .Field(t => t.Code)
            .Type<NonNullType<OrderCodeType>>();
    }
}

public sealed class OrderQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("order")
            .Argument("code", a => a.Type<NonNullType<OrderCodeType>>())
            .Resolve(context =>
            {
                var code = context.ArgumentValue<string>("code");
                return new Order { Code = code };
            })
            .Type<OrderType>();
    }
}
```

Register descriptor-based root types with `AddQueryType<T>`:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<OrderQueriesType>()
    .AddType<OrderType>()
    .AddType<OrderCodeType>();
```

Before you apply the scalar, Hot Chocolate infers `String` for a `string` property:

```graphql
type Order {
  code: String!
}
```

After you apply `OrderCodeType`, the field uses your scalar:

```graphql
scalar OrderCode

type Order {
  code: OrderCode!
}
```

# Send and receive scalar values

Clients use the same schema name for literals and variables.

Inline literal:

```graphql
query {
  order(code: "ORD-123456") {
    code
  }
}
```

Variable operation:

```graphql
query GetOrder($code: OrderCode!) {
  order(code: $code) {
    code
  }
}
```

Variables JSON:

```json
{
  "code": "ORD-123456"
}
```

Response:

```json
{
  "data": {
    "order": {
      "code": "ORD-123456"
    }
  }
}
```

If a client sends `"ABC-123456"`, the scalar raises a coercion error before the resolver receives the value.

# Bind a dedicated runtime type

If a scalar is important in your domain model, prefer a dedicated CLR type. Then Hot Chocolate can infer your scalar for every `OrderCode` usage without affecting unrelated strings.

```csharp
// Types/OrderCode.cs
public readonly record struct OrderCode(string Value)
{
    public override string ToString() => Value;
}
```

Use the same coercion shape, but make the runtime type `OrderCode`:

```csharp
// Types/OrderCodeType.cs
#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

public sealed class OrderCodeType : ScalarType<OrderCode, StringValueNode>
{
    private static readonly Regex s_orderCode = new(
        @"^ORD-[0-9]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public OrderCodeType()
        : base("OrderCode")
    {
        Description = "An order code in the format ORD-123456.";
    }

    protected override OrderCode OnCoerceInputLiteral(StringValueNode valueLiteral)
        => Parse(valueLiteral.Value);

    protected override OrderCode OnCoerceInputValue(
        JsonElement inputValue,
        IFeatureProvider context)
        => Parse(inputValue.GetString()!);

    protected override void OnCoerceOutputValue(
        OrderCode runtimeValue,
        ResultElement resultValue)
    {
        AssertOrderCode(runtimeValue.Value);
        resultValue.SetStringValue(runtimeValue.Value);
    }

    protected override StringValueNode OnValueToLiteral(OrderCode runtimeValue)
    {
        AssertOrderCode(runtimeValue.Value);
        return new StringValueNode(runtimeValue.Value);
    }

    private OrderCode Parse(string value)
    {
        AssertOrderCode(value);
        return new OrderCode(value);
    }

    private void AssertOrderCode(string value)
    {
        if (!s_orderCode.IsMatch(value))
        {
            throw new LeafCoercionException(
                "Order codes must match the format ORD-123456.",
                this);
        }
    }
}
```

Register the scalar and bind the runtime type:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypes()
    .AddType<OrderCodeType>()
    .BindRuntimeType<OrderCode, OrderCodeType>();
```

You can also make the scalar implicit at the type level:

```csharp
public OrderCodeType()
    : base("OrderCode", BindingBehavior.Implicit)
{
}
```

Use either explicit runtime binding or implicit binding deliberately. Do not bind `string` globally to a domain scalar unless every string in the schema should become that scalar.

Use `AddTypeConverter` when you reuse an existing scalar for a different CLR type. For a scalar with its own schema name and coercion rules, implement the scalar directly.

# Use specialized base classes for common patterns

## RegexType for string formats

Use `RegexType` when a regular expression fully describes the scalar.

```csharp
using HotChocolate.Types;

public sealed class SlugType : RegexType
{
    public SlugType()
        : base(
            "Slug",
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            "A lower-case URL slug.")
    {
    }

    protected override LeafCoercionException FormatException(string runtimeValue)
        => new(
            "Slugs must contain lower-case letters, numbers, and single hyphens.",
            this);
}
```

Register the subclass:

```csharp
builder
    .AddGraphQL()
    .AddType<SlugType>();
```

For one-off scalar registration, instantiate `RegexType` directly:

```csharp
builder
    .AddGraphQL()
    .AddType(new RegexType(
        "Slug",
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        "A lower-case URL slug."));
```

## IntegerTypeBase for numeric ranges

Use `IntegerTypeBase<T>` for integer values with a minimum and maximum.

```csharp
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

public sealed class TcpPortType : IntegerTypeBase<int>
{
    public TcpPortType()
        : base("TcpPort", min: 1, max: 65535)
    {
        Description = "A TCP port number from 1 through 65535.";
    }

    protected override int OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToInt32();

    protected override int OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt32();

    protected override void OnCoerceOutputValue(
        int runtimeValue,
        ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    protected override IValueNode OnValueToLiteral(int runtimeValue)
        => new IntValueNode(runtimeValue);

    protected override LeafCoercionException FormatError(int runtimeValue)
        => new("TCP ports must be between 1 and 65535.", this);
}
```

`FloatTypeBase<T>` has the same range-validation purpose for floating-point or decimal-like values. Use it when a scalar remains the right schema contract. If the value also needs a unit, currency, or other field, model an object and input object instead.

# Handle ID, UUID, and scalar specifications

Use `ID` for opaque identifiers that clients pass around for refetching, caching, or entity identity. The `ID` scalar accepts string and integer input and serializes as a JSON string. Do not create an `OrderId` custom scalar only to name an entity ID. Create one only when clients need a public, non-opaque format contract.

Use `UUID` for `Guid` values that clients should understand as UUIDs, such as tracking IDs and correlation IDs. Do not duplicate UUID parsing in a custom scalar.

Set `SpecifiedBy` only when the scalar follows a stable public scalar specification. Private domain formats usually do not need a `@specifiedBy` URL.

# Handle errors consistently

Invalid scalar values should become GraphQL coercion errors. Throw `LeafCoercionException` from intentional validation logic and pass the scalar instance, usually `this`, to the constructor.

```csharp
throw new LeafCoercionException(
    "Order codes must match the format ORD-123456.",
    this);
```

Let the base class handle incompatible literal or JSON kinds where possible. Override the error factory methods only when you need consistent custom messages:

- `CreateCoerceInputLiteralError`
- `CreateCoerceInputValueError`
- `CreateCoerceOutputValueError`
- `CreateValueToLiteralError`

Do not include sensitive rejected values in error text.

| Failure                            | Where it appears                        | What to check                                 |
| ---------------------------------- | --------------------------------------- | --------------------------------------------- |
| Literal kind mismatch              | Validation error for inline query input | `TLiteral` and `OnCoerceInputLiteral`.        |
| JSON variable kind mismatch        | Variable coercion error                 | `OnCoerceInputValue` and JSON `ValueKind`.    |
| Format validation failure          | Input or output coercion error          | Shared validation method and message.         |
| Resolver returns wrong CLR type    | Execution error on the field            | Resolver return type and scalar runtime type. |
| Resolver returns invalid CLR value | Execution error on the field            | `OnCoerceOutputValue` validation.             |
| Schema/default value cannot print  | Schema build or printing error          | `OnValueToLiteral`.                           |

# Test a custom scalar

Test scalar behavior at two levels:

1. Unit-level scalar tests call coercion methods through the scalar type and verify parsing, output serialization, literalization, and failures.
2. Schema or execution tests verify the public SDL, variable coercion, inline literals, resolver output, and binding behavior.

Use this checklist before exposing the scalar to clients:

| Scenario                                                                     | Expected result                                                              |
| ---------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| SDL contains `scalar OrderCode` where the field or argument uses the scalar. | Clients see the schema contract.                                             |
| Valid inline literal is accepted.                                            | Resolver receives the .NET runtime value.                                    |
| Invalid inline literal fails.                                                | Client receives a coercion error.                                            |
| Valid JSON variable is accepted.                                             | Resolver receives the .NET runtime value.                                    |
| Invalid JSON variable fails.                                                 | Client receives a variable coercion error.                                   |
| Valid resolver output serializes.                                            | Response contains the expected JSON value.                                   |
| Invalid resolver output fails.                                               | Client receives a field execution error.                                     |
| `OnValueToLiteral` returns the expected AST node.                            | Defaults and schema output can be printed.                                   |
| Explicit or implicit binding is verified.                                    | Related fields infer the intended scalar and unrelated fields do not change. |

Representative execution test:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

public sealed class OrderCodeScalarTests
{
    [Fact]
    public async Task Execute_Should_Return_OrderCode_When_Variable_Is_Valid()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<OrderCodeType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            query GetOrder($code: OrderCode!) {
              order(code: $code) {
                code
              }
            }
            """,
            new Dictionary<string, object?>
            {
                ["code"] = "ORD-123456"
            });

        // assert
        Assert.Equal(
            """
            {
              "data": {
                "order": {
                  "code": "ORD-123456"
                }
              }
            }
            """,
            result.ToJson().Trim());
    }

    private sealed class Query
    {
        public Order GetOrder([GraphQLType<OrderCodeType>] string code)
            => new() { Code = code };
    }

    private sealed class Order
    {
        [GraphQLType<OrderCodeType>]
        public required string Code { get; init; }
    }
}
```

Expected response shape:

```json
{
  "data": {
    "order": {
      "code": "ORD-123456"
    }
  }
}
```

If you use CookieCrumble snapshots in this repository, snapshot the SDL and execution result shapes so review shows the public contract.

# Troubleshoot custom scalars

| Symptom                                                | Likely cause                                                                | Fix                                                                                                   |
| ------------------------------------------------------ | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| Field still appears as `String`.                       | The scalar is registered, but the field still uses inferred CLR mapping.    | Annotate the field or argument, configure it with a descriptor, or bind a dedicated CLR runtime type. |
| Variables fail but inline literals work.               | Variables arrive as `JsonElement`, not `StringValueNode`.                   | Check `OnCoerceInputValue`.                                                                           |
| Inline literals fail but variables work.               | The literal kind does not match `TLiteral`.                                 | Check the client syntax and `OnCoerceInputLiteral`.                                                   |
| Resolver output fails.                                 | Output coercion did not write a valid response value.                       | Check `OnCoerceOutputValue` and use the correct `ResultElement` setter.                               |
| Schema/default value cannot print.                     | Literalization is missing or invalid.                                       | Check `OnValueToLiteral`.                                                                             |
| Every string became the custom scalar.                 | A global string binding or implicit string scalar changed inference.        | Remove global string binding. Use explicit annotations or a dedicated CLR value type.                 |
| The scalar needs several fields.                       | The value is structured, not a leaf value.                                  | Use [object types](../object-types) and [input object types](../input-object-types).                  |
| Filtering does not expose the operations you expected. | Custom scalar types and filter operation input types are separate concerns. | See [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types).             |

# Next steps

- Check [Built-in Scalars](./built-in-scalars) before creating a custom scalar.
- Use [Community Scalars](./community-scalars) for optional common scalar packages.
- Use [NodaTime Scalars](./nodatime-scalars) for NodaTime runtime types.
- Use [Object Types](../object-types) and [Input Object Types](../input-object-types) for structured values.
- Use [Enums](../enums) for fixed symbolic choices.
- Read [the v15 to v16 migration guide](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16) if you are updating scalar code that uses older method names.
