---
title: "Scalars"
---

Scalars are the leaf types in a GraphQL schema. They represent concrete values like strings, numbers, and dates. Unlike object types, scalars cannot be decomposed further. They are where the query ends and actual data is returned.

Every scalar defines how values convert between the GraphQL wire format (JSON) and the .NET runtime representation. GraphQL includes five built-in scalars (`String`, `Int`, `Float`, `Boolean`, and `ID`), and Hot Chocolate adds many more for common .NET types.

# .NET Type to GraphQL Scalar Mapping

Hot Chocolate automatically maps .NET types to GraphQL scalars. When you use a `string` property, it becomes a `String` field in your schema without any configuration.

| .NET Type         | GraphQL Scalar  | Notes                                                                     |
| ----------------- | --------------- | ------------------------------------------------------------------------- |
| `string`          | `String`        | UTF-8 character sequence                                                  |
| `bool`            | `Boolean`       | `true` or `false`                                                         |
| `int`             | `Int`           | Signed 32-bit integer                                                     |
| `float`, `double` | `Float`         | IEEE 754 double-precision                                                 |
| `decimal`         | `Decimal`       | High-precision decimal (separate from `Float`)                            |
| `long`            | `Long`          | Signed 64-bit integer                                                     |
| `short`           | `Short`         | Signed 16-bit integer                                                     |
| `DateTime`        | `DateTime`      | Date and time with time zone offset                                       |
| `DateTimeOffset`  | `DateTime`      | Date and time with time zone offset                                       |
| `DateOnly`        | `Date`          | Date without time or time zone                                            |
| `TimeOnly`        | `LocalTime`     | Time of day without date or time zone                                     |
| `TimeSpan`        | `Duration`      | Duration of time (renamed from `TimeSpan` in v16)                         |
| `Guid`            | `UUID`          | Universally unique identifier (RFC 9562)                                  |
| `Uri`             | `URI`           | Uniform resource identifier (new in v16, replaces `URL` for `System.Uri`) |
| `byte[]`          | `Base64String`  | Base64-encoded byte array (new in v16, replaces deprecated `ByteArray`)   |
| `byte`            | `UnsignedByte`  | Unsigned 8-bit integer (renamed in v16)                                   |
| `sbyte`           | `Byte`          | Signed 8-bit integer (renamed in v16)                                     |
| `ushort`          | `UnsignedShort` | Unsigned 16-bit integer (new in v16)                                      |
| `uint`            | `UnsignedInt`   | Unsigned 32-bit integer (new in v16)                                      |
| `ulong`           | `UnsignedLong`  | Unsigned 64-bit integer (new in v16)                                      |
| `JsonElement`     | `Any`           | Any valid GraphQL value (v16 merged `Json` into `Any`)                    |

Hot Chocolate only exposes scalars that your schema uses. Unused scalars do not appear in the generated schema.

# Built-in Spec Scalars

## String

Represents a UTF-8 character sequence. Automatically inferred from `string`.

```sdl
type Product {
  description: String
}
```

## Boolean

Represents `true` or `false`. Automatically inferred from `bool`.

```sdl
type Product {
  purchasable: Boolean
}
```

## Int

Represents a signed 32-bit integer. Automatically inferred from `int`.

```sdl
type Product {
  quantity: Int
}
```

## Float

Represents double-precision fractional values (IEEE 754). Automatically inferred from `float` or `double`.

```sdl
type Product {
  price: Float
}
```

Hot Chocolate provides a separate `Decimal` scalar for `decimal` values, giving you higher precision than `Float`.

## ID

Represents a unique identifier. The `ID` type is **not** automatically inferred. You must annotate it explicitly.

`ID` values are always serialized as strings in client-server communication, but you can use any CLR type (`int`, `string`, `Guid`) on the server side. This allows you to change the underlying type without affecting the schema or your clients.

<ExampleTabs>
<Implementation>

```csharp
// Types/Product.cs
public sealed class Product
{
    [GraphQLType<IdType>]
    public int Id { get; set; }
}

// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product GetProduct([GraphQLType<IdType>] int id)
    {
        // Omitted code for brevity
    }
}
```

</Implementation>
<Code>

