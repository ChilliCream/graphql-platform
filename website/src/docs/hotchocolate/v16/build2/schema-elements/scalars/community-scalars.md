---
title: "Community Scalars"
---

GraphQL defines a small set of built-in scalar contracts. Hot Chocolate adds more built-in scalars and optional scalar packages for common formats such as email addresses, IP addresses, colors, coordinates, ISBN values, phone numbers, MAC addresses, and UTC offsets.

Use this page when you need to install `HotChocolate.Types.Scalars`, annotate a field or argument with one of its scalar types, or configure common built-in scalars that often need an extra registration step. For the complete built-in mapping table, see [Built-in Scalars](./built-in-scalars). For NodaTime runtime types, see [NodaTime Scalars](./nodatime-scalars). For authoring a scalar, see [Custom Scalars](./custom-scalars).

# Choose the scalar by the contract you want

Start with the public contract you want clients to see, not only the CLR type you use in .NET.

| Contract                                                                                                               | Recommended action                                                                                       | Notes                                                                                                                    |
| ---------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Built-in CLR mapping such as `Guid`, `DateTimeOffset`, `JsonElement`, numeric variants, `Uri`, or `byte[]`             | Use [Built-in Scalars](./built-in-scalars). Configure here only when the default behavior is not enough. | Hot Chocolate infers these from CLR types.                                                                               |
| Validated format such as email, IP address, color, phone number, ISBN, latitude, longitude, MAC address, or UTC offset | Install `HotChocolate.Types.Scalars` and annotate fields, arguments, or input fields.                    | Most package scalars share common CLR types such as `string` or `double`, so explicit schema typing is usually required. |
| NodaTime runtime model                                                                                                 | Use [NodaTime Scalars](./nodatime-scalars).                                                              | Register `HotChocolate.Types.NodaTime` when your domain model uses NodaTime types.                                       |
| JSON-like metadata or interoperability payload                                                                         | Use `Any` with `JsonElement`. Add a converter only for CLR object or dictionary output.                  | `Any` reduces schema discoverability and validation. Prefer object types when fields are known.                          |
| Domain-specific format or coercion rules                                                                               | Use [Custom Scalars](./custom-scalars).                                                                  | Create a custom scalar only for a reusable leaf value contract.                                                          |

# Add the additional scalars package

Install the package that matches the Hot Chocolate version used by the rest of your application.

<PackageInstallation packageName="HotChocolate.Types.Scalars" />

Then reference the scalar type from `HotChocolate.Types` where you configure your schema:

```csharp
using HotChocolate.Types;
```

You do not need to register every package scalar globally. The common path is to use the scalar type where the contract matters.

# Use a package scalar on a field or argument

A `string` property infers as `String`. A `double` property infers as `Float`. If the GraphQL contract is more specific, annotate that field or configure it with a descriptor.

```csharp
using HotChocolate.Types;

public sealed class Contact
{
    [GraphQLType<EmailAddressType>]
    public string Email { get; init; } = default!;
}

[QueryType]
public static partial class ContactQueries
{
    public static Contact? ContactByEmail(
        [GraphQLType<EmailAddressType>] string email)
    {
        // Look up the contact by email address.
        return null;
    }
}
```

Expected SDL:

```graphql
type Contact {
  email: EmailAddress!
}

type Query {
  contactByEmail(email: EmailAddress!): Contact
}
```

You can use descriptor configuration when you keep schema metadata outside the model:

```csharp
using HotChocolate.Types;

public sealed class ContactType : ObjectType<Contact>
{
    protected override void Configure(IObjectTypeDescriptor<Contact> descriptor)
    {
        descriptor
            .Field(t => t.Email)
            .Type<NonNullType<EmailAddressType>>();
    }
}

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("contactByEmail")
            .Argument("email", a => a.Type<NonNullType<EmailAddressType>>())
            .Resolve(context => FindContact(context.ArgumentValue<string>("email")));
    }

    private static Contact? FindContact(string email)
    {
        // Look up the contact by email address.
        return null;
    }
}
```

Prefer local annotations or field configuration over schema-wide runtime binding such as `BindRuntimeType<string, EmailAddressType>()`. A global string binding is only appropriate when every `string` in that schema context has the same GraphQL scalar contract.

