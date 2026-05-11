---
title: "Built-in Scalars"
---

Hot Chocolate automatically maps common .NET types to GraphQL scalar types. When you define properties or arguments in your schema, the framework infers the appropriate GraphQL scalar based on the CLR type. This page details the available built-in scalars, their .NET mappings, and how to customize their behavior.

# Mapping C# Members to SDL

Hot Chocolate infers the GraphQL scalar type from your .NET property or parameter types. Here is an example of typical mappings:

```csharp
using HotChocolate.Types;

public class Product
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool InStock { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public Guid TrackingId { get; set; }
    public DateTimeOffset ShippedAt { get; set; }
    public Uri ImageUrl { get; set; } = default!;
    public byte[] Checksum { get; set; } = default!;
}
```

This C# class produces the following SDL:

```graphql
type Product {
  name: String!
  description: String
  inStock: Boolean!
  quantity: Int!
  price: Decimal!
  trackingId: UUID!
  shippedAt: DateTime!
  imageUrl: URI!
  checksum: Base64String!
}
```

The scalar name is determined by the CLR type (for example, `Guid` maps to `UUID`, not `ID`). Nullability is based on C# nullable annotations.

# Built-in Scalar Mapping Table

The table below lists how .NET types map to GraphQL scalars:

| .NET Type         | GraphQL Scalar  | Inferred Automatically | Client Value Shape            | Notes                                                        |
| ----------------- | --------------- | ---------------------- | ----------------------------- | ------------------------------------------------------------ |
| `string`          | `String`        | Yes                    | JSON string                   | GraphQL spec scalar                                          |
| `bool`            | `Boolean`       | Yes                    | JSON boolean                  | GraphQL spec scalar                                          |
| `int`             | `Int`           | Yes                    | JSON number (signed 32-bit)   | GraphQL spec scalar                                          |
| `float`, `double` | `Float`         | Yes                    | JSON number                   | GraphQL spec scalar. Not suitable for precise decimal values |
| `decimal`         | `Decimal`       | Yes                    | JSON number                   | Extended scalar for high-precision decimal values            |
| `long`            | `Long`          | Yes                    | JSON number (signed 64-bit)   | Extended scalar                                              |
| `short`           | `Short`         | Yes                    | JSON number (signed 16-bit)   | Extended scalar                                              |
| `sbyte`           | `Byte`          | Yes                    | JSON number (signed 8-bit)    | Extended scalar                                              |
| `byte`            | `UnsignedByte`  | Yes                    | JSON number (unsigned 8-bit)  | Extended scalar                                              |
| `ushort`          | `UnsignedShort` | Yes                    | JSON number (unsigned 16-bit) | Extended scalar                                              |
| `uint`            | `UnsignedInt`   | Yes                    | JSON number (unsigned 32-bit) | Extended scalar                                              |
| `ulong`           | `UnsignedLong`  | Yes                    | JSON number (unsigned 64-bit) | Extended scalar                                              |
| `DateOnly`        | `LocalDate`     | Yes                    | String (`yyyy-MM-dd`)         | Calendar date without time or time zone                      |
| `DateTime`        | `DateTime`      | Yes                    | ISO 8601 string with offset   | Offset-aware timestamp                                       |
| `DateTimeOffset`  | `DateTime`      | Yes                    | ISO 8601 string with offset   | Offset-aware timestamp                                       |
| `TimeOnly`        | `LocalTime`     | Yes                    | String (`HH:mm:ss`)           | Time of day without date                                     |
| `TimeSpan`        | `Duration`      | Yes                    | ISO 8601 duration string      | Elapsed time duration                                        |
| `Guid`            | `UUID`          | Yes                    | String (formatted GUID)       | Universally unique identifier                                |
| `Uri`             | `URI`           | Yes                    | String (URI)                  | Absolute or relative URI                                     |
| `byte[]`          | `Base64String`  | Yes                    | String (base64 encoded)       | Binary data as base64                                        |
| `JsonElement`     | `Any`           | Yes                    | Any JSON value                | Arbitrary JSON-like values                                   |
| `object`          | `Any`           | Yes                    | Any JSON value                | Arbitrary JSON-like values                                   |
| (none)            | `ID`            | No                     | String                        | Explicit binding required for graph identifiers              |

