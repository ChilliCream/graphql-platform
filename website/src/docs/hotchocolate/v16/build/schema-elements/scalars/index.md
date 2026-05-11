---
title: "Scalars"
---

Scalars are the leaf values at the edges of a Hot Chocolate schema. They represent values such as strings, numbers, dates, identifiers, URLs, and JSON values. Clients can select a scalar field, but cannot select subfields beneath a scalar.

This page will help you choose the right scalar approach. For full details on scalar behavior, continue from this overview to the built-in, package, NodaTime, or custom scalar documentation.

# Start with inferred scalar values

When your CLR type already describes the value, allow Hot Chocolate to infer the scalar. Add explicit schema metadata only when the GraphQL contract requires a different meaning.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

public sealed class Product
{
    [ID]
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public decimal Price { get; init; }

    public DateTime CreatedAt { get; init; }

    public Guid TrackingId { get; init; }
}

[QueryType]
public static partial class ProductQueries
{
    public static Product GetProduct()
        => new()
        {
            Id = Guid.Parse("8f3f1f2e-3d6c-4f9a-95b4-39c86b5d0a61"),
            Name = "Trail Backpack",
            Price = 129.95m,
            CreatedAt = new DateTime(2025, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            TrackingId = Guid.Parse("a0b6f3db-68f7-44bf-98a9-3ef0e4f6a23a")
        };
}
```

Hot Chocolate generates a schema like this:

```graphql
type Query {
  product: Product!
}

type Product {
  id: ID!
  name: String!
  price: Decimal!
  createdAt: DateTime!
  trackingId: UUID!
}
```

The `id` field uses `ID` because it is marked as an opaque identifier. The `trackingId` field remains a `UUID` because a plain `Guid` maps to `UUID`. The `[ID]` attribute, provided by Hot Chocolate's Relay helpers, rewrites the schema field to `ID` and formats the value as an opaque global identifier. Use `[GraphQLType<IdType>]` if you need the raw GraphQL `ID` scalar without Relay formatting.

# Understand the scalar boundary

A scalar bridges three value forms:

1. GraphQL literals or JSON variable values from the client
2. .NET runtime values used by resolvers and services
3. JSON response values sent back to the client

```text
GraphQL literal or JSON variable
        |
        v
scalar input coercion
        |
        v
.NET runtime value
        |
        v
resolver and application code
        |
        v
scalar output serialization
        |
        v
JSON response value
```

A scalar is responsible for parsing, validation, runtime conversion, and response serialization for a single leaf value. It does not provide selectable fields. If clients need to request fields like `amount` and `currency`, use an object type instead of a scalar.

# Let Hot Chocolate infer built-in scalars

Use inferred built-in scalars when the CLR type clearly communicates the GraphQL value.

| CLR type                     | GraphQL scalar | Use when                                                  |
| ---------------------------- | -------------- | --------------------------------------------------------- |
| `string`                     | `String`       | You expose text.                                          |
| `bool`                       | `Boolean`      | You expose true or false values.                          |
| `int`                        | `Int`          | You expose signed 32-bit integers.                        |
| `float`, `double`            | `Float`        | You expose floating-point values.                         |
| `decimal`                    | `Decimal`      | You expose high-precision decimal values.                 |
| `DateTime`, `DateTimeOffset` | `DateTime`     | You expose a date and time with offset semantics.         |
| `DateOnly`                   | `Date`         | You expose a calendar date.                               |
| `TimeOnly`                   | `LocalTime`    | You expose a time of day.                                 |
| `TimeSpan`                   | `Duration`     | You expose a duration.                                    |
| `Guid`                       | `UUID`         | You expose a UUID value, not an opaque entity identifier. |
| `Uri`                        | `URI`          | You expose an absolute or relative URI.                   |
| `byte[]`                     | `Base64String` | You expose binary data as base64 text.                    |
| `JsonElement`                | `Any`          | You expose intentionally open JSON-like data.             |

Hot Chocolate only includes scalars that your schema actually uses. See [Built-in Scalars](./built-in-scalars) for the full mapping table, numeric variants, date and time options, UUID formats, `Any`, `URI`, and `Base64String`.

# Mark opaque identifiers with ID

Use `ID` when clients should treat a value as an identifier and pass it around without interpreting it. The `ID` scalar accepts string and integer inputs and serializes as a string in the GraphQL response. Your server code can use explicit CLR types such as `int`, `string`, or `Guid`.

`ID` is explicit. Hot Chocolate does not infer it from a property named `Id`, from `string`, or from `Guid`. Use `[GraphQLType<IdType>]` or descriptor configuration for the raw `ID` scalar contract. Use `[ID]` or `[ID<T>]` when you want Relay-style global ID formatting and decoding.

```csharp
using HotChocolate.Types.Relay;

public sealed class ChatMessage
{
    [ID]
    public Guid Id { get; init; }

