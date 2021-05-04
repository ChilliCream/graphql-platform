---
title: "Scalars"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

A GraphQL schema should be built as expressive as possible.
Just from looking at the schema, a developer should know how to use the API.
In GraphQL you are not limited to only describing the structure of a type, you can even describe value types.
Scalar types represent types that can hold data of a specific kind.
Scalars are leaf types, meaning you cannot use e.g. `{ fieldname }` to further drill down into the type.

A scalar must only know how to serialize and deserialize the value of the field.
GraphQL gives you the freedom to define custom scalar types.
This makes them the perfect tool for expressive value types.
You could create a scalar for `CreditCardNumber` or `NonEmptyString`.

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
| `Byte`      |                                                             |
| `ByteArray` | Base64 encoded array of bytes                               |
| `Short`     | Signed 16-bit numeric non-fractional value                  |
| `Long`      | Signed 64-bit numeric non-fractional value                  |
| `Decimal`   | .NET Floating Point Type                                    |
| `Url`       | Url                                                         |
| `DateTime`  | ISO-8601 date time                                          |
| `Date`      | ISO-8601 date                                               |
| `Uuid`      | GUID                                                        |
| `Any`       | This type can be anything, string, int, list or object etc. |

# Using Scalars

HotChocolate will automatically detect which scalars are in use and will only expose those in the introspection. This keeps the schema definition small, simple and clean.

The schema discovers .NET types and binds the matching scalar to the type.
HotChocolate, for example, automatically binds the `StringType` on a member of the type `System.String`.
You can override these mappings by explicitly specifying type bindings on the request executor builder.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .BindRuntimeType<string, StringType>()
        .AddQueryType<Query>();
}
```

Furthermore, you can also bind scalars to arrays or type structures:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .BindRuntimeType<byte[], ByteArrayType>()
        .AddQueryType<Query>();
}
```

# Uuid Type

The `Uuid` scalar supports the following serialization formats.

| Specifier   | Format                                                               |
| ----------- | -------------------------------------------------------------------- |
| N (default) | 00000000000000000000000000000000                                     |
| D           | 00000000-0000-0000-0000-000000000000                                 |
| B           | {00000000-0000-0000-0000-000000000000}                               |
| P           | (00000000-0000-0000-0000-000000000000)                               |
| X           | {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} |

The `UuidType` will always return the value in the specified format. In case it is used as an input type, it will first try to parse the result in the specified format. If the parsing does not succeed, it will try to parse the value in other formats.

To change the default format you have to register the `UuidType` with the specfier on the schema:

```csharp
services
   .AddGraphQLServer()
   ... // your configuration
   .AddType(new UuidType('D'));
```

# Any Type

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

If you want to access the data you can either fetch data as an object or you can ask the context to provide it as a specific object.