**Note:** Hot Chocolate only includes scalar definitions that are actually used in your schema. Unused scalars are not present.

# GraphQL spec scalars

## String

The `String` scalar represents textual data as UTF-8 character sequences. It maps automatically from `string`:

```csharp
public class Author
{
    public string Name { get; set; } = default!;
}
```

```graphql
type Author {
  name: String!
}
```

Clients send and receive JSON strings:

```json
{
  "name": "Jane Doe"
}
```

## Boolean

The `Boolean` scalar represents `true` or `false` values. It maps automatically from `bool`:

```csharp
public class Feature
{
    public bool IsEnabled { get; set; }
}
```

```graphql
type Feature {
  isEnabled: Boolean!
}
```

Clients send and receive JSON booleans:

```json
{
  "isEnabled": true
}
```

## Int

The `Int` scalar represents signed 32-bit integer values. It maps automatically from `int`:

```csharp
public class Inventory
{
    public int Quantity { get; set; }
}
```

```graphql
type Inventory {
  quantity: Int!
}
```

Clients send and receive JSON numbers within the signed 32-bit range.

For wider or unsigned integer types, Hot Chocolate provides extended scalars like `Long`, `UnsignedInt`, and others listed in the mapping table.

## Float

The `Float` scalar represents signed double-precision floating-point values. It maps automatically from `float` and `double`:

```csharp
public class Review
{
    public double Rating { get; set; }
}
```

```graphql
type Review {
  rating: Float!
}
```

Clients send and receive JSON numbers.

**Caution:** `Float` is not suitable for precise decimal values like currency or prices. Use `Decimal` for those domains.

## ID

The `ID` scalar represents a unique identifier for an object in your graph. It carries semantic meaning for object identity, refetching, and client caching.

Unlike other scalars, `ID` is **not** inferred automatically. You must bind it explicitly even if you use `int`, `Guid`, or `string` as the runtime type.

**Key behaviors:**

- Serializes as a JSON string in responses
- Accepts both string and integer values as input
- Rejects float-like numeric input

**Common gotcha:** `Guid` properties map to `UUID`, not `ID`. If you want graph identifier semantics, you need explicit binding.

# Use ID explicitly

To expose an `ID` scalar in your schema, use the `[GraphQLType<IdType>]` attribute or the `[ID]` attribute:

```csharp
using HotChocolate.Types;

public class Product
{
    [GraphQLType<IdType>]
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class Query
{
    public Product? GetProduct([GraphQLType<IdType>] int id)
    {
        // implementation
        return null;
    }
}
```

For strongly typed IDs, you can also use the generic `[ID<T>]` attribute:

```csharp
using HotChocolate.Types;

public class Product
{
    [ID<Product>]
    public int Id { get; set; }
}

public class Query
{
    public Product? GetProduct([ID<Product>] int id)
    {
        // implementation
        return null;
    }
}
```

Both approaches generate the same SDL:

```graphql
type Product {
  id: ID!
}

type Query {
  product(id: ID!): Product
}
```

Clients can send IDs as strings or integers:

```json
{
  "id": "123"
}
```

```json
{
  "id": 123
}
```

# Numeric scalars beyond the spec

Hot Chocolate extends the GraphQL specification with additional numeric scalars for wider ranges and unsigned values:

| Scalar          | .NET Type | Range                  | Common Use                                     | Client Consideration                                       |
| --------------- | --------- | ---------------------- | ---------------------------------------------- | ---------------------------------------------------------- |
| `Decimal`       | `decimal` | High-precision decimal | Prices, financial values, precise measurements | JavaScript clients may need special handling for precision |
| `Long`          | `long`    | Signed 64-bit          | Large counts, IDs, timestamps                  | JavaScript numbers lose precision above 2^53               |
| `Short`         | `short`   | Signed 16-bit          | Small integer values                           |                                                            |
| `Byte`          | `sbyte`   | Signed 8-bit           | Small signed values                            |                                                            |
| `UnsignedByte`  | `byte`    | Unsigned 8-bit         | Flags, small unsigned values                   |                                                            |
| `UnsignedShort` | `ushort`  | Unsigned 16-bit        | Port numbers, unsigned counts                  |                                                            |
| `UnsignedInt`   | `uint`    | Unsigned 32-bit        | Large unsigned counts                          |                                                            |
| `UnsignedLong`  | `ulong`   | Unsigned 64-bit        | Very large unsigned values                     | JavaScript numbers lose precision above 2^53               |

