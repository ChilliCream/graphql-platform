---
title: "Scalars"
---

Scalars are the leaf types of a GraphQL schema — they represent concrete values like strings, numbers, and dates. Unlike object types, scalars cannot be decomposed further; they are where the query ends and actual data is returned.

Every scalar defines how values are converted between their GraphQL wire format (JSON) and .NET runtime representation. GraphQL includes five built-in scalars (`String`, `Int`, `Float`, `Boolean`, and `ID`), but you can also define custom scalars like `DateTime`, `Uuid`, or `EmailAddress` to add domain-specific validation and improve the clarity of your API. Hot Chocolate already comes with lots of additional scalars.

# Built-in Scalars

The GraphQL specification defines five scalar types that every implementation must support.

## String

```sdl
type Product {
  description: String;
}
```

This scalar represents a UTF-8 character sequence.

It is automatically inferred from the usage of the .NET [string type](https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/reference-types#the-string-type).

## Boolean

```sdl
type Product {
  purchasable: Boolean;
}
```

This scalar represents a Boolean value, which can be either `true` or `false`.

It is automatically inferred from the usage of the .NET [bool type](https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool).

## Int

```sdl
type Product {
  quantity: Int;
}
```

This scalar represents a signed 32-bit numeric non-fractional value.

It is automatically inferred from the usage of the .NET [int type](https://docs.microsoft.com/dotnet/api/system.int32).

## Float

```sdl
type Product {
  price: Float;
}
```

This scalar represents double-precision fractional values, as specified by IEEE 754.

It is automatically inferred from the usage of the .NET [float](https://docs.microsoft.com/dotnet/api/system.single) or [double type](https://docs.microsoft.com/dotnet/api/system.double).

> Note: We introduced a separate `Decimal` scalar to handle `decimal` values.

## ID

```sdl
type Product {
  id: ID!;
}
```

This scalar is used to facilitate technology-specific Ids, like `int`, `string` or `Guid`.

It is **not** automatically inferred and the `IdType` needs to be [explicitly specified](/docs/hotchocolate/v16/defining-a-schema/object-types#explicit-types).

`ID` values are always represented as a [String](#string) in client-server communication, but can be coerced to their expected type on the server.

<ExampleTabs>
<Implementation>

```csharp
public class Product
{
    [GraphQLType<IdType>]
    public int Id { get; set; }
}

public class Query
{
    public Product GetProduct([GraphQLType<IdType>] int id)
    {
        // Omitted code for brevity
    }
}
```

</Implementation>
<Code>

```csharp
public class Product
{
    public int Id { get; set; }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("Product");

        descriptor.Field(f => f.Id).Type<IdType>();
    }
}

public class QueryType : ObjectType
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
public class Product
{
    public int Id { get; set; }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          product(id: ID): Product
        }

        type Product {
          id: ID
        }
    ")
    .BindRuntimeType<Product>()
    .AddResolver("Query", "product", context =>
    {
        var id = context.ArgumentValue<int>("id");

        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>

Notice how our code uses `int` for the `Id`, but in a request / response it would be serialized as a `string`. This allows us to switch the CLR type of our `Id`, without affecting the schema and our clients.

# GraphQL Community Scalars

The website <https://scalars.graphql.org/> hosts specifications for GraphQL scalars defined by the community. The community scalars use the `@specifiedBy` directive to point to the spec that is implemented.

```sdl
scalar UUID @specifiedBy(url: "https://tools.ietf.org/html/rfc4122")
```

## DateTime Type

This scalar represents an exact point in time. This point in time is specified by having an offset to UTC and does **not** use a time zone.

It is a slightly refined version of [RFC 3339](https://tools.ietf.org/html/rfc3339), including the [errata](https://www.rfc-editor.org/errata/rfc3339).

```sdl
scalar DateTime @specifiedBy(url: "https://scalars.graphql.org/andimarek/date-time.html")
```

> Note: The Hot Chocolate implementation diverges slightly from the DateTime scalar specification, and allows fractional seconds of 0-7 digits, as opposed to exactly 3.

<Video videoId="gO3bNKBmXZM" />

## LocalDate Type

This scalar represents a date without a time-zone in the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) calendar system.

The pattern is "YYYY-MM-DD" with "YYYY" representing the year, "MM" the month, and "DD" the day.

```sdl
scalar LocalDate @specifiedBy(url: "https://scalars.graphql.org/andimarek/local-date.html")
```

# .NET Scalars

In addition to the scalars defined by the specification, Hot Chocolate also supports the following set of scalar types:

| Type            | Description                                                                                               |
| --------------- | --------------------------------------------------------------------------------------------------------- |
| `Byte`          | Signed 8-bit numeric non‐fractional value greater than or equal to -128 and smaller than or equal to 127. |
| `ByteArray`     | Base64 encoded array of bytes                                                                             |
| `Date`          | ISO-8601 date                                                                                             |
| `Decimal`       | .NET Floating Point Type                                                                                  |
| `Json`          | This type can be anything, string, int, list or object, etc.                                              |
| `LocalDateTime` | Local date/time string (i.e., with no associated timezone) with the format `YYYY-MM-DDThh:mm:ss`          |
| `LocalTime`     | Local time string (i.e., with no associated timezone) in 24-hr `HH:mm:ss`                                 |
| `Long`          | Signed 64-bit numeric non-fractional value                                                                |
| `Short`         | Signed 16-bit numeric non-fractional value                                                                |
| `TimeSpan`      | ISO-8601 duration                                                                                         |
| `UnsignedByte`  | Unsigned 8-bit numeric non-fractional value greater than or equal to 0                                    |
| `UnsignedInt`   | Unsigned 32‐bit numeric non‐fractional value greater than or equal to 0                                   |
| `UnsignedLong`  | Unsigned 64‐bit numeric non‐fractional value greater than or equal to 0                                   |
| `UnsignedShort` | Unsigned 16‐bit numeric non‐fractional value greater than or equal to 0 and smaller or equal to 65535.    |
| `Url`           | Url                                                                                                       |
| `Uuid`          | GUID                                                                                                      |

## Uuid Type

The `Uuid` scalar supports the following serialization formats.

| Specifier   | Format                                                               |
| ----------- | -------------------------------------------------------------------- |
| N           | 00000000000000000000000000000000                                     |
| D (default) | 00000000-0000-0000-0000-000000000000                                 |
| B           | {00000000-0000-0000-0000-000000000000}                               |
| P           | (00000000-0000-0000-0000-000000000000)                               |
| X           | {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} |

The `UuidType` will always return the value in the specified format. In case it is used as an input type, it will first try to parse the result in the specified format. If the parsing does not succeed, it will try to parse the value in other formats.

To change the default format we have to register the `UuidType` with the specifier on the schema:

```csharp
builder.Services
   .AddGraphQLServer()
   .AddType(new UuidType('D'));
```

## Any Type

The `Any` scalar is a special type that can be compared to `object` in C#.
`Any` allows us to specify any literal or return any output type.

Consider the following type:

```sdl
type Query {
  foo(bar: Any): String
}
```

Since our field `foo` specifies an argument `bar` of type `Any` all of the following queries would be valid:

```graphql
{
  a: foo(bar: 1)
  b: foo(bar: [1, 2, 3, 4, 5])
  a: foo(bar: "abcdef")
  a: foo(bar: true)
  a: foo(bar: { a: "foo", b: { c: 1 } })
  a: foo(bar: [{ a: "foo", b: { c: 1 } }, { a: "foo", b: { c: 1 } }])
}
```

The same goes for the output side. `Any` can return a structure of data although it is a scalar type.

If we want to access the data we can either fetch data as an object or you can ask the context to provide it as a specific object.

```csharp
object foo = context.ArgumentValue<object>("bar");
Foo foo = context.ArgumentValue<Foo>("bar");
```

We can also ask the context which kind the current argument is:

```csharp
ValueKind kind = context.ArgumentKind("bar");
```

The value kind will tell us by which kind of literal the argument is represented.

> An integer literal can still contain a long value and a float literal could be a decimal but it also could just be a float.

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

If we want to access an object dynamically without serializing it to a strongly typed model we can get it as `IReadOnlyDictionary<string, object>` or as `ObjectValueNode`.

Lists can be accessed generically by getting them as `IReadOnlyList<object>` or as `ListValueNode`.

# Additional Scalars

We also offer a separate package with scalars for more specific use cases.

To use these scalars we have to add the `HotChocolate.Types.Scalars` package.

<PackageInstallation packageName="HotChocolate.Types.Scalars" />

**Available Scalars:**

| Type             | Description                                                                                                                                              |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EmailAddress     | Email address, represented as UTF-8 character sequences, as defined in [RFC5322](https://tools.ietf.org/html/rfc5322)                                    |
| HexColor         | HEX color code                                                                                                                                           |
| Hsl              | CSS HSL color as defined [here][2]                                                                                                                       |
| Hsla             | CSS HSLA color as defined [here][2]                                                                                                                      |
| IPv4             | IPv4 address as defined [here](https://en.wikipedia.org/wiki/IPv4)                                                                                       |
| IPv6             | IPv6 address as defined in [RFC8064](https://tools.ietf.org/html/rfc8064)                                                                                |
| Isbn             | ISBN-10 or ISBN-13 number as defined [here](https://en.wikipedia.org/wiki/International_Standard_Book_Number)                                            |
| Latitude         | Decimal degrees latitude number                                                                                                                          |
| Longitude        | Decimal degrees longitude number                                                                                                                         |
| MacAddress       | IEEE 802 48-bit (MAC-48/EUI-48) and 64-bit (EUI-64) Mac addresses, represented as UTF-8 character sequences, as defined in [RFC7042][3] and [RFC7043][4] |
| PhoneNumber      | A value that conforms to the standard E.164 format as defined [here](https://en.wikipedia.org/wiki/E.164)                                                |
| Rgb              | CSS RGB color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)                                                       |
| Rgba             | CSS RGBA color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)                                                      |
| UtcOffset        | A value of format `±hh:mm`                                                                                                                               |

[2]: https://developer.mozilla.org/docs/Web/CSS/color_value#hsl_colors
[3]: https://tools.ietf.org/html/rfc7042#page-19
[4]: https://tools.ietf.org/html/rfc7043

Most of these scalars are built on top of native .NET types. An Email Address for example is represented as a `string`, but just returning a `string` from our resolver would result in Hot Chocolate interpreting it as a `StringType`. We need to explicitly specify that the returned type (`string`) should be treated as an `EmailAddressType`.

```csharp
[GraphQLType(typeof(EmailAddressType))]
public string GetEmail() => "test@example.com";
```

[Learn more about explicitly specifying GraphQL types](/docs/hotchocolate/v16/defining-a-schema/object-types#explicit-types)

## NodaTime

We also offer a package specifically for [NodaTime](https://github.com/nodatime/nodatime).

It can be installed like the following.

<PackageInstallation packageName="HotChocolate.Types.NodaTime" />

**Available Scalars:**

| Type           | Description                                                                               | Example                                       |
| -------------- | ----------------------------------------------------------------------------------------- | --------------------------------------------- |
| DateTimeZone   | A [NodaTime DateTimeZone](https://nodatime.org/TimeZones)                                 | `"Europe/Rome"`                               |
| Duration       | A [NodaTime Duration](https://nodatime.org/3.0.x/userguide/duration-patterns)             | `"-123:07:53:10.019"`                         |
| Instant        | A [NodaTime Instant](https://nodatime.org/3.0.x/userguide/instant-patterns)               | `"2020-02-20T17:42:59Z"`                      |
| IsoDayOfWeek   | A [NodaTime IsoDayOfWeek](https://nodatime.org/3.0.x/api/NodaTime.IsoDayOfWeek.html)      | `7`                                           |
| LocalDate      | A [NodaTime LocalDate](https://nodatime.org/3.0.x/userguide/localdate-patterns)           | `"2020-12-25"`                                |
| LocalDateTime  | A [NodaTime LocalDateTime](https://nodatime.org/3.0.x/userguide/localdatetime-patterns)   | `"2020-12-25T13:46:78"`                       |
| LocalTime      | A [NodaTime LocalTime](https://nodatime.org/3.0.x/userguide/localtime-patterns)           | `"12:42:13.03101"`                            |
| OffsetDateTime | A [NodaTime OffsetDateTime](https://nodatime.org/3.0.x/userguide/offsetdatetime-patterns) | `"2020-12-25T13:46:78+02:35"`                 |
| OffsetDate     | A [NodaTime OffsetDate](https://nodatime.org/3.0.x/userguide/offsetdate-patterns)         | `"2020-12-25+02:35"`                          |
| OffsetTime     | A [NodaTime OffsetTime](https://nodatime.org/3.0.x/userguide/offsettime-patterns)         | `"13:46:78+02:35"`                            |
| Offset         | A [NodeTime Offset](https://nodatime.org/3.0.x/userguide/offset-patterns)                 | `"+02:35"`                                    |
| Period         | A [NodeTime Period](https://nodatime.org/3.0.x/userguide/period-patterns)                 | `"P-3W3DT139t"`                               |
| ZonedDateTime  | A [NodaTime ZonedDateTime](https://nodatime.org/3.0.x/userguide/zoneddatetime-patterns)   | `"2020-12-31T19:40:13 Asia/Kathmandu +05:45"` |

When returning a NodaTime type from one of our resolvers, for example a `NodaTime.Duration`, we also need to explicitly register the corresponding scalar type. In the case of a `NodaTime.Duration` this would be the `DurationType` scalar.

```csharp
public class Query
{
    public Duration GetDuration() => Duration.FromMinutes(3);
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<DurationType>();
```

This package was originally developed by [@shoooe](https://github.com/shoooe).

# Binding behavior

Hot Chocolate binds most of the native .NET types automatically.
A `System.String` is for example automatically mapped to a `StringType` in the schema.

We can override these mappings by explicitly specifying type bindings.

```csharp
builder.Services
    .AddGraphQLServer()
    .BindRuntimeType<string, StringType>();
```

Furthermore, we can also bind scalars to arrays or type structures:

```csharp
builder.Services
    .AddGraphQLServer()
    .BindRuntimeType<byte[], ByteArrayType>();
```

Hot Chocolate only exposes the used scalars in the generated schema, keeping it simple and clean.

# Custom Converters

We can reuse existing scalar types and bind them to different runtime types by specifying converters.

We could for example register converters between [NodaTime](https://nodatime.org/)'s `OffsetDateTime` and .NET's `DateTimeOffset` to reuse the existing `DateTimeType`.

```csharp
public class Query
{
    public OffsetDateTime GetDateTime(OffsetDateTime offsetDateTime)
    {
        return offsetDateTime;
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .BindRuntimeType<OffsetDateTime, DateTimeType>()
    .AddTypeConverter<OffsetDateTime, DateTimeOffset>(
        x => x.ToDateTimeOffset())
    .AddTypeConverter<DateTimeOffset, OffsetDateTime>(
        x => OffsetDateTime.FromDateTimeOffset(x));
```

# Scalar Options

Some scalars like `TimeSpan` or `Uuid` have options like their serialization format.

We can specify these options by registering the scalar explicitly.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddType(new UuidType('D'));
```

# Custom Scalars

A scalar type converts values between their GraphQL wire format and their .NET runtime representation. Each custom scalar must handle four conversion scenarios:

| Method                   | Direction              | Purpose                                                                    |
| ------------------------ | ---------------------- | -------------------------------------------------------------------------- |
| **OnCoerceInputLiteral** | GraphQL literal → .NET | Parses values embedded directly in a query, e.g. `{ field(arg: "value") }` |
| **OnCoerceInputValue**   | JSON → .NET            | Parses values provided as variables in the request                         |
| **OnCoerceOutputValue**  | .NET → JSON            | Writes resolver results to the response                                    |
| **OnValueToLiteral**     | .NET → GraphQL literal | Converts default values for schema introspection                           |

The easiest way to create a custom scalar is to extend `ScalarType<TRuntimeType, TLiteral>`, which provides the basic scaffolding.

```csharp
public sealed class CreditCardNumberType : ScalarType<string, StringValueNode>
{
    private readonly ICreditCardValidator _validator;

    // we can inject services that have been registered
    // with the DI container
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

    private void AssertCreditCardNumberFormat(string number)
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

Hot Chocolate provides specialized base classes for common scalar patterns that handle much of the boilerplate for you.

### Integer Scalars

Use `IntegerTypeBase<T>` for scalars that represent numeric values with min/max constraints. The base class handles parsing, validation, and range checking automatically.

```csharp
public class TcpPortType : IntegerTypeBase<int>
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

    public override void OnCoerceOutputValue(int runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    public override IValueNode OnValueToLiteral(int runtimeValue)
        => new IntValueNode(runtimeValue);
}
```

The `IntegerTypeBase` automatically validates that values fall within the specified range and throws a `LeafCoercionException` if they don't. To customize the error message, override the `FormatError` method:

```csharp
protected override LeafCoercionException FormatError(int runtimeValue)
    => new LeafCoercionException(
        $"The value {runtimeValue} is not a valid TCP port. Must be between 1 and 65535.",
        this);
```

Hot Chocolate also provides a `FloatTypeBase<T>` for floating-point scalars (`float`, `double`, `decimal`) that need min/max range validation.

### Regex-Based Scalars

Use `RegexType` for string scalars that must match a specific pattern. This is ideal for formats like phone numbers, postal codes, or identifiers.

```csharp
public class HexColorType : RegexType
{
    public HexColorType()
        : base(
            "HexColor",
            @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
            "A hex color code, e.g. #FF5733 or #F53")
    {
    }
}
```

You can also instantiate `RegexType` directly when registering scalars:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddType(new RegexType(
        "PostalCode",
        @"^\d{5}(-\d{4})?$",
        "US postal code in format 12345 or 12345-6789"));
```

Like `IntegerTypeBase` and `FloatTypeBase`, `RegexType` automatically validates values and throws a `LeafCoercionException` if they don't match the pattern. To customize the error message, override the `FormatException` method:

```csharp
protected override LeafCoercionException FormatException(string runtimeValue)
    => new LeafCoercionException(
        $"'{runtimeValue}' is not a valid hex color. Expected format: #RGB or #RRGGBB.",
        this);
```
