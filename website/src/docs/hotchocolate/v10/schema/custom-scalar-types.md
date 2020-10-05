---
title: Scalar Type Support
---

Scalar types in GraphQL represent the leaf types of the graph like `String` or `Int`.

Scalar values represent the concrete data that is exposed.

# Core Scalar Types

Hot Chocolate comes with the following core scalar types that are defined by the GraphQL specification:

| Type      | Description                                                 |
| --------- | ----------------------------------------------------------- |
| `Int`     | Signed 32-bit numeric non-fractional value                  |
| `Float`   | Double-precision fractional values as specified by IEEE 754 |
| `String`  | UTF-8 character sequences                                   |
| `Boolean` | Boolean type representing true or false                     |
| `ID`      | Unique identifier                                           |

# Extended Scalar Types

Apart from the core scalars we have also added support for an extended set of scalar types:

| Type       | Description                                                 |
| ---------- | ----------------------------------------------------------- |
| `Byte`     |                                                             |
| `Short`    | Signed 16-bit numeric non-fractional value                  |
| `Long`     | Signed 64-bit numeric non-fractional value                  |
| `Decimal`  | .NET Floating Point Type                                    |
| `Url`      | Url                                                         |
| `DateTime` | ISO-8601 date time                                          |
| `Date`     | ISO-8601 date                                               |
| `Uuid`     | GUID                                                        |
| `Any`      | This type can be anything, string, int, list or object etc. |

# Using Scalars

We will automatically detect which of our scalars are being used and only integrate the ones needed.

This keeps the schema definition small, simple and clean.

For our built-in types we also have added automatic .NET type inference. This means that we will automatically translate for instance a `System.String` to a GraphQL `StringType`. We can override these default mappings by explicitly specifying type bindings with the schema builder.

```csharp
SchemaBuilder.New()
    .BindClrType<string, MyCustomStringType>()
    ...
    .Create();
```

Furthermore, we can also bind scalars to arrays or type structures:

```csharp
SchemaBuilder.New()
    .BindClrType<byte[], ByteArrayType>()
    ...
    .Create();
```

Theses explicit bindings will overwrite the internal default bindings.

Specifying such a bindings explicitly can also be important when we have two types that bind to the same .NET Type like with `DateTimeType` and `DateType`.

```csharp
SchemaBuilder.New()
    .BindClrType<DateTime, DateTimeType>()
    ...
    .Create();
```

This will ensure that the type inference works predictable and will by default infer `DateTimeType` from `DateTime` for instance.

As I said before in most cases we do not need to do anything since Hot Chocolate has default bindings.

# Any Type

The `Any` scalar is a special type that can be compared to `object` in c#. Any allows us to specify any literal or return any output type.

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

If we want to access the data we can either fetch data as object or we can ask the context to provide it as a specific object.

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

If we want to access an object in a dynamic way without serializing it to a strongly typed model we can get it as `IReadOnlyDictionary<string, object>` or as `ObjectValueNode`.

Lists can be accessed in a generic way by getting them as `IReadOnlyList<object>` or as `ListValueNode`.

# Custom Scalars

In order to implement a new scalar type extend the type: `ScalarType`.

The following example shows you how a Custom String type could be implemented.

```csharp
public sealed class CustomStringType
    : ScalarType
{
    public CustomStringType()
        : base("CustomString")
    {
    }

    // define which .NET type represents your type
    public override Type ClrType { get; } = typeof(string);

    // define which literals this type can be parsed from.
    public override bool IsInstanceOfType(IValueNode literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        return literal is StringValueNode
            || literal is NullValueNode;
    }

    // define how a literal is parsed to the native .NET type.
    public override object ParseLiteral(IValueNode literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        if (literal is StringValueNode stringLiteral)
        {
            return stringLiteral.Value;
        }

        if (literal is NullValueNode)
        {
            return null;
        }

        throw new ArgumentException(
            "The string type can only parse string literals.",
            nameof(literal));
    }

    // define how a native type is parsed into a literal,
    public override IValueNode ParseValue(object value)
    {
        if (value == null)
        {
            return new NullValueNode(null);
        }

        if (value is string s)
        {
            return new StringValueNode(null, s, false);
        }

        if (value is char c)
        {
            return new StringValueNode(null, c.ToString(), false);
        }

        throw new ArgumentException(
            "The specified value has to be a string or char in order " +
            "to be parsed by the string type.");
    }

    // define the result serialization. A valid output must be of the following .NET types:
    // System.String, System.Char, System.Int16, System.Int32, System.Int64,
    // System.Float, System.Double, System.Decimal and System.Boolean
    public override object Serialize(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string s)
        {
            return s;
        }

        if(value is char c)
        {
            return c;
        }

        throw new ArgumentException(
            "The specified value cannot be serialized by the StringType.");
    }

    public override bool TryDeserialize(object serialized, out object value)
    {
        if (serialized is null)
        {
            value = null;
            return true;
        }

        if (serialized is string s)
        {
            value = s;
            return true;
        }

        value = null;
        return false;
    }
}
```
