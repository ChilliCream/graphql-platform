---
title: "Community Scalars"
---

GraphQL provides a limited set of built-in scalar types. Hot Chocolate extends this set with additional built-in scalars and an optional package, `HotChocolate.Types.Scalars`, which covers common formats like email addresses, IP addresses, colors, coordinates, ISBNs, phone numbers, MAC addresses, and UTC offsets.

Refer to this page when you need to install `HotChocolate.Types.Scalars`, annotate a field or argument with one of its scalar types, or configure built-in scalars that require extra registration. For the full mapping table, see [Built-in Scalars](./built-in-scalars). For NodaTime support, see [NodaTime Scalars](./nodatime-scalars). To create your own scalar, see [Custom Scalars](./custom-scalars).

# Choose the scalar based on the contract

Begin by considering the public contract you want to expose to clients, not only the CLR type used in .NET.

| Contract                                                                                                               | Recommended action                                                                                        | Notes                                                                                                                 |
| ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Built-in CLR mapping such as `Guid`, `DateTimeOffset`, `JsonElement`, numeric variants, `Uri`, or `byte[]`             | Use [Built-in Scalars](./built-in-scalars). Configure here only if the default behavior does not suffice. | Hot Chocolate infers these from CLR types.                                                                            |
| Validated format such as email, IP address, color, phone number, ISBN, latitude, longitude, MAC address, or UTC offset | Install `HotChocolate.Types.Scalars` and annotate fields, arguments, or input fields.                     | Most package scalars share common CLR types like `string` or `double`, so explicit schema typing is usually required. |
| NodaTime runtime model                                                                                                 | Use [NodaTime Scalars](./nodatime-scalars).                                                               | Register `HotChocolate.Types.NodaTime` if your domain model uses NodaTime types.                                      |
| JSON-like metadata or interoperability payload                                                                         | Use `Any` with `JsonElement`. Add a converter only for CLR object or dictionary output.                   | `Any` reduces schema discoverability and validation. Prefer object types when fields are known.                       |
| Domain-specific format or coercion rules                                                                               | Use [Custom Scalars](./custom-scalars).                                                                   | Create a custom scalar only for a reusable leaf value contract.                                                       |

# Add the additional scalars package

Install the package version that matches the rest of your Hot Chocolate application.

<PackageInstallation packageName="HotChocolate.Types.Scalars" />

Next, reference the scalar type from `HotChocolate.Types` in your schema configuration:

```csharp
using HotChocolate.Types;
```

You do not need to register every package scalar globally. Typically, use the scalar type only where the contract is relevant.

# Use a package scalar on a field or argument

By default, a `string` property maps to the `String` scalar, and a `double` property maps to `Float`. If your GraphQL contract requires a more specific type, annotate the field or configure it with a descriptor.

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

If you keep schema metadata outside your model, use descriptor configuration:

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

Prefer local annotations or field configuration instead of schema-wide runtime binding like `BindRuntimeType<string, EmailAddressType>()`. Only use a global string binding if every `string` in your schema context represents the same GraphQL scalar contract.

# Reference: available package scalars

The additional scalars package provides the following scalar types:

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

# Configure UUID for GUID contracts

`Guid` automatically maps to the `UUID` scalar. Use `UUID` when the public value is a UUID that clients may need to parse, validate, or generate. Use `ID` for opaque GraphQL identities.

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

