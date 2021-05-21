---
title: "Scalars"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

A GraphQL schema should be built as expressive as possible.
Just from looking at the schema, a developer should know how to use the API.
In GraphQL we are not limited to only describing the structure of a type, we can even describe value types.
Scalar types represent types that can hold data of a specific kind.
Scalars are leaf types, meaning we cannot use e.g. `{ fieldname }` to further drill down into the type.

A scalar must only know how to serialize and deserialize the value of the field.
GraphQL gives us the freedom to define custom scalar types.
This makes them the perfect tool for expressive value types.
We could, for example, create a scalar for `CreditCardNumber` or `NonEmptyString`.

The GraphQL specification defines the following scalars

| Type      | Description                                                 |
| --------- | ----------------------------------------------------------- |
| `Int`     | Signed 32-bit numeric non-fractional value                  |
| `Float`   | Double-precision fractional values as specified by IEEE 754 |
| `String`  | UTF-8 character sequences                                   |
| `Boolean` | Boolean type representing true or false                     |
| `ID`      | Unique identifier                                           |

In addition to the scalars defined by the specification, HotChocolate also supports the following set of scalar types:

| Type        | Description                                                 |
| ----------- | ----------------------------------------------------------- |
| `Byte`      | TODO                                                        |
| `ByteArray` | Base64 encoded array of bytes                               |
| `Short`     | Signed 16-bit numeric non-fractional value                  |
| `Long`      | Signed 64-bit numeric non-fractional value                  |
| `Decimal`   | .NET Floating Point Type                                    |
| `Url`       | Url                                                         |
| `DateTime`  | ISO-8601 date time                                          |
| `Date`      | ISO-8601 date                                               |
| `Uuid`      | GUID                                                        |
| `Any`       | This type can be anything, string, int, list or object etc. |

# Usage

HotChocolate will automatically detect which scalars are in use and will only expose those in the introspection. This keeps the schema definition small, simple and clean.

The schema discovers .NET types and binds the matching scalar to the type.
HotChocolate, for example, automatically binds the `StringType` on a member of the type `System.String`.
We can override these mappings by explicitly specifying type bindings on the request executor builder.

```csharp
services
    .AddGraphQLServer()
    .BindRuntimeType<string, StringType>();
```

Furthermore, we can also bind scalars to arrays or type structures:

```csharp
services
    .AddGraphQLServer()
    .BindRuntimeType<byte[], ByteArrayType>();
```

## Uuid Type

The `Uuid` scalar supports the following serialization formats.

| Specifier   | Format                                                               |
| ----------- | -------------------------------------------------------------------- |
| N (default) | 00000000000000000000000000000000                                     |
| D           | 00000000-0000-0000-0000-000000000000                                 |
| B           | {00000000-0000-0000-0000-000000000000}                               |
| P           | (00000000-0000-0000-0000-000000000000)                               |
| X           | {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} |

The `UuidType` will always return the value in the specified format. In case it is used as an input type, it will first try to parse the result in the specified format. If the parsing does not succeed, it will try to parse the value in other formats.

To change the default format we have to register the `UuidType` with the specfier on the schema:

