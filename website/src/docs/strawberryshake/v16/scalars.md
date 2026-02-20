---
title: "Scalars"
---

Strawberry Shake supports the following scalars out of the box:

| Type          | Description                                                                                                     |
| ------------- | --------------------------------------------------------------------------------------------------------------- |
| Any           | The [Any][1] scalar type represents any valid GraphQL value.                                                    |
| Base64String  | The [Base64String][2] scalar type represents an array of bytes encoded as a Base64 string.                      |
| Boolean       | The [Boolean][3] scalar type represents `true` or `false`.                                                      |
| Byte          | The [Byte][4] scalar type represents a signed 8-bit integer.                                                    |
| ByteArray     | Base64-encoded array of bytes. (DEPRECATED, use `Base64String`)                                                 |
| Date          | The [Date][5] scalar type represents a date in UTC.                                                             |
| DateTime      | The [DateTime][6] scalar type represents a date and time with time zone offset information.                     |
| Decimal       | The [Decimal][7] scalar type represents a decimal floating-point number with high precision.                    |
| Float         | The [Float][8] scalar type represents signed double-precision fractional values as specified by [IEEE 754][9].  |
| ID            | The [ID][10] scalar type represents a unique identifier, often used to refetch an object or as key for a cache. |
| Int           | The [Int][11] scalar type represents a signed 32-bit numeric non-fractional value.                              |
| LocalDate     | The [LocalDate][12] scalar type represents a date without time or time zone information.                        |
| LocalDateTime | The [LocalDateTime][13] scalar type represents a date and time without time zone information.                   |
| LocalTime     | The [LocalTime][14] scalar type represents a time of day without date or time zone information.                 |
| Long          | The [Long][15] scalar type represents a signed 64-bit integer.                                                  |
| Short         | The [Short][16] scalar type represents a signed 16-bit integer.                                                 |
| String        | The [String][17] scalar type represents textual data, represented as a sequence of Unicode code points.         |
| TimeSpan      | The [TimeSpan][18] scalar type represents a duration of time.                                                   |
| UnsignedByte  | The [UnsignedByte][19] scalar type represents an unsigned 8-bit integer.                                        |
| UnsignedInt   | The [UnsignedInt][20] scalar type represents an unsigned 32-bit integer.                                        |
| UnsignedLong  | The [UnsignedLong][21] scalar type represents an unsigned 64-bit integer.                                       |
| UnsignedShort | The [UnsignedShort][22] scalar type represents an unsigned 16-bit integer.                                      |
| URI           | The [URI][23] scalar type represents a Uniform Resource Identifier (URI) as defined by RFC 3986.                |
| URL           | The [URL][24] scalar type represents a Uniform Resource Locator (URL) as defined by RFC 3986.                   |
| UUID          | The [UUID][25] scalar type represents a Universally Unique Identifier (UUID) as defined by RFC 9562.            |

[1]: https://scalars.graphql.org/chillicream/any.html
[2]: https://scalars.graphql.org/chillicream/base64-string.html
[3]: https://spec.graphql.org/September2025/#sec-Boolean
[4]: https://scalars.graphql.org/chillicream/byte.html
[5]: https://scalars.graphql.org/chillicream/date.html
[6]: https://scalars.graphql.org/chillicream/date-time.html
[7]: https://scalars.graphql.org/chillicream/decimal.html
[8]: https://spec.graphql.org/September2025/#sec-Float
[9]: https://en.wikipedia.org/wiki/IEEE_floating_point
[10]: https://spec.graphql.org/September2025/#sec-ID
[11]: https://spec.graphql.org/September2025/#sec-Int
[12]: https://scalars.graphql.org/chillicream/local-date.html
[13]: https://scalars.graphql.org/chillicream/local-date-time.html
[14]: https://scalars.graphql.org/chillicream/local-time.html
[15]: https://scalars.graphql.org/chillicream/long.html
[16]: https://scalars.graphql.org/chillicream/short.html
[17]: https://spec.graphql.org/September2025/#sec-String
[18]: https://scalars.graphql.org/chillicream/time-span.html
[19]: https://scalars.graphql.org/chillicream/unsigned-byte.html
[20]: https://scalars.graphql.org/chillicream/unsigned-int.html
[21]: https://scalars.graphql.org/chillicream/unsigned-long.html
[22]: https://scalars.graphql.org/chillicream/unsigned-short.html
[23]: https://scalars.graphql.org/chillicream/uri.html
[24]: https://scalars.graphql.org/chillicream/url.html
[25]: https://scalars.graphql.org/chillicream/uuid.html

# Custom Scalars

As an addition to the scalars listed above, you can define your own scalars for the client.
A scalar has two representations: the `runtimeType` and the `serializationType`.
The `runtimeType` refers to the type you use in your dotnet application.
The `serializationType` is the type that is used to transport the value.

Let us explore this with the example of `DateTime`. The server serializes a date into a string on the server.
It is transported as a string over the wire:

```json
{
  "user": {
    // the serializationType in this case is string
    "registrationDate": "02-04-2001T12:00:03Z"
  }
}
```

The `registrationDate` in our .NET client, should on the other hand be represented as a `System.DateTime`.