# Use the packaged scalar reference

The additional scalars package provides these v16 scalar types:

| Scalar         | Resolver runtime | GraphQL value example                               | Use when                                                                                                                |
| -------------- | ---------------- | --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `EmailAddress` | `string`         | `admin@example.com`                                 | The value should match the HTML email address format used by browser email inputs.                                      |
| `HexColor`     | `string`         | `#1f6feb`, `#fff`, `#1f6feb80`                      | The value is a CSS hex color.                                                                                           |
| `Hsl`          | `string`         | `hsl(210 50% 40%)`                                  | The value is a CSS HSL color.                                                                                           |
| `Hsla`         | `string`         | `hsla(210 50% 40% / 0.5)`                           | The value is a CSS HSLA color with alpha.                                                                               |
| `IPv4`         | `string`         | `192.168.0.1`, `192.168.0.0/24`                     | The value is an IPv4 address or CIDR address.                                                                           |
| `IPv6`         | `string`         | `2001:db8::1`, `2001:db8::/32`                      | The value is an IPv6 address or CIDR address.                                                                           |
| `Isbn`         | `string`         | `978-1-56619-909-4`, `0-306-40615-2`                | The value is an ISBN-10 or ISBN-13.                                                                                     |
| `Latitude`     | `double`         | `47° 36' 35.28" N`                                  | The GraphQL value is a string in degrees, minutes, and optional seconds. The resolver runtime value is decimal degrees. |
| `Longitude`    | `double`         | `122° 19' 59.88" W`                                 | The GraphQL value is a string in degrees, minutes, and optional seconds. The resolver runtime value is decimal degrees. |
| `MacAddress`   | `string`         | `00:1A:2B:3C:4D:5E`                                 | The value is a MAC-48, EUI-48, or EUI-64 address.                                                                       |
| `PhoneNumber`  | `string`         | `+12025550142`                                      | The value is an E.164 phone number.                                                                                     |
| `Rgb`          | `string`         | `rgb(31 111 235)`, `rgb(31, 111, 235)`              | The value is a CSS RGB color.                                                                                           |
| `Rgba`         | `string`         | `rgba(31 111 235 / 0.5)`, `rgba(31, 111, 235, 0.5)` | The value is a CSS RGBA color with alpha.                                                                               |
| `UtcOffset`    | `TimeSpan`       | `+00:00`, `-05:00`, `+05:30`                        | The value is a UTC offset from the supported offset list.                                                               |

`Latitude` and `Longitude` are string-valued GraphQL scalars with `double` resolver values. They parse coordinate strings into decimal degrees and serialize decimal degrees back to coordinate strings.

These scalars validate the transport value. They do not replace business validation or authorization. For example, `EmailAddress` can reject malformed email syntax, but it cannot prove that the mailbox exists or belongs to the current user.

# Configure UUID when clients need a GUID contract

`Guid` maps to `UUID` automatically. Use `UUID` when the public value is a UUID that clients may parse, validate, or generate. Use `ID` when the value is an opaque GraphQL identity.

```csharp
public sealed class ImportJob
{
    public Guid CorrelationId { get; init; }
}
```

Expected SDL:

```graphql
scalar UUID
  @specifiedBy(url: "https://scalars.graphql.org/chillicream/uuid.html")

type ImportJob {
  correlationId: UUID!
}
```

By default, UUID values serialize in the hyphenated `D` format:

```text
00000000-0000-0000-0000-000000000000
```

Register `UuidType` when clients require another output format:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddType(new UuidType(defaultFormat: 'N'));
```

Use `enforceFormat: true` when input must match the configured format:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddType(new UuidType(defaultFormat: 'D', enforceFormat: true));
```

| Format | Shape                     | Example                                  |
| ------ | ------------------------- | ---------------------------------------- |
| `N`    | 32 digits                 | `00000000000000000000000000000000`       |
| `D`    | Hyphenated, default       | `00000000-0000-0000-0000-000000000000`   |
| `B`    | Hyphenated in braces      | `{00000000-0000-0000-0000-000000000000}` |
| `P`    | Hyphenated in parentheses | `(00000000-0000-0000-0000-000000000000)` |