```csharp
Foo foo = context.Argument<Foo>("bar");
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

If you want to access an object dynamically without serializing it to a strongly typed model you can get it as `IReadOnlyDictionary<string, object>` or as `ObjectValueNode`.

Lists can be accessed generically by getting them as `IReadOnlyList<object>` or as `ListValueNode`.

# Custom Converter

HotChocolate converts .Net types to match the types supported by the scalar of the field.
By default, all standard .Net types have converters registered.
You can register converters and reuse the built-in scalar types.
In case you use a non-standard library, e.g. [Noda Time](https://nodatime.org/), you can register a converter and use the standard `DateTimeType`.

```csharp
public class Query
{
    public OffsetDateTime GetDateTime(OffsetDateTime offsetDateTime)
    {
        return offsetDateTime;
    }
}
```

_Startup_

```csharp
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
```

# Custom Scalars

All scalars in HotChocolate are defined though a `ScalarType`
The easiest way to create a custom scalar is to extend `ScalarType<TRuntimeType, TLiteral>`.
This base class already includes basic serialization and parsing logic.

```csharp
public sealed class CreditCardNumberType
    : ScalarType<string, StringValueNode>
{
    private readonly ICreditCardValidator _validator;

    /// Like all type system objects, Scalars have support for dependency injection
    public CreditCardNumberType(ICreditCardValidator validator)
        : base("CreditCardNumber")
    {
        _validator = validator;
        Description = "Represents a credit card number in the format of XXXX XXXX XXXX XXXX";
    }

    /// <summary>
    /// Checks if a incoming StringValueNode is valid. In this case the string value is only
    /// valid if it passes the credit card validation
    /// </summary>
    /// <param name="valueSyntax">The valueSyntax to validate</param>
    /// <returns>true if the value syntax holds a valid credit card number</returns>
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        return _validator.ValidateCreditCard(valueSyntax.Value);
    }

    /// <summary>
    /// Checks if a incoming string is valid. In this case the string value is only
    /// valid if it passes the credit card validation
    /// </summary>
    /// <param name="runtimeValue">The valueSyntax to validate</param>
    /// <returns>true if the value syntax holds a valid credit card number</returns>
    protected override bool IsInstanceOfType(string runtimeValue)
    {
        return _validator.ValidateCreditCard(runtimeValue);
    }

    /// <summary>
    /// Converts a StringValueNode to a string
    /// </summary>
    protected override string ParseLiteral(StringValueNode valueSyntax) =>
        valueSyntax.Value;

    /// <summary>
    /// Converts a string to a StringValueNode
    /// </summary>
    protected override StringValueNode ParseValue(string runtimeValue) =>
        new StringValueNode(runtimeValue);

    /// <summary>
    /// Parses a result value of this into a GraphQL value syntax representation.
    /// In this case this is just ParseValue
    /// </summary>
    public override IValueNode ParseResult(object? resultValue) =>
        ParseValue(resultValue);
}
```

By extending `ScalarType` you have full control over serialization and parsing.

```csharp
    public sealed class CreditCardNumberType
        : ScalarType
    {
        private readonly ICreditCardValidator _validator;

        /// Like all type system objects, Scalars have support for dependency injection
        public CreditCardNumberType(ICreditCardValidator validator)
            : base("CreditCardNumber")
        {
            _validator = validator;
            Description = "Represents a credit card number in the format of XXXX XXXX XXXX XXXX";
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
        public override object ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is StringValueNode stringLiteral &&
                _validator.ValidateCreditCard(stringLiteral.Value))
            {
                return stringLiteral.Value;
            }

            throw new SerializationException(
                "The specified value has to be a credit card number in the format " +
                "XXXX XXXX XXXX XXXX",
                nameof(valueSyntax));
        }

        // define how a native type is parsed into a literal,
        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is string s &&
                _validator.ValidateCreditCard(s))
            {
                return new StringValueNode(null, s, false);
            }

            throw new SerializationException(
                "The specified value has to be a credit card number in the format " +
                "XXXX XXXX XXXX XXXX");
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is string s &&
                _validator.ValidateCreditCard(s))
            {
                return new StringValueNode(null, s, false);
            }

            throw new SerializationException(
                "The specified value has to be a credit card number in the format " +
                "XXXX XXXX XXXX XXXX");
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is string s &&
                _validator.ValidateCreditCard(s))
            {
                resultValue = s;
                return true;
            }

            resultValue = null;
            return false;
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            if (serialized is string s &&
                _validator.ValidateCreditCard(s))
            {
                value = s;
                return true;
            }

            value = null;
            return false;
        }
    }
```

# Additional Scalars

HotChocolate provides additional scalars for more specific usecases.

To use these scalars you have to add the package `HotChocolate.Types.Scalars`

```csharp
dotnet add package HotChocolate.Types.Scalars
```

These scalars cannot be mapped by HotChocolate to a field.
You need to specify them manually.

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
    protected override void Configure(
        IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.UserName).Type<NonEmptyStringType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
type User {
  userName: NonEmptyString
}
```

</ExampleTabs.Schema>
</ExampleTabs>

You will also have to add the Scalar to the schema:

```csharp
services
    .AddGraphQLServer()
    // ....
    .AddType<NonEmptyStringType>()
```

**Available Scalars:**