## Decimal versus Float

Use `Decimal` for precise decimal arithmetic and `Float` for approximate floating-point values:

```csharp
public class ProductPrice
{
    public decimal Price { get; set; }      // Maps to Decimal
    public double Rating { get; set; }       // Maps to Float
}
```

```graphql
type ProductPrice {
  price: Decimal!
  rating: Float!
}
```

While `Decimal` helps preserve precision, remember that accurate money handling also requires currency information and appropriate domain modeling.

## Long integers and client considerations

```csharp
public class Metrics
{
    public long InventoryCount { get; set; }
}
```

```graphql
type Metrics {
  inventoryCount: Long!
}
```

**Warning:** JavaScript clients may lose precision with 64-bit integer values above 2^53. Consider using strings for large identifiers or implement special serialization for large numeric values.

# Date and time scalars

Hot Chocolate provides several date and time scalars for different temporal concepts:

| Domain Value           | .NET Type                    | Inferred Scalar | Example SDL Value           | Notes                        |
| ---------------------- | ---------------------------- | --------------- | --------------------------- | ---------------------------- |
| Offset-aware timestamp | `DateTime`, `DateTimeOffset` | `DateTime`      | `2024-01-15T14:30:00+01:00` | Includes time zone offset    |
| Local calendar date    | `DateOnly`                   | `LocalDate`     | `2024-01-15`                | Date without time or offset  |
| Local date and time    | Explicit scalar binding      | `LocalDateTime` | `2024-01-15T14:30:00`       | Date and time without offset |
| Local time             | `TimeOnly`                   | `LocalTime`     | `14:30:00`                  | Time without date or offset  |
| Duration               | `TimeSpan`                   | `Duration`      | `PT2H30M`                   | ISO 8601 duration            |

Example:

```csharp
public class Shipment
{
    public DateTimeOffset ShippedAt { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public TimeSpan TransitDuration { get; set; }
}
```

```graphql
type Shipment {
  shippedAt: DateTime!
  deliveryDate: LocalDate!
  pickupTime: LocalTime!
  transitDuration: Duration!
}
```

The `Date` scalar is also built in for date-only values. `DateOnly` maps to `LocalDate` by default, so bind `DateType` explicitly when the schema contract should use `Date`.

For additional date and time types using NodaTime, see the [NodaTime Scalars](/docs/hotchocolate/v16/build/type-system/scalars/nodatime-scalars) documentation.

## Configure date/time precision

The `DateTime`, `LocalDateTime`, and `LocalTime` scalars accept configuration through `DateTimeOptions`:

```csharp
builder
    .AddGraphQL()
    .AddType(new DateTimeType(new DateTimeOptions
    {
        OutputPrecision = 3,
        InputPrecision = 9,
        ValidateInputFormat = true
    }))
    .AddType(new LocalDateTimeType(new DateTimeOptions
    {
        OutputPrecision = 3
    }))
    .AddType(new LocalTimeType(new DateTimeOptions
    {
        OutputPrecision = 3
    }));
```

**Options:**

| Option                | Description                               | Default | Maximum |
| --------------------- | ----------------------------------------- | ------- | ------- |
| `OutputPrecision`     | Fractional second digits in output        | 7       | 7       |
| `InputPrecision`      | Maximum fractional second digits accepted | 9       | 9       |
| `ValidateInputFormat` | Enforce strict input format validation    | `true`  | -       |

**Important:** .NET BCL date and time types preserve up to 7 fractional second digits. If you accept higher input precision (for example, 9 digits), the extra digits may be rounded during parsing.

# Identifiers and UUIDs

Hot Chocolate provides two distinct scalars for identifier values:

