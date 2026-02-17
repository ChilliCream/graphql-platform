---
title: "Scalars"
---

Strawberry Shake supports the following scalars out of the box:

| Type           | Description                                                 |
| -------------- | ----------------------------------------------------------- |
| `Base64String` | Base64 encoded array of bytes                               |
| `Boolean`      | Boolean type representing true or false                     |
| `Byte`         |                                                             |
| `ByteArray`    | Base64 encoded array of bytes (DEPRECATED)                  |
| `Date`         | ISO-8601 date                                               |
| `DateTime`     | ISO-8601 date time                                          |
| `Decimal`      | .NET Floating Point Type                                    |
| `Float`        | Double-precision fractional values as specified by IEEE 754 |
| `ID`           | Unique identifier                                           |
| `Int`          | Signed 32-bit numeric non-fractional value                  |
| `Long`         | Signed 64-bit numeric non-fractional value                  |
| `Short`        | Signed 16-bit numeric non-fractional value                  |
| `String`       | UTF-8 character sequences                                   |
| `Url`          | Url                                                         |
| `Uuid`         | GUID                                                        |

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
