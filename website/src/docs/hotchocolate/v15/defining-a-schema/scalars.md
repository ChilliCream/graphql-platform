---
title: "Scalars"
---

Scalar types are the primitives of our schema and can hold a specific type of data. They are leaf types, meaning we cannot use e.g. `{ fieldName }` to further drill down into the type. The main purpose of a scalar is to define how a value is serialized and deserialized.

Besides basic scalars like `String` and `Int`, we can also create custom scalars like `CreditCardNumber` or `SocialSecurityNumber`. These custom scalars can greatly enhance the expressiveness of our schema and help new developers to get a grasp of our API.

# GraphQL scalars

The GraphQL specification defines the following scalars.

## String

```sdl
type Product {
  description: String;
}
```

This scalar represents an UTF-8 character sequence.

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

It is **not** automatically inferred and the `IdType` needs to be [explicitly specified](/docs/hotchocolate/v15/defining-a-schema/object-types#explicit-types).

`ID` values are always represented as a [String](#string) in client-server communication, but can be coerced to their expected type on the server.

<ExampleTabs>
<Implementation>

```csharp
public class Product
{
    [GraphQLType(typeof(IdType))]
    public int Id { get; set; }
}

public class Query
{
    public Product GetProduct([GraphQLType(typeof(IdType))] int id)
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

The website <https://www.graphql-scalars.com/> hosts specifications for GraphQL scalars defined by the community. The community scalars use the `@specifiedBy` directive to point to the spec that is implemented.

```sdl
scalar UUID @specifiedBy(url: "https://tools.ietf.org/html/rfc4122")
```

## DateTime Type

A custom GraphQL scalar which represents an exact point in time. This point in time is specified by having an offset to UTC and does not use time zone.

The DateTime scalar is based on [RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339).

```sdl
scalar DateTime @specifiedBy(url: "https://www.graphql-scalars.com/date-time/")
```

> Note: The Hot Chocolate implementation diverges slightly from the DateTime Scalar specification, and allows fractional seconds of 0-7 digits, as opposed to exactly 3.

<Video videoId="gO3bNKBmXZM" />

# .NET Scalars

In addition to the scalars defined by the specification, Hot Chocolate also supports the following set of scalar types:

| Type        | Description                                                  |
| ----------- | ------------------------------------------------------------ |
| `Byte`      | Byte                                                         |
| `ByteArray` | Base64 encoded array of bytes                                |
| `Short`     | Signed 16-bit numeric non-fractional value                   |
| `Long`      | Signed 64-bit numeric non-fractional value                   |
| `Decimal`   | .NET Floating Point Type                                     |
| `Url`       | Url                                                          |
| `Date`      | ISO-8601 date                                                |
| `TimeSpan`  | ISO-8601 duration                                            |
| `Uuid`      | GUID                                                         |
| `Any`       | This type can be anything, string, int, list or object, etc. |

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
| Hsl              | CSS HSL color as defined [here][1]                                                                                                                       |
| Hsla             | CSS HSLA color as defined [here][1]                                                                                                                      |
| IPv4             | IPv4 address as defined [here](https://en.wikipedia.org/wiki/IPv4)                                                                                       |
| IPv6             | IPv6 address as defined in [RFC8064](https://tools.ietf.org/html/rfc8064)                                                                                |
| Isbn             | ISBN-10 or ISBN-13 number as defined [here](https://en.wikipedia.org/wiki/International_Standard_Book_Number)                                            |
| Latitude         | Decimal degrees latitude number                                                                                                                          |
| Longitude        | Decimal degrees longitude number                                                                                                                         |
| LocalCurrency    | Currency string                                                                                                                                          |
| LocalDate        | ISO date string, represented as UTF-8 character sequences yyyy-mm-dd, as defined in [RFC3339][2]                                                         |
| LocalTime        | Local time string (i.e., with no associated timezone) in 24-hr `HH:mm:ss`                                                                                |
| MacAddress       | IEEE 802 48-bit (MAC-48/EUI-48) and 64-bit (EUI-64) Mac addresses, represented as UTF-8 character sequences, as defined in [RFC7042][3] and [RFC7043][4] |
| NegativeFloat    | Double‐precision fractional value less than 0                                                                                                            |
| NegativeInt      | Signed 32-bit numeric non-fractional with a maximum of -1                                                                                                |
| NonEmptyString   | Non empty textual data, represented as UTF‐8 character sequences with at least one character                                                             |
| NonNegativeFloat | Double‐precision fractional value greater than or equal to 0                                                                                             |
| NonNegativeInt   | Unsigned 32-bit numeric non-fractional value greater than or equal to 0                                                                                  |
| NonPositiveFloat | Double‐precision fractional value less than or equal to 0                                                                                                |
| NonPositiveInt   | Signed 32-bit numeric non-fractional value less than or equal to 0                                                                                       |
| PhoneNumber      | A value that conforms to the standard E.164 format as defined [here](https://en.wikipedia.org/wiki/E.164)                                                |
| PositiveInt      | Signed 32‐bit numeric non‐fractional value of at least the value 1                                                                                       |
| PostalCode       | Postal code                                                                                                                                              |
| Port             | TCP port within the range of 0 to 65535                                                                                                                  |
| Rgb              | CSS RGB color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)                                                       |
| Rgba             | CSS RGBA color as defined [here](https://developer.mozilla.org/docs/Web/CSS/color_value#rgb_colors)                                                      |
| SignedByte       | Signed 8-bit numeric non‐fractional value greater than or equal to -127 and smaller than or equal to 128.                                                |
| UnsignedInt      | Unsigned 32‐bit numeric non‐fractional value greater than or equal to 0                                                                                  |
| UnsignedLong     | Unsigned 64‐bit numeric non‐fractional value greater than or equal to 0                                                                                  |
| UnsignedShort    | Unsigned 16‐bit numeric non‐fractional value greater than or equal to 0 and smaller or equal to 65535.                                                   |
| UtcOffset        | A value of format `±hh:mm`                                                                                                                               |

[1]: https://developer.mozilla.org/docs/Web/CSS/color_value#hsl_colors
[2]: https://tools.ietf.org/html/rfc3339
[3]: https://tools.ietf.org/html/rfc7042#page-19
[4]: https://tools.ietf.org/html/rfc7043

Most of these scalars are built on top of native .NET types. An Email Address for example is represented as a `string`, but just returning a `string` from our resolver would result in Hot Chocolate interpreting it as a `StringType`. We need to explicitly specify that the returned type (`string`) should be treated as an `EmailAddressType`.

```csharp
[GraphQLType(typeof(EmailAddressType))]
public string GetEmail() => "test@example.com";
```

[Learn more about explicitly specifying GraphQL types](/docs/hotchocolate/v15/defining-a-schema/object-types#explicit-types)

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

All scalars in Hot Chocolate are defined through a `ScalarType`.
The easiest way to create a custom scalar is to extend `ScalarType<TRuntimeType, TLiteral>`.
This base class already includes basic serialization and parsing logic.

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

    // is another StringValueNode an instance of this scalar
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        => IsInstanceOfType(valueSyntax.Value);

    // is another string .NET type an instance of this scalar
    protected override bool IsInstanceOfType(string runtimeValue)
        => _validator.ValidateCreditCard(runtimeValue);

    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);

    // define how a value node is parsed to the string .NET type
    protected override string ParseLiteral(StringValueNode valueSyntax)
        => valueSyntax.Value;

    // define how the string .NET type is parsed to a value node
    protected override StringValueNode ParseValue(string runtimeValue)
        => new StringValueNode(runtimeValue);

    public override bool TryDeserialize(object? resultValue,
        out object? runtimeValue)
    {
        runtimeValue = null;

        if (resultValue is string s && _validator.ValidateCreditCard(s))
        {
            runtimeValue = s;
            return true;
        }

        return false;
    }

    public override bool TrySerialize(object? runtimeValue,
        out object? resultValue)
    {
        resultValue = null;

        if (runtimeValue is string s && _validator.ValidateCreditCard(s))
        {
            resultValue = s;
            return true;
        }

        return false;
    }
}
```

By extending `ScalarType` we have full control over serialization and parsing.

```csharp
public class CreditCardNumberType : ScalarType
{
    private readonly ICreditCardValidator _validator;

    public CreditCardNumberType(ICreditCardValidator validator)
        : base("CreditCardNumber")
    {
        _validator = validator;

        Description = "Represents a credit card number";
    }

    // define which .NET type represents your type
    public override Type RuntimeType { get; } = typeof(string);

    // define which value nodes this type can be parsed from
    public override bool IsInstanceOfType(IValueNode valueSyntax)
    {
        if (valueSyntax == null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        return valueSyntax is StringValueNode stringValueNode &&
            _validator.ValidateCreditCard(stringValueNode.Value);
    }

    // define how a value node is parsed to the native .NET type
    public override object ParseLiteral(IValueNode valueSyntax,
        bool withDefaults = true)
    {
        if (valueSyntax is StringValueNode stringLiteral &&
            _validator.ValidateCreditCard(stringLiteral.Value))
        {
            return stringLiteral.Value;
        }

        throw new SerializationException(
            "The specified value has to be a credit card number in the format "
                + "XXXX XXXX XXXX XXXX",
            this);
    }

    // define how the .NET type is parsed to a value node
    public override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is string s &&
            _validator.ValidateCreditCard(s))
        {
            return new StringValueNode(null, s, false);
        }

        throw new SerializationException(
            "The specified value has to be a credit card number in the format "
                + "XXXX XXXX XXXX XXXX",
            this);
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is string s &&
            _validator.ValidateCreditCard(s))
        {
            return new StringValueNode(null, s, false);
        }

        throw new SerializationException(
            "The specified value has to be a credit card number in the format "
                + "XXXX XXXX XXXX XXXX",
            this);
    }

    public override bool TrySerialize(object? runtimeValue,
        out object? resultValue)
    {
        resultValue = null;

        if (runtimeValue is string s &&
            _validator.ValidateCreditCard(s))
        {
            resultValue = s;
            return true;
        }

        return false;
    }

    public override bool TryDeserialize(object? resultValue,
        out object? runtimeValue)
    {
        runtimeValue = null;

        if (resultValue is string s &&
            _validator.ValidateCreditCard(s))
        {
            runtimeValue = s;
            return true;
        }

        return false;
    }
}
```

The implementation of [Hot Chocolate's own scalars](https://github.com/ChilliCream/graphql-platform/tree/main/src/HotChocolate/Core/src/Types.Scalars) can be used as a reference for writing custom scalars.