| Type             | Description                                                                                                                                                                                                             |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EmailAddress     | The `EmailAddress` scalar type represents a email address, represented as UTF-8 character sequences that follows the specification defined in RFC 5322.                                                                 |
| HexColor         | The `HexColor` scalar type represents a valid HEX color code.                                                                                                                                                           |
| Hsl              | The `Hsl` scalar type represents a valid a CSS HSL color as defined [here](https://developer.mozilla.org/en-US/docs/Web/CSS/) color_value#hsl_colors.                                                                   |
| Hsla             | The `Hsla` scalar type represents a valid a CSS HSLA color as defined [here](https://developer.mozilla.org/en-US/docs/Web/CSS/) color_value#hsl_colors.                                                                 |
| IPv4             | The `IPv4` scalar type represents a valid a IPv4 address as defined [here](https://en.wikipedia.org/wiki/) IPv4.                                                                                                        |
| IPv6             | The `IPv6` scalar type represents a valid a IPv6 address as defined here [RFC8064](https://tools.ietf.org/html/rfc8064).                                                                                                |
| Isbn             | The `ISBN` scalar type is a ISBN-10 or ISBN-13 number: https:\/\/en.wikipedia.org\/wiki\/International_Standard_Book_Number.                                                                                            |
| LocalCurrency    | The `LocalCurrency` scalar type is a currency string.                                                                                                                                                                   |
| LocalDate        | The `LocalDate` scalar type represents a ISO date string, represented as UTF-8 character sequences yyyy-mm-dd. The scalar follows the specification defined in RFC3339.                                                 |
| LocalTime        | The `LocalTime` scalar type is a local time string (i.e., with no associated timezone) in 24-hr `HH:mm:ss]`.                                                                                                            |
| MacAddress       | The `MacAddess` scalar type represents a IEEE 802 48-bit Mac address, represented as UTF-8 character sequences. The scalar follows the specification defined in [RFC7042](https://tools.ietf.org/html/rfc7042#page-19). |
| NegativeFloat    | The `NegativeFloat` scalar type represents a double‐precision fractional value less than 0.                                                                                                                             |
| NegativeInt      | The `NegativeIntType` scalar type represents a signed 32-bit numeric non-fractional with a maximum of -1.                                                                                                               |
| NonEmptyString   | The `NonNullString` scalar type represents non empty textual data, represented as UTF‐8 character sequences with at least one character.                                                                                |
| NonNegativeFloat | The `NonNegativeFloat` scalar type represents a double‐precision fractional value greater than or equal to 0.                                                                                                           |
| NonNegativeInt   | The `NonNegativeIntType` scalar type represents a unsigned 32-bit numeric non-fractional value greater than or equal to 0.                                                                                              |
| NonPositiveFloat | The `NonPositiveFloat` scalar type represents a double‐precision fractional value less than or equal to 0.                                                                                                              |
| NonPositiveInt   | The `NonPositiveInt` scalar type represents a signed 32-bit numeric non-fractional value less than or equal to 0.                                                                                                       |
| PhoneNumber      | The `PhoneNumber` scalar type represents a value that conforms to the standard E.164 format as specified [here](https://en.wikipedia.org/wiki/E).164.                                                                   |
| PositiveInt      | The `PositiveInt` scalar type represents a signed 32‐bit numeric non‐fractional value of at least the value 1.                                                                                                          |
| PostalCode       | The `PostalCode` scalar type represents a valid postal code.                                                                                                                                                            |
| Port             | The `Port` scalar type represents a field whose value is a valid TCP port within the range of 0 to 65535.                                                                                                               |
| Rgb              | The `RGB` scalar type represents a valid CSS RGB color as defined [here](<https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()>).                                                              |
| Rgba             | The `RGBA` scalar type represents a valid CSS RGBA color as defined [here](<https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()>).                                                            |
| UnsignedInt      | The `UnsignedInt` scalar type represents a unsigned 32‐bit numeric non‐fractional value greater than or equal to 0.                                                                                                     |
| UnsignedLong     | The `UnsignedLong` scalar type represents a unsigned 64‐bit numeric non‐fractional value greater than or equal to 0.                                                                                                    |
| UtcOffset        | The `UtcOffset` scalar type represents a value of format `±hh:mm`.                                                                                                                                                      |