Register `UuidType` if clients require a different output format:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddType(new UuidType(defaultFormat: 'N'));
```

Set `enforceFormat: true` if input must match the configured format:

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

If you are upgrading old schema snapshots, note that older Hot Chocolate versions used the scalar name `Uuid`. The scalar name is `UUID`.

# Configure DateTime for precision or input format

`DateTime` and `DateTimeOffset` map to the built-in `DateTime` scalar. Adjust the configuration only if you need to change the default precision or enforce strict input validation. For the full temporal mapping table, see [Built-in Scalars](./built-in-scalars).

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

Set precision and validation using `DateTimeOptions`:

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

Client inputs for `DateTime` must include a date, time, and offset, such as `2025-02-01T12:00:00.000Z` or `2025-02-01T12:00:00.000+01:00`. A date-only string like `2025-02-01` is not a valid `DateTime` value. Use a date scalar when the contract is a calendar date.

# Use Any for JSON-like metadata and interoperability

The `Any` scalar is built in, but many JSON scenarios require explicit `AnyType` annotations or `.AddJsonTypeConverter()`. Use `Any` for extension data, metadata, or interoperability boundaries where the JSON shape is intentionally open.

When the fields are known, prefer object types and input object types. These provide clients with selectable fields, stronger validation, and improved tooling.

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

| Resolver shape                                            | Registration needed                                     | Notes                                                      |
| --------------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------------- |
| `JsonElement`                                             | None beyond using `Any` in the schema.                  | Preferred for dynamic JSON shapes.                         |
| `Dictionary<string, object>`                              | `.AddJsonTypeConverter()`                               | Be aware of trimming constraints and cyclic object graphs. |
| Custom DTO returned as `Any`                              | `.AddJsonTypeConverter()` or a dedicated type converter | Prefer a GraphQL object type when fields are known.        |
| Custom conversion such as `TimeZoneInfo` to a JSON string | `AddTypeConverter<TFrom, JsonElement>()`                | Use for focused conversions, not broad domain modeling.    |

`Json` is merged into `Any`. Replace old `JsonType` annotations with `AnyType`. Complex input values arrive as `JsonElement`. Cyclic CLR object graphs cannot be coerced to JSON, so return an acyclic DTO or prebuilt `JsonElement`.

`AddJsonTypeConverter()` may use reflection or dynamic code paths in some environments. Consider this for advanced deployment scenarios such as trimming and Native AOT.

# Bind package scalars in filters

Adding a package scalar to an object field does not automatically apply it to generated filter operations. To use the same scalar in filters, define a filter operation input type and bind the CLR runtime type in your filtering convention.

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

See the [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) documentation for the full filtering configuration model.

# When not to use these scalars

- Avoid using `Any` as a replacement for object types or input object types when the fields are known.
- Do not rely on package validation scalars as your only business validation or authorization layer.
- Only bind common CLR types globally if that CLR type always represents the same scalar contract in your schema context.
- For NodaTime details, see [NodaTime Scalars](./nodatime-scalars).
- For custom parsing or result coercion, see [Custom Scalars](./custom-scalars).

# Troubleshooting common scalar registrations

| Symptom                                                             | Likely cause                                                   | Solution                                                                                     |
| ------------------------------------------------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `EmailAddress` or another package scalar appears as `String`.       | The member is a `string`, and Hot Chocolate inferred `String`. | Add `[GraphQLType<EmailAddressType>]` or use a descriptor with `.Type<EmailAddressType>()`.  |
| `Latitude` or `Longitude` appears as `Float`.                       | The member is a `double`, and Hot Chocolate inferred `Float`.  | Add `[GraphQLType<LatitudeType>]`, `[GraphQLType<LongitudeType>]`, or use descriptor config. |
| A package scalar works on output but not in filters.                | Filter operations use separate input types.                    | Add a filter operation input type and bind the runtime type in filtering configuration.      |
| `UUID` output format differs from client expectations.              | The default UUID format is `D`.                                | Register `new UuidType(defaultFormat: 'N')`, `D`, `B`, or `P`.                               |
| `UUID` accepts a format you wanted to reject.                       | The scalar can parse other supported GUID formats by default.  | Register `new UuidType(defaultFormat: 'D', enforceFormat: true)`.                            |
| `DateTime` rejects a date-only string or a value without an offset. | `DateTime` expects a date, time, and offset.                   | Send a full timestamp or choose the appropriate date or local time scalar.                   |
| `Any` returns an error for dictionaries or CLR objects.             | `Any` centers on `JsonElement` by default.                     | Return `JsonElement` directly or register `.AddJsonTypeConverter()`.                         |
| `Any` returns an error for a cyclic object graph.                   | Cyclic objects cannot be converted to JSON.                    | Return an acyclic DTO or a prebuilt `JsonElement`.                                           |

# Next steps

1. See [Built-in Scalars](./built-in-scalars) for the full scalar mapping table, including `ID`, `UUID`, `DateTime`, `Any`, `URI`, and `Base64String`.
2. Use [NodaTime Scalars](./nodatime-scalars) if your domain model uses NodaTime types.
3. Refer to [Custom Scalars](./custom-scalars) when no built-in or package scalar matches your reusable leaf value contract.
4. Use [Object Types](../object-types) or [Input Object Types](../input-object-types) when clients need structured fields instead of `Any`.
5. See [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) for custom filter operation bindings with package scalars.