```csharp
// Types/Product.cs
public sealed class Product
{
    public int Id { get; set; }
}

// Types/ProductType.cs
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("Product");
        descriptor.Field(f => f.Id).Type<IdType>();
    }
}

// Types/QueryType.cs
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Argument("id", a => a.Type<IdType>())
            .Type<ProductType>()
            .Resolve(context =>
            {
                var id = context.ArgumentValue<int>("id");

                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

```csharp
// Types/Product.cs
public sealed class Product
{
    public int Id { get; set; }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(
        """
        type Query {
            product(id: ID): Product
        }

        type Product {
            id: ID
        }
        """)
    .BindRuntimeType<Product>()
    .AddResolver("Query", "product", context =>
    {
        var id = context.ArgumentValue<int>("id");

        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>

[Learn more about explicit types](/docs/hotchocolate/v16/building-a-schema/object-types#explicit-types)

# Extended Scalars

Beyond the five spec scalars, Hot Chocolate provides these scalars out of the box:

| Type                | Description                                  |
| ------------------- | -------------------------------------------- |
| [Any][1]            | Represents any valid GraphQL value           |
| [Base64String][2]   | Base64-encoded byte array                    |
| [Byte][3]           | Signed 8-bit integer                         |
| [Date][4]           | Date in UTC                                  |
| [DateTime][5]       | Date and time with time zone offset          |
| [Decimal][6]        | High-precision decimal floating-point number |
| [Duration][7]       | Duration of time                             |
| [LocalDate][8]      | Date without time or time zone               |
| [LocalDateTime][9]  | Date and time without time zone              |
| [LocalTime][10]     | Time of day without date or time zone        |
| [Long][11]          | Signed 64-bit integer                        |
| [Short][12]         | Signed 16-bit integer                        |
| [UnsignedByte][13]  | Unsigned 8-bit integer                       |
| [UnsignedInt][14]   | Unsigned 32-bit integer                      |
| [UnsignedLong][15]  | Unsigned 64-bit integer                      |
| [UnsignedShort][16] | Unsigned 16-bit integer                      |
| [URI][17]           | Uniform resource identifier (RFC 3986)       |
| [UUID][19]          | Universally unique identifier (RFC 9562)     |

[1]: https://scalars.graphql.org/chillicream/any.html
[2]: https://scalars.graphql.org/chillicream/base64-string.html
[3]: https://scalars.graphql.org/chillicream/byte.html
[4]: https://scalars.graphql.org/chillicream/date.html
[5]: https://scalars.graphql.org/chillicream/date-time.html
[6]: https://scalars.graphql.org/chillicream/decimal.html
[7]: https://scalars.graphql.org/chillicream/duration.html
[8]: https://scalars.graphql.org/chillicream/local-date.html
[9]: https://scalars.graphql.org/chillicream/local-date-time.html
[10]: https://scalars.graphql.org/chillicream/local-time.html
[11]: https://scalars.graphql.org/chillicream/long.html
[12]: https://scalars.graphql.org/chillicream/short.html
[13]: https://scalars.graphql.org/chillicream/unsigned-byte.html
[14]: https://scalars.graphql.org/chillicream/unsigned-int.html
[15]: https://scalars.graphql.org/chillicream/unsigned-long.html
[16]: https://scalars.graphql.org/chillicream/unsigned-short.html
[17]: https://scalars.graphql.org/chillicream/uri.html
[19]: https://scalars.graphql.org/chillicream/uuid.html

## DateTime Configuration

`HotChocolate.Types.DateTimeOptions` configures the built-in BCL-backed `DateTime`, `LocalDateTime`, and `LocalTime` scalars:

- `InputPrecision` controls how many fractional second digits are accepted during parsing, up to `9`.
- `OutputPrecision` controls how many fractional second digits are written during serialization, up to `7`.
- `ValidateInputFormat` controls whether input is validated against the expected scalar format before parsing.

Although the built-in scalars can parse up to 9 fractional second digits, the underlying BCL types only preserve up to 7 digits (100-nanosecond precision), so additional digits are rounded during parsing.

To customize the built-in scalars, register configured scalar instances explicitly:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddType(new DateTimeType(new DateTimeOptions
    {
        OutputPrecision = 3
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

<Video videoId="gO3bNKBmXZM" />

## UUID Format

The `UUID` scalar supports multiple serialization formats:

| Specifier   | Format                                                               |
| ----------- | -------------------------------------------------------------------- |
| N           | 00000000000000000000000000000000                                     |
| D (default) | 00000000-0000-0000-0000-000000000000                                 |
| B           | {00000000-0000-0000-0000-000000000000}                               |
| P           | (00000000-0000-0000-0000-000000000000)                               |
| X           | {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} |

The `UuidType` always returns values in the specified format. When parsing input, it tries the specified format first, then falls back to other formats.

To change the default format:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddType(new UuidType('N'));
```

## The Any Scalar

The `Any` scalar is comparable to `object` in C#. It accepts any literal and can return any output type.

```sdl
type Query {
  metadata(filter: Any): Any
}
```

All of the following queries are valid against an `Any` argument:

```graphql
{
  a: metadata(filter: 1)
  b: metadata(filter: [1, 2, 3])
  c: metadata(filter: "text")
  d: metadata(filter: true)
  e: metadata(filter: { key: "value", nested: { count: 1 } })
}
```

### Runtime type

The `Any` scalar uses `System.Text.Json.JsonElement` as its .NET runtime type. Fields annotated with `Any` expect resolvers to return a `JsonElement`.

To access an argument dynamically:

```csharp
// Types/MetadataQueries.cs
JsonElement value = context.ArgumentValue<JsonElement>("filter");

if (value.ValueKind == JsonValueKind.Object)
{
    string? name = value.GetProperty("name").GetString();
}
```

To deserialize into a strongly typed model:

```csharp
MyFilter filter = context.ArgumentValue<MyFilter>("filter");
```

You can also inspect the value kind to determine how the argument was provided:

```csharp
ValueKind kind = context.ArgumentKind("filter");
```

The `ValueKind` enum tells you which kind of literal represents the argument:

```csharp
public enum ValueKind
{
    String,
    Integer,
    Float,
    Boolean,
    Enum,
    Object,
    Null
}
```

> An integer literal can contain a long value, and a float literal can be a decimal or a float.

### Returning dictionaries and arbitrary .NET types

By default, `Any` expects a `JsonElement`. To return common .NET types such as `Dictionary<string, object>` or `ExpandoObject`, register the JSON type converter:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddJsonTypeConverter();
```

With the converter registered, resolvers can return dictionaries or any JSON-serializable object:

```csharp
// Types/MetadataQueries.cs
[GraphQLType<AnyType>]
public object GetData() => new Dictionary<string, object>
{
    { "name", "John" },
    { "age", 30 }
};
```

### Custom type serialization

For custom reference types, register a dedicated converter to control serialization. For example, to serialize `TimeZoneInfo` as its string ID instead of a full JSON object:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeConverter<TimeZoneInfo, JsonElement>(
        value => JsonSerializer.SerializeToElement(value.Id));
```

The resolver can then return the type directly:

```csharp
// Types/SettingsQueries.cs
[GraphQLType<AnyType>]
public TimeZoneInfo GetTimezone() => TimeZoneInfo.Utc; // serializes as "UTC"
```

# Additional Scalars Package

For more specific use cases, install the `HotChocolate.Types.Scalars` package:

<PackageInstallation packageName="HotChocolate.Types.Scalars" />

| Type         | Description                                                                                                       |
| ------------ | ----------------------------------------------------------------------------------------------------------------- |
| EmailAddress | Email address as defined in [RFC 5322](https://tools.ietf.org/html/rfc5322)                                       |
| HexColor     | HEX color code                                                                                                    |
| Hsl          | CSS HSL color as defined [here][20]                                                                               |
| Hsla         | CSS HSLA color as defined [here][20]                                                                              |
| IPv4         | IPv4 address as defined [here](https://en.wikipedia.org/wiki/IPv4)                                                |
| IPv6         | IPv6 address as defined in [RFC 8064](https://tools.ietf.org/html/rfc8064)                                        |
| Isbn         | ISBN-10 or ISBN-13 number as defined [here](https://en.wikipedia.org/wiki/International_Standard_Book_Number)     |
| Latitude     | Decimal degrees latitude number                                                                                   |
| Longitude    | Decimal degrees longitude number                                                                                  |
| MacAddress   | IEEE 802 48-bit (MAC-48/EUI-48) and 64-bit (EUI-64) Mac addresses as defined in [RFC 7042][21] and [RFC 7043][22] |
| PhoneNumber  | E.164 format phone number as defined [here](https://en.wikipedia.org/wiki/E.164)                                  |
| Rgb          | CSS RGB color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)                |
| Rgba         | CSS RGBA color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)               |
| UtcOffset    | A value of format `±hh:mm`                                                                                        |

[20]: https://developer.mozilla.org/docs/Web/CSS/color_value#hsl_colors
[21]: https://tools.ietf.org/html/rfc7042#page-19
[22]: https://tools.ietf.org/html/rfc7043

Many of these scalars are built on native .NET types. An email address, for example, is represented as a `string`, but returning a `string` from your resolver causes Hot Chocolate to interpret it as a `StringType`. You need to specify the scalar type explicitly:

```csharp
// Types/UserQueries.cs
[GraphQLType<EmailAddressType>]
public string GetEmail() => "test@example.com";
```

[Learn more about explicit types](/docs/hotchocolate/v16/building-a-schema/object-types#explicit-types)

# NodaTime Scalars

For [NodaTime](https://github.com/nodatime/nodatime) types, install the dedicated package:

<PackageInstallation packageName="HotChocolate.Types.NodaTime" />

`HotChocolate.Types.NodaTime` provides alternative implementations of the same five built-in date and time scalars defined by the specifications on [scalars.graphql.org](https://scalars.graphql.org/):

| GraphQL Scalar     | NodaTime Runtime Type                                                         | Replaces Built-in Mapping |
| ------------------ | ----------------------------------------------------------------------------- | ------------------------- |
| [DateTime][5]      | [OffsetDateTime](https://nodatime.org/3.2.x/api/NodaTime.OffsetDateTime.html) | `DateTimeOffset`          |
| [Duration][7]      | [Duration](https://nodatime.org/3.2.x/api/NodaTime.Duration.html)             | `TimeSpan`                |
| [LocalDate][8]     | [LocalDate](https://nodatime.org/3.2.x/api/NodaTime.LocalDate.html)           | `DateOnly`                |
| [LocalDateTime][9] | [LocalDateTime](https://nodatime.org/3.2.x/api/NodaTime.LocalDateTime.html)   | `DateTime`                |
| [LocalTime][10]    | [LocalTime](https://nodatime.org/3.2.x/api/NodaTime.LocalTime.html)           | `TimeOnly`                |

These NodaTime scalars expose the same `@specifiedBy` URLs and implement the same GraphQL scalar specifications as the built-in versions, but they use NodaTime runtime types and may differ subtly in behavior. For example, the NodaTime implementations support up to 9 fractional second digits (nanosecond precision), whereas the equivalent BCL types only support up to 7 fractional second digits (100-nanosecond precision).

Register them with `AddNodaTime()`:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddNodaTime();
```

`AddNodaTime()` registers the five scalar types above and configures the related CLR bindings and converters automatically.

If you prefer, you can still register individual scalar types explicitly. For example:

```csharp
// Program.cs
using NodaTimeDurationType = HotChocolate.Types.NodaTime.DurationType;

builder.Services
    .AddGraphQLServer()
    .AddType<NodaTimeDurationType>();
```

## NodaTime scalar options

`HotChocolate.Types.NodaTime.DateTimeOptions` configures the NodaTime-backed `DateTime`, `LocalDateTime`, and `LocalTime` scalars:

- `InputPrecision` controls how many fractional second digits are accepted during parsing, up to `9`.
- `OutputPrecision` controls how many fractional second digits are written during serialization, up to `9`.

Unlike the built-in BCL-backed scalars, the NodaTime implementations preserve up to 9 fractional second digits (nanosecond precision).

If you need non-default NodaTime precision settings, register those scalar types individually instead of using `AddNodaTime()`:

```csharp
// Program.cs
using NodaTimeDateTimeOptions = HotChocolate.Types.NodaTime.DateTimeOptions;
using NodaTimeDateTimeType = HotChocolate.Types.NodaTime.DateTimeType;
using NodaTimeLocalDateTimeType = HotChocolate.Types.NodaTime.LocalDateTimeType;
using NodaTimeLocalTimeType = HotChocolate.Types.NodaTime.LocalTimeType;

builder.Services
    .AddGraphQLServer()
    .AddType(new NodaTimeDateTimeType(new NodaTimeDateTimeOptions
    {
        OutputPrecision = 3
    }))
    .AddType(new NodaTimeLocalDateTimeType(new NodaTimeDateTimeOptions
    {
        OutputPrecision = 3
    }))
    .AddType(new NodaTimeLocalTimeType(new NodaTimeDateTimeOptions
    {
        OutputPrecision = 3
    }));
```

# Binding Behavior

You can override the default .NET-to-scalar mappings by specifying type bindings explicitly:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .BindRuntimeType<string, StringType>();
```

You can also bind scalars to arrays or complex types:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .BindRuntimeType<byte[], Base64StringType>();
```

# Custom Converters

You can reuse existing scalar types with different runtime types by registering converters. For example, to map NodaTime's `OffsetDateTime` to the existing `DateTimeType`:

```csharp
// Types/ScheduleQueries.cs
public sealed class ScheduleQueries
{
    public OffsetDateTime GetDateTime(OffsetDateTime offsetDateTime)
    {
        return offsetDateTime;
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ScheduleQueries>()
    .BindRuntimeType<OffsetDateTime, DateTimeType>()
    .AddTypeConverter<OffsetDateTime, DateTimeOffset>(
        x => x.ToDateTimeOffset())
    .AddTypeConverter<DateTimeOffset, OffsetDateTime>(
        x => OffsetDateTime.FromDateTimeOffset(x));
```

# Custom Scalars

A custom scalar converts values between the GraphQL wire format and a .NET runtime type. Each custom scalar handles four conversion scenarios:

| Method                 | Direction               | Purpose                                                           |
| ---------------------- | ----------------------- | ----------------------------------------------------------------- |
| `OnCoerceInputLiteral` | GraphQL literal to .NET | Parses values embedded in a query, e.g. `{ field(arg: "value") }` |
| `OnCoerceInputValue`   | JSON to .NET            | Parses values provided as variables in the request                |
| `OnCoerceOutputValue`  | .NET to JSON            | Writes resolver results to the response                           |
| `OnValueToLiteral`     | .NET to GraphQL literal | Converts default values for schema introspection                  |

Extend `ScalarType<TRuntimeType, TLiteral>` to create a custom scalar:

```csharp
// Types/CreditCardNumberType.cs
public sealed class CreditCardNumberType : ScalarType<string, StringValueNode>
{
    private readonly ICreditCardValidator _validator;

    // You can inject services registered with the DI container
    public CreditCardNumberType(ICreditCardValidator validator)
        : base("CreditCardNumber")
    {
        _validator = validator;
        Description = "Represents a credit card number";
    }

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        AssertCreditCardNumberFormat(valueLiteral.Value);
        return valueLiteral.Value;
    }

    protected override string OnCoerceInputValue(
        JsonElement inputValue,
        IFeatureProvider context)
    {
        var value = inputValue.GetString()!;
        AssertCreditCardNumberFormat(value);
        return value;
    }

    protected override void OnCoerceOutputValue(
        string runtimeValue,
        ResultElement resultValue)
    {
        AssertCreditCardNumberFormat(runtimeValue);
        resultValue.SetStringValue(runtimeValue);
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
    {
        AssertCreditCardNumberFormat(runtimeValue);
        return new StringValueNode(runtimeValue);
    }

    private void AssertCreditCardNumberFormat(string value)
    {
        if (!_validator.ValidateCreditCard(value))
        {
            throw new LeafCoercionException(
                "The specified value is not a valid credit card number.",
                this);
        }
    }
}
```

## Specialized Base Classes

Hot Chocolate provides specialized base classes for common scalar patterns.

### Integer scalars

Use `IntegerTypeBase<T>` for numeric scalars with min/max constraints. The base class handles parsing, validation, and range checking automatically.

```csharp
// Types/TcpPortType.cs
public sealed class TcpPortType : IntegerTypeBase<int>
{
    public TcpPortType()
        : base("TcpPort", min: 1, max: 65535)
    {
        Description = "A valid TCP port number (1-65535)";
    }

    protected override int OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToInt32();

    protected override int OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt32();

    protected override void OnCoerceOutputValue(int runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    protected override IValueNode OnValueToLiteral(int runtimeValue)
        => new IntValueNode(runtimeValue);
}
```

`IntegerTypeBase` validates that values fall within the specified range and throws a `LeafCoercionException` if they do not. To customize the error message, override `FormatError`:

```csharp
protected override LeafCoercionException FormatError(int runtimeValue)
    => new LeafCoercionException(
        $"The value '{runtimeValue}' is not a valid TCP port. Must be between 1 and 65535.",
        this);
```

Hot Chocolate also provides `FloatTypeBase<T>` for floating-point scalars (`float`, `double`, `decimal`) that need min/max range validation.

### Regex-based scalars

Use `RegexType` for string scalars that must match a specific pattern. This works well for formats like phone numbers, postal codes, or identifiers.

```csharp
// Types/HexColorType.cs
public sealed class HexColorType : RegexType
{
    public HexColorType()
        : base(
            "HexColor",
            "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
            "A hex color code, e.g. #FF5733 or #F53")
    {
    }
}
```

You can also instantiate `RegexType` directly when registering scalars:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddType(new RegexType(
        "PostalCode",
        @"^\d{5}(-\d{4})?$",
        "US postal code in format 12345 or 12345-6789"));
```

To customize the error message for pattern validation failures, override `FormatException`:

```csharp
protected override LeafCoercionException FormatException(string runtimeValue)
    => new LeafCoercionException(
        $"'{runtimeValue}' is not a valid hex color. Expected format: #RGB or #RRGGBB.",
        this);
```

# Next Steps

- **Need to define object types?** See [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types).
- **Need to accept complex inputs?** See [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types).
- **Need to define enums?** See [Enums](/docs/hotchocolate/v16/building-a-schema/enums).
- **Need to add custom validation logic?** See [Directives](/docs/hotchocolate/v16/building-a-schema/directives).