```csharp
services
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

HotChocolate provides additional scalars for more specific usecases.

To use these scalars we have to add the `HotChocolate.Types.Scalars` package.

```bash
dotnet add package HotChocolate.Types.Scalars
```

These additional scalars are not mapped by HotChocolate automatically, we need to specify them manually.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class User
{
    [GraphQLType(typeof(NonEmptyStringType))]
    public string UserName { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field(f => f.UserName)
            .Type<NonEmptyStringType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type User {
                  userName: NonEmptyString
                }
            ")
            .BindComplexType<User>()
            .AddType<NonEmptyStringType>();
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>

**Available Scalars:**

| Type             | Description                                                                                                                                               |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EmailAddress     | An email address, represented as UTF-8 character sequences that follows the specification defined in RFC 5322.                                            |
| HexColor         | A valid HEX color code.                                                                                                                                   |
| Hsl              | A valid a CSS HSL color as defined [here][1].                                                                                                             |
| Hsla             | A valid a CSS HSLA color as defined [here][1].                                                                                                            |
| IPv4             | A valid IPv4 address as defined [here](https://en.wikipedia.org/wiki/IPv4).                                                                               |
| IPv6             | A valid IPv6 address as defined in [RFC8064](https://tools.ietf.org/html/rfc8064).                                                                        |
| Isbn             | An ISBN-10 or ISBN-13 number as defined [here](https://en.wikipedia.org/wiki/International_Standard_Book_Number).                                         |
| Latitude         | A valid decimal degrees latitude number.                                                                                                                  |
| Longitude        | A valid decimal degrees longitude number.                                                                                                                 |
| LocalCurrency    | A currency string.                                                                                                                                        |
| LocalDate        | An ISO date string, represented as UTF-8 character sequences yyyy-mm-dd, as defined in [RFC3339][2].                                                      |
| LocalTime        | A local time string (i.e., with no associated timezone) in 24-hr `HH:mm:ss]`.                                                                             |
| MacAddress       | IEEE 802 48-bit (MAC-48/EUI-48) and 64-bit (EUI-64) Mac addresses, represented as UTF-8 character sequences, as defined in [RFC7042][3] and [RFC7043][4]. |
| NegativeFloat    | A double‐precision fractional value less than 0.                                                                                                          |
| NegativeInt      | A signed 32-bit numeric non-fractional with a maximum of -1.                                                                                              |
| NonEmptyString   | Non empty textual data, represented as UTF‐8 character sequences with at least one character.                                                             |
| NonNegativeFloat | A double‐precision fractional value greater than or equal to 0.                                                                                           |
| NonNegativeInt   | An unsigned 32-bit numeric non-fractional value greater than or equal to 0.                                                                               |
| NonPositiveFloat | A double‐precision fractional value less than or equal to 0.                                                                                              |
| NonPositiveInt   | A signed 32-bit numeric non-fractional value less than or equal to 0.                                                                                     |
| PhoneNumber      | A value that conforms to the standard E.164 format as defined [here](https://en.wikipedia.org/wiki/E.164).                                                |
| PositiveInt      | A signed 32‐bit numeric non‐fractional value of at least the value 1.                                                                                     |
| PostalCode       | A valid postal code.                                                                                                                                      |
| Port             | A valid TCP port within the range of 0 to 65535.                                                                                                          |
| Rgb              | A valid CSS RGB color as defined [here](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb_colors).                                         |
| Rgba             | A valid CSS RGBA color as defined [here](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb_colors).                                        |
| UnsignedInt      | An unsigned 32‐bit numeric non‐fractional value greater than or equal to 0.                                                                               |
| UnsignedLong     | An unsigned 64‐bit numeric non‐fractional value greater than or equal to 0.                                                                               |
| UtcOffset        | A value of format `±hh:mm`.                                                                                                                               |

[1]: https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#hsl_colors
[2]: https://tools.ietf.org/html/rfc3339
[3]: https://tools.ietf.org/html/rfc7042#page-19
[4]: https://tools.ietf.org/html/rfc7043

# Custom Converters

HotChocolate converts .Net types to match the types supported by the scalar of the field.
By default, all standard .Net types have converters registered.
We can register converters and reuse the built-in scalar types.
In case we use a non-standard library, e.g. [NodaTime](https://nodatime.org/), we can register a converter and use the standard `DateTimeType`.

```csharp
public class Query
{
    public OffsetDateTime GetDateTime(OffsetDateTime offsetDateTime)
    {
        return offsetDateTime;
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .BindRuntimeType<OffsetDateTime, DateTimeType>()
            .AddTypeConverter<OffsetDateTime, DateTimeOffset>(
                x => x.ToDateTimeOffset())
            .AddTypeConverter<DateTimeOffset, OffsetDateTime>(
                x => OffsetDateTime.FromDateTimeOffset(x));
    }
}
```

# Custom Scalars

All scalars in HotChocolate are defined through a `ScalarType`.
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

    // is another StringValueNode compatible with CreditCardNumberType
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        => IsInstanceOfType(valueSyntax.Value);

    // todo: document
    protected override bool IsInstanceOfType(string runtimeValue)
        => _validator.ValidateCreditCard(runtimeValue);

    // todo: document
    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);

    // todo: document
    protected override string ParseLiteral(StringValueNode valueSyntax)
        => valueSyntax.Value;

    // todo: document
    protected override StringValueNode ParseValue(string runtimeValue)
        => new StringValueNode(runtimeValue);

    // todo: document
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

    // todo: document
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

    // define which literals this type can be parsed from.
    public override bool IsInstanceOfType(IValueNode valueSyntax)
    {
        if (valueSyntax == null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        return valueSyntax is StringValueNode stringValueNode &&
            _validator.ValidateCreditCard(stringValueNode.Value);
    }

    // define how a literal is parsed to the native .NET type.
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

    // define how a .NET type is parsed to a literal,
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

    // todo: when is this useful?
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

    // todo: document
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

    // todo: document
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
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

Checkout how we have implemented [Hot Chocolate's scalars](https://github.com/ChilliCream/hotchocolate/tree/main/src/HotChocolate/Core/src/Types.Scalars).