If you are upgrading old schema snapshots, note that older Hot Chocolate versions used the scalar name `Uuid`. In v16, the scalar name is `UUID`.

# Configure DateTime when precision or input format matters

`DateTime` and `DateTimeOffset` map to the built-in `DateTime` scalar. Use this section only when the default precision or strict input validation needs to change. For the complete temporal mapping table, see [Built-in Scalars](./built-in-scalars).

```csharp
public sealed class AuditEntry
{
    public DateTimeOffset CreatedAt { get; init; }
}
```

Expected SDL:

```graphql
scalar DateTime
  @specifiedBy(url: "https://scalars.graphql.org/chillicream/date-time.html")

type AuditEntry {
  createdAt: DateTime!
}
```

Configure precision with `DateTimeOptions`:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddType(new DateTimeType(new DateTimeOptions
    {
        OutputPrecision = 3,
        InputPrecision = 9,
        ValidateInputFormat = true
    }));
```

| Option                | What it controls                                           |
| --------------------- | ---------------------------------------------------------- |
| `InputPrecision`      | Maximum fractional second precision accepted from clients. |
| `OutputPrecision`     | Fractional second precision used when formatting results.  |
| `ValidateInputFormat` | Whether strict input format validation is enforced.        |

Client inputs for `DateTime` need a date, time, and offset, for example `2025-02-01T12:00:00.000Z` or `2025-02-01T12:00:00.000+01:00`. A date-only string such as `2025-02-01` is not a `DateTime` value. Use the appropriate date scalar when the contract is a calendar date.

# Use Any for JSON-like metadata and interoperability

`Any` is built in, but many v16 JSON scenarios need explicit `AnyType` annotations or `.AddJsonTypeConverter()`. Use it for extension data, metadata, or interoperability boundaries where the JSON shape is intentionally open.

Prefer object types and input object types when the fields are known. They give clients selectable fields, stronger validation, and better tooling.

```csharp
using System.Text.Json;
using HotChocolate.Types;

[QueryType]
public static partial class MetadataQueries
{
    public static JsonElement Metadata(
        [GraphQLType<AnyType>] JsonElement input)
    {
        return input;
    }

    public static string? MetadataSource(
        [GraphQLType<AnyType>] JsonElement input)
    {
        if (input.ValueKind == JsonValueKind.Object &&
            input.TryGetProperty("source", out var source) &&
            source.ValueKind == JsonValueKind.String)
        {
            return source.GetString();
        }

        return null;
    }
}
```

Expected SDL:

```graphql
type Query {
  metadata(input: Any!): Any!
  metadataSource(input: Any!): String
}
```

Example operation:

```graphql
query {
  metadata(input: { source: "mobile", priority: 1, flags: ["beta"] })
  metadataSource(input: { source: "mobile" })
}
```

Example response:

```json
{
  "data": {
    "metadata": {
      "source": "mobile",
      "priority": 1,
      "flags": ["beta"]
    },
    "metadataSource": "mobile"
  }
}
```

Return `JsonElement` directly when possible:

```csharp
using System.Text.Json;
using HotChocolate.Types;