    public DateTime SentAt { get; init; }
}
```

Expected SDL:

```graphql
type ChatMessage {
  id: ID!
  sentAt: DateTime!
}
```

A response still contains a JSON string for the identifier. With global object identification enabled, that string is opaque and not the raw `Guid` value.

Use `[ID<T>]` when the identifier belongs to a specific GraphQL type or participates in typed ID behavior. `[ID<T>]` uses the configured GraphQL type name when one exists.

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Relay;

[MutationType]
public static partial class BasketMutations
{
    public static Task<Basket> AddToBasketAsync(
        [ID<Product>] int productId,
        int quantity,
        BasketService baskets,
        CancellationToken cancellationToken)
        => baskets.AddToBasketAsync(productId, quantity, cancellationToken);
}
```

Expected SDL:

```graphql
type Mutation {
  addToBasket(productId: ID!, quantity: Int!): Basket!
}
```

The client sends the `Product` ID string. Hot Chocolate decodes that value before your resolver receives the `int productId`. For global object identification, node IDs, and composite IDs, see the [Relay and global object identification docs](/docs/hotchocolate/v16/build/schema-elements/relay).

# Keep UUID values as UUID

Use `UUID` when the value is a UUID that clients can recognize as a UUID. Use `ID` when the value identifies a schema object and should remain opaque to clients.

```csharp
using HotChocolate.Types.Relay;

public sealed class ImportJob
{
    public Guid CorrelationId { get; init; }

    [ID]
    public Guid Id { get; init; }
}
```

Expected SDL:

```graphql
type ImportJob {
  correlationId: UUID!
  id: ID!
}
```

Choose `UUID` for tracking values, correlation values, external UUID fields, and values that clients may validate or compare as UUIDs. Use `ID` for entity identity in your GraphQL contract.

# Use Any only for intentional flexibility

Use `Any` when the shape is intentionally open, such as for metadata, extension bags, pass-through JSON, or interoperability boundaries. Prefer object types for selectable result shapes and input object types for known input shapes.

`JsonElement` maps to `Any`.

```csharp
using System.Text.Json;
using HotChocolate.Types;

[QueryType]
public static partial class MetadataQueries
{
    public static JsonElement Metadata(
        [GraphQLType<AnyType>] JsonElement input)
        => input;
}
```

Expected SDL:

```graphql
type Query {
  metadata(input: Any!): Any!
}
```

Example operation:

```graphql
query {
  metadata(input: { source: "mobile", flags: ["beta"] })
}
```

Example response:

```json
{
  "data": {
    "metadata": {
      "source": "mobile",
      "flags": ["beta"]
    }
  }
}
```

See [Built-in Scalars](./built-in-scalars) for details about `ArgumentValue<JsonElement>`, value kinds, dictionaries, object return values, and `AddJsonTypeConverter()`.

# Pick a package scalar before writing a custom format

Use the Additional Scalars Package when you need a common string-like format such as email, phone number, IP address, color, ISBN, MAC address, or UTC offset.

| Need                     | Package type examples                    | Route                                    |
| ------------------------ | ---------------------------------------- | ---------------------------------------- |
| Email or phone values    | `EmailAddressType`, `PhoneNumberType`    | [Community Scalars](./community-scalars) |
| Network values           | `IPv4Type`, `IPv6Type`, `MacAddressType` | [Community Scalars](./community-scalars) |
| Colors and catalog codes | `HexColorType`, `IsbnType`               | [Community Scalars](./community-scalars) |
| Offset strings           | `UtcOffsetType`                          | [Community Scalars](./community-scalars) |

Install and configure `HotChocolate.Types.Scalars` as described on the detailed page. Many package scalars use `string` as their CLR value, so you often need explicit scalar typing instead of relying on `string` inference.

# Use NodaTime when your model uses NodaTime

Use `HotChocolate.Types.NodaTime` when your application model uses NodaTime types and precision is important. Register the package with `AddNodaTime()`.

```csharp
builder
    .AddGraphQL()
    .AddNodaTime();
```

`AddNodaTime()` registers five spec-aligned scalars:

| GraphQL scalar  | NodaTime focus               |
| --------------- | ---------------------------- |
| `DateTime`      | Offset date and time values. |
| `Duration`      | NodaTime duration values.    |
| `LocalDateTime` | Local date and time values.  |
| `LocalDate`     | Local date values.           |
| `LocalTime`     | Local time values.           |

These scalars use NodaTime runtime types and can preserve up to 9 fractional second digits. `AddNodaTime()` does not bind `System.TimeSpan` to NodaTime `Duration`. See [NodaTime Scalars](./nodatime-scalars) for setup, mappings, options, and migration notes.

# Create a custom scalar only for a reusable leaf format

Create a custom scalar when you need a reusable, indivisible wire format with its own parse and serialize rules.

| Candidate                                                             | Better route when it does not fit                   |
| --------------------------------------------------------------------- | --------------------------------------------------- |
| Credit card token, postal code, TCP port, compact external identifier | Custom scalar, if the transport format is reusable. |
| Value with fields clients should select                               | [Object Types](../object-types).                    |
| Known structured input                                                | [Input Object Types](../input-object-types).        |
| Closed set of values                                                  | [Enums](../enums).                                  |
| Business validation on an ordinary value                              | Validation, directives, or package scalar.          |
| Different CLR representation for an existing scalar                   | Runtime type binding or type converter.             |
| Opaque entity identity                                                | `ID` plus Relay or global object identification.    |

The custom scalar page covers the implementation APIs, including `ScalarType<TRuntimeType>` and scalar coercion methods. This overview focuses on the decision process.

# Choose the route by problem

| What you want to model                                 | Use this                                          | Go to                                                                                                    |
| ------------------------------------------------------ | ------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Ordinary CLR primitives and BCL date or time values    | Inferred built-in scalars                         | [Built-in Scalars](./built-in-scalars)                                                                   |
| `Guid` as a value                                      | `UUID`                                            | [Built-in Scalars](./built-in-scalars)                                                                   |
| Opaque entity identifiers                              | `ID`, `[ID]`, `[ID<T>]`, Relay or global IDs      | [Built-in Scalars](./built-in-scalars), then [Relay](/docs/hotchocolate/v16/build/schema-elements/relay) |
| Arbitrary JSON-like values                             | `Any`                                             | [Built-in Scalars](./built-in-scalars)                                                                   |
| `Uri`, `byte[]`, date/time precision, UUID formats     | Built-in scalar options                           | [Built-in Scalars](./built-in-scalars)                                                                   |
| Email, phone, IP, color, ISBN, MAC address, UTC offset | `HotChocolate.Types.Scalars`                      | [Community Scalars](./community-scalars)                                                                 |
| NodaTime domain model                                  | `HotChocolate.Types.NodaTime` and `AddNodaTime()` | [NodaTime Scalars](./nodatime-scalars)                                                                   |
| File upload input                                      | `Upload` and `AddUploadType()`                    | [File uploads](/docs/hotchocolate/v16/_leagcy/server/files)                                              |
| Reusable custom leaf format                            | Custom scalar                                     | [Custom Scalars](./custom-scalars)                                                                       |
| Known input shape                                      | Input object type                                 | [Input Object Types](../input-object-types)                                                              |
| Selectable result shape                                | Object type                                       | [Object Types](../object-types)                                                                          |
| Closed set                                             | Enum                                              | [Enums](../enums)                                                                                        |

# Follow the scalar decision flow

Use this flow if you are unsure whether a scalar is the right schema element.

```text
Does the value have selectable fields?
  yes -> use an object type
  no  -> continue

Is the client sending a known structured value?
  yes -> use an input object type
  no  -> continue

Is the value a closed set?
  yes -> use an enum
  no  -> continue

Is the value an opaque entity identifier?
  yes -> use ID, then check Relay or global ID docs
  no  -> continue

Does a built-in scalar already match the CLR value?
  yes -> use the built-in scalar
  no  -> continue

Does the Additional Scalars Package or NodaTime match the domain?
  yes -> use that package scalar
  no  -> continue

Is only the CLR representation different?
  yes -> use runtime type binding or a type converter
  no  -> continue

Is the value a reusable indivisible wire format?
  yes -> create a custom scalar
  no  -> revisit object, input object, enum, validation, or ID modeling
```

# Troubleshoot scalar choices

| Symptom                                                                        | Cause                                             | What to do                                                                                                                                          |
| ------------------------------------------------------------------------------ | ------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| My `id` field is `String`, not `ID`.                                           | `ID` is explicit.                                 | Add `[ID]`, `[ID<T>]`, `[GraphQLType<IdType>]`, or equivalent type configuration.                                                                   |
| My `Guid` field is `UUID`, but I expected `ID`.                                | Plain `Guid` maps to `UUID`.                      | Mark opaque identifiers with `[ID]` or configure the field as `IdType`.                                                                             |
| My dictionary or POCO does not return as `Any`.                                | `Any` uses `JsonElement` as its runtime type.     | Use `JsonElement`, register `AddJsonTypeConverter()`, or model a specific object type.                                                              |
| My `URL`, `ByteArray`, `Json`, or `TimeSpan` scalar changed after upgrading.   | Several scalar mappings were renamed or merged.   | Review [Built-in Scalars](./built-in-scalars) and the [v15 to v16 migration guide](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16). |
| My NodaTime type is missing, or `TimeSpan` did not become NodaTime `Duration`. | NodaTime registrations are specific.              | Register `AddNodaTime()` and review [NodaTime Scalars](./nodatime-scalars).                                                                         |
| I only need validation for a string.                                           | Validation alone may not require a custom scalar. | Check package scalars, validation, directives, and custom scalar reuse before creating a new scalar.                                                |

# Choose your next step

1. Start with inferred CLR mappings in [Built-in Scalars](./built-in-scalars).
2. Clarify `ID` and `UUID` behavior in [Built-in Scalars](./built-in-scalars), then use [Relay and global object identification](/docs/hotchocolate/v16/build/schema-elements/relay) for node IDs and typed IDs.
3. Add common format scalars with [Community Scalars](./community-scalars).
4. Use NodaTime domain types with [NodaTime Scalars](./nodatime-scalars).
5. Author a custom scalar only after you rule out built-ins, package scalars, converters, object types, input object types, enums, validation, and Relay IDs.
6. For adjacent schema modeling, see [Object Types](../object-types), [Input Object Types](../input-object-types), [Enums](../enums), [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments), and [File uploads](/docs/hotchocolate/v16/_leagcy/server/files).