| Scalar | Purpose                   | .NET Mapping       | Serialization           | Input Formats     |
| ------ | ------------------------- | ------------------ | ----------------------- | ----------------- |
| `ID`   | Semantic graph identifier | None (explicit)    | String                  | String or integer |
| `UUID` | Concrete GUID value       | `Guid` (automatic) | String (formatted GUID) | GUID string       |

**When to use:**

- Use `UUID` when your schema exposes a GUID-shaped value that clients need to parse or generate as a GUID
- Use `ID` when your schema exposes an opaque graph identifier for refetching, caching, or relating objects
- Do not rely on property names or the CLR `Guid` type to produce `ID` automatically

## Configure UUID format

By default, UUIDs serialize in the hyphenated format (`D`). You can configure a different format:

```csharp
builder
    .AddGraphQL()
    .AddType(new UuidType('N'));
```

**Supported formats:**

| Format | Description          | Example                                  |
| ------ | -------------------- | ---------------------------------------- |
| `N`    | 32 digits            | `00000000000000000000000000000000`       |
| `D`    | Hyphenated (default) | `00000000-0000-0000-0000-000000000000`   |
| `B`    | Braces               | `{00000000-0000-0000-0000-000000000000}` |
| `P`    | Parentheses          | `(00000000-0000-0000-0000-000000000000)` |

You can also enforce that input values match the configured format:

```csharp
builder
    .AddGraphQL()
    .AddType(new UuidType('N', enforceFormat: true));
```

# URI values

The `URI` scalar maps automatically from `Uri` and accepts both relative and absolute URIs:

```csharp
public class Document
{
    public Uri ImageUrl { get; set; } = default!;
    public Uri DocumentationUrl { get; set; } = default!;
}
```

```graphql
type Document {
  imageUrl: URI!
  documentationUrl: URI!
}
```

**Serialization behavior:**

- Absolute URIs serialize using `AbsoluteUri`
- Relative URIs serialize using `ToString()`

Clients send and receive URI values as JSON strings:

```json
{
  "imageUrl": "https://example.com/product.jpg",
  "documentationUrl": "/docs/readme"
}
```

# Byte arrays and base64 strings

The `Base64String` scalar maps automatically from `byte[]` and transports binary data as base64-encoded strings:

```csharp
public class File
{
    public byte[] Checksum { get; set; } = default!;
}
```

```graphql
type File {
  checksum: Base64String!
}
```

Clients send and receive base64-encoded strings:

```json
{
  "checksum": "SGVsbG8gV29ybGQ="
}
```

**Warning:** `Base64String` is suitable for small binary values like checksums or hashes. For large file uploads or downloads, use dedicated file upload mechanisms instead.

# Arbitrary JSON-like values with Any

The `Any` scalar accepts and returns arbitrary JSON-like values. It maps automatically from `JsonElement` and `object`:

```csharp
public class Resource
{
    public JsonElement Metadata { get; set; }
}

public class Query
{
    public Resource[] SearchResources([GraphQLType<AnyType>] object filter)
    {
        // implementation
        return [];
    }
}
```

```graphql
type Resource {
  metadata: Any!
}

type Query {
  searchResources(filter: Any): [Resource!]!
}
```

**Accepted value shapes:**

- Objects
- Lists
- Strings
- Integers
- Floats
- Booleans
- JSON input values

Clients can send structured values:

```json
{
  "filter": {
    "category": "electronics",
    "priceRange": [100, 500]
  }
}
```

**Gotcha:** By default, `Any` centers on `JsonElement`. If you need to return dictionaries or arbitrary .NET objects, you may need to register converters:

```csharp
builder
    .AddGraphQL()
    .AddJsonTypeConverter();
```

For complex `Any` scenarios, prefer `JsonElement` first. Register `AddJsonTypeConverter()` when you need dictionaries or other JSON-serializable .NET objects.

# Override an inferred scalar

If automatic scalar inference does not match your needs, override it with the `[GraphQLType<T>]` attribute:

```csharp
using HotChocolate.Types;

public class Schedule
{
    // DateOnly maps to LocalDate by default. Use DateType when the schema contract needs Date.
    [GraphQLType<DateType>]
    public DateOnly BusinessDate { get; set; }
}
```