[QueryType]
public static partial class ImportQueries
{
    [GraphQLType<AnyType>]
    public static JsonElement ImportMetadata()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "source": "import",
              "priority": 1
            }
            """);

        return document.RootElement.Clone();
    }
}
```

If you need to return dictionaries or other JSON-serializable CLR objects through `Any`, register the JSON type converter:

```csharp
builder
    .AddGraphQL()
    .AddJsonTypeConverter();
```

```csharp
using HotChocolate.Types;

[QueryType]
public static partial class ImportQueries
{
    [GraphQLType<AnyType>]
    public static object ImportMetadataObject()
        => new Dictionary<string, object>
        {
            ["source"] = "import",
            ["priority"] = 1
        };
}
```

| Resolver shape                                            | Registration needed                                     | Notes                                                    |
| --------------------------------------------------------- | ------------------------------------------------------- | -------------------------------------------------------- |
| `JsonElement`                                             | None beyond using `Any` in the schema.                  | Preferred for dynamic JSON shapes.                       |
| `Dictionary<string, object>`                              | `.AddJsonTypeConverter()`                               | Watch for trimming constraints and cyclic object graphs. |
| Custom DTO returned as `Any`                              | `.AddJsonTypeConverter()` or a dedicated type converter | Prefer a GraphQL object type when fields are known.      |
| Custom conversion such as `TimeZoneInfo` to a JSON string | `AddTypeConverter<TFrom, JsonElement>()`                | Use for focused conversions, not broad domain modeling.  |

In v16, `Json` is merged into `Any`. Replace old `JsonType` annotations with `AnyType`. Complex input values arrive as `JsonElement`. Cyclic CLR object graphs cannot be coerced to JSON, so return an acyclic DTO or prebuilt `JsonElement`.

`AddJsonTypeConverter()` can use reflection or dynamic code paths in some environments. Treat that as an advanced deployment consideration for trimming and Native AOT scenarios.

# Bind package scalars in filters

A package scalar on an object field does not automatically make generated filter operations use the same scalar. Define a filter operation input type and bind the CLR runtime type in your filtering convention.

```csharp
using HotChocolate.Data.Filters;
using HotChocolate.Types;

public sealed class EmailAddressOperationFilterInputType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<EmailAddressType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<EmailAddressType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<EmailAddressType>();
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddFiltering(x => x
        .AddDefaults()
        .BindRuntimeType<string, EmailAddressOperationFilterInputType>());
```

Use the [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) documentation for the complete filtering configuration model.

# Know when not to use these scalars

- Do not use `Any` as a substitute for object types or input object types when the fields are known.
- Do not use package validation scalars as your only business validation or authorization layer.
- Do not bind common CLR types globally unless that CLR type always represents the same scalar contract in the schema context.
- Do not use this page for NodaTime details. Use [NodaTime Scalars](./nodatime-scalars).
- Do not use this page for custom parsing or result coercion. Use [Custom Scalars](./custom-scalars).

# Troubleshoot common scalar registrations

| Symptom                                                             | Likely cause                                                   | What to do                                                                                      |
| ------------------------------------------------------------------- | -------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `EmailAddress` or another package scalar appears as `String`.       | The member is a `string`, and Hot Chocolate inferred `String`. | Add `[GraphQLType<EmailAddressType>]` or descriptor `.Type<EmailAddressType>()`.                |
| `Latitude` or `Longitude` appears as `Float`.                       | The member is a `double`, and Hot Chocolate inferred `Float`.  | Add `[GraphQLType<LatitudeType>]`, `[GraphQLType<LongitudeType>]`, or descriptor configuration. |
| A package scalar works on output but not in filters.                | Filter operations use separate input types.                    | Add a filter operation input type and bind the runtime type in filtering configuration.         |
| `UUID` output format differs from client expectations.              | The default UUID format is `D`.                                | Register `new UuidType(defaultFormat: 'N')`, `D`, `B`, or `P`.                                  |
| `UUID` accepts a format you wanted to reject.                       | The scalar can parse other supported GUID formats by default.  | Register `new UuidType(defaultFormat: 'D', enforceFormat: true)`.                               |
| `DateTime` rejects a date-only string or a value without an offset. | `DateTime` expects a date, time, and offset.                   | Send a full timestamp or choose the appropriate date or local time scalar.                      |
| `Any` returns an error for dictionaries or CLR objects.             | `Any` centers on `JsonElement` by default.                     | Return `JsonElement` directly or register `.AddJsonTypeConverter()`.                            |
| `Any` returns an error for a cyclic object graph.                   | Cyclic objects cannot be converted to JSON.                    | Return an acyclic DTO or a prebuilt `JsonElement`.                                              |

# Choose your next step

1. Use [Built-in Scalars](./built-in-scalars) for the full scalar mapping table, `ID`, `UUID`, `DateTime`, `Any`, `URI`, and `Base64String` behavior.
2. Use [NodaTime Scalars](./nodatime-scalars) when your domain model uses NodaTime types.
3. Use [Custom Scalars](./custom-scalars) when no built-in or package scalar matches a reusable leaf value contract.
4. Use [Object Types](../object-types) or [Input Object Types](../input-object-types) when clients need known structured fields instead of `Any`.
5. Use [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) when package scalars need custom filter operation bindings.