```csharp
public partial class GetUser_User : IEquatable<GetUser_User>, IGetUser_User
{
    // ....

    // The runtimeType is DateTime
    public DateTime? RegistrationDate { get; }

    // ....
}
```

By default, all custom scalars are treated like the `String` scalar.
This means, that the client expects a string value and will deserialize it to a `System.String`.

If you want to change the `serializationType` or/and the `runtimeType` of a scalar, you have to specify the desired types in the `schema.extensions.graphql`.
You can declare a scalar extension and add the `@serializationType` or/and the `@runtimeType` directive.

```graphql
"""
Defines the serialization type of a scalar.
"""
directive @serializationType(
  """
  The fully qualified .NET type name.
  """
  name: String!

  """
  Indicates whether the specified type is a value type (struct).
  """
  valueType: Boolean = false
) on SCALAR

"""
Defines the runtime type of a scalar.
"""
directive @runtimeType(
  """
  The fully qualified .NET type name.
  """
  name: String!

  """
  Indicates whether the specified type is a value type (struct).
  """
  valueType: Boolean = false
) on SCALAR

"""
Represents an integer value that is greater or equal to 0.
"""
extend scalar PositiveInt
    @serializationType(name: "global::System.Int32")
    @runtimeType(name: "global::System.Int32")
```

As soon as you specify custom serialization and runtime types you also need to provide a serializer for the type.

## Serializer

A scalar identifies its serializer by the scalar name, runtime- and serialization type.
You have to provide an `ISerializer` as soon as you change the `serializationType` or the `runtimeType`.
Use the base class `ScalarSerializer<TValue>` or `ScalarSerializer<TSerializer, TRuntime>` to create your custom serializer.

### Simple Example

If the serialization and the value type are identical, you can just use the `ScalarSerializer` base class.

_schema.extensions.graphql_

```graphql
extend scalar PositiveInt
  @serializationType(name: "global::System.Int32")
  @runtimeType(name: "global::System.Int32")
```

_serializer_

```csharp
public class PositiveIntSerializer : ScalarSerializer<int>
{
    public PositiveIntSerializer()
        : base(
            // the name of the scalar
            "PositiveInt")
    {
    }
}
```

_configuration_

```csharp
serviceCollection.AddSerializer<PositiveIntSerializer>();
```

> ⚠️ **Note:** When using a value type (struct) with `@serializationType` or `@runtimeType`, you must set `valueType: true` to ensure correct code generation.<br />
> This is not required for intrinsic primitive value types already supported as built-in scalars by Strawberry Shake (e.g., `int`, `float`, `bool`).<br />
> Example: `@serializationType(name: "global::System.Numerics.Vector2", valueType: true)`

### Any or JSON

Some GraphQL schemas contain untyped fields, whose types are often called `Any` or `JSON`. Strawberry Shake allows you to access these fields.

By default Strawberry Shake will use the built-in `JsonSerializer` to represent these fields as `JsonDocument`. If you want a different representation or use a different JSON library you can do so by providing a custom serializer that handles JSON scalars.

Json objects are internally handled as `JsonElement` provided by `System.Text.Json`. You can use this to handle serialization by yourself.

> Note: If you want the raw json from the `JsonElement` use `GetRawText`.
> In order to have a custom serializer you need to specify runtime and serialization type.

_schema.extensions.graphql_

```graphql
extend scalar Any
  @serializationType(name: "global::System.Object")
  @runtimeType(name: "global::System.Text.Json.JsonElement")
```

Also you need to provide a custom serializer to handle the parsing of the `JsonElement` to whatever type you desire.

_serializer_

```csharp
public class MyJsonSerializer : ScalarSerializer<JsonElement, object>
{
    public MyJsonSerializer(string typeName = BuiltInScalarNames.Any)
        : base(typeName)
    {
    }
    public override object Parse(JsonElement serializedValue)
    {
        // handle the serialization of the JsonElement
    }
    protected override JsonElement Format(object runtimeValue)
    {
        // handle the serialization of the runtime representation in case
        // the scalar is used as a variable.
    }
}
```

### Advanced Example

Your schema contains `X509Certificate`'s. These are serialized to `Base64` on the server and transported as strings.

_schema.extensions.graphql_

```graphql
extend scalar X509Certificate
  @serializationType(name: "global::System.String")
  @runtimeType(
    name: "global::System.Security.Cryptography.X509Certificates.X509Certificate2"
  )
```

_serializer_

```csharp
public class X509CertificateSerializer
    : ScalarSerializer<string, X509Certificate2>
{
    public X509CertificateSerializer()
       : base(
           // the name of the scalar
           "X509Certificate")
    {
    }

    // Parses the value that is returned from the server (Output)
    public override X509Certificate2 Parse(string serializedValue)
    {
        return new X509Certificate2(Convert.FromBase64String(serializedValue));
    }

    // Formats the value to send to the server (Input)
    protected override string Format(X509Certificate2 runtimeValue)
    {
        return Convert.ToBase64String(runtimeValue.Export(X509ContentType.Cert));
    }
}
```

_configuration_

```csharp
serviceCollection.AddSerializer<X509CertificateSerializer>();
```