For more complex field configuration, see [Object Types](../object-types#choose-attributes-or-descriptors).

To create your own scalar types, see [Custom Scalars](/docs/hotchocolate/v16/build/type-system/scalars/custom-scalars).

# Nullability and generated SDL

Scalar inference determines the GraphQL type name, while C# nullability determines whether the SDL type is nullable or non-null:

```csharp
#nullable enable

public class Product
{
    public string Name { get; set; } = default!;     // String!
    public string? Description { get; set; }          // String
    public int Quantity { get; set; }                 // Int!
    public int? OptionalQuantity { get; set; }        // Int
}
```

```graphql
type Product {
  name: String!
  description: String
  quantity: Int!
  optionalQuantity: Int
}
```

The exact nullable SDL output depends on your project's nullable reference type settings and Hot Chocolate conventions.

# Troubleshooting built-in scalars

## My Guid field is UUID, not ID

This is expected behavior. `Guid` maps automatically to `UUID`, which represents a concrete universally unique identifier value.

If you need graph identifier semantics for refetching and caching, use explicit `ID` binding:

```csharp
[GraphQLType<IdType>]
public Guid Id { get; set; }
```

Or use the generic attribute:

```csharp
[ID<Product>]
public Guid Id { get; set; }
```

## My ID argument rejects a value

The `ID` scalar accepts strings and integers, but rejects float-like numeric input.

**Accepted:**

```json
{ "id": "123" }
{ "id": 123 }
```

**Rejected:**

```json
{ "id": 123.45 }
```

## My DateOnly field is not the scalar I expected

Hot Chocolate maps `DateOnly` to `LocalDate`. If you expected `Date`, you can override the mapping explicitly:

```csharp
[GraphQLType<DateType>]
public DateOnly MyDate { get; set; }
```

Both `Date` and `LocalDate` use the `yyyy-MM-dd` format, but they carry different semantic descriptions in the schema.

## My date/time input is rejected

By default, date and time scalars validate input format strictly. If you need to accept looser formats, configure `ValidateInputFormat`:

```csharp
builder
    .AddGraphQL()
    .AddType(new DateTimeType(new DateTimeOptions
    {
        ValidateInputFormat = false
    }));
```

## My timestamp lost fractional precision

Output precision is limited by `OutputPrecision` (default 7, maximum 7). If you need fewer digits in output, configure it:

```csharp
builder
    .AddGraphQL()
    .AddType(new DateTimeType(new DateTimeOptions
    {
        OutputPrecision = 3  // milliseconds only
    }));
```

Remember that .NET BCL date and time values preserve up to 7 fractional second digits. If you accept higher input precision (via `InputPrecision`), extra digits will be rounded during parsing.

## My price became Float

The `double` and `float` types map to `Float`, which uses approximate floating-point arithmetic.

For precise decimal values like prices, use `decimal`:

```csharp
public decimal Price { get; set; }  // Maps to Decimal
```

## My byte[] is unreadable on the client

The `Base64String` scalar transports byte arrays as base64-encoded strings. Clients must decode the base64 string to access the original bytes.

Most client libraries provide base64 decoding utilities. For example, in JavaScript:

```javascript
const bytes = atob(base64String);
```

## I returned a dictionary from an Any field and it failed

By default, `Any` expects `JsonElement` values. To return dictionaries or arbitrary .NET objects, register the JSON type converter:

```csharp
builder
    .AddGraphQL()
    .AddJsonTypeConverter();
```

For more complex cases, decide whether `Any` is still the right contract or whether a specific object or input object type would be clearer.

# Next steps

- [Custom Scalars](/docs/hotchocolate/v16/build/type-system/scalars/custom-scalars) - Create your own scalar types for domain-specific values
- [NodaTime Scalars](/docs/hotchocolate/v16/build/type-system/scalars/nodatime-scalars) - Use NodaTime types for advanced date and time handling
- [Community Scalars](/docs/hotchocolate/v16/build/type-system/scalars/community-scalars) - Discover additional scalar types from the community
- [Object Types](../object-types#choose-attributes-or-descriptors) - Learn advanced field configuration with attributes and descriptors
