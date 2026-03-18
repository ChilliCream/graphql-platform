---
title: "Documentation"
---

GraphQL descriptions enrich your schema with information that consumers see in developer tools, IDE autocompletion, and introspection results. Every type, field, argument, and enum value can carry a description string.

```graphql
"Represents a registered user."
type User {
  "The unique username."
  username: String!
}
```

Hot Chocolate provides two ways to add descriptions: the `[GraphQLDescription]` attribute and XML documentation comments.

# Using GraphQLDescription

The `[GraphQLDescription]` attribute sets a description on any schema element.

<ExampleTabs>
<Implementation>

```csharp
// Types/User.cs
[GraphQLDescription("Represents a registered user.")]
public class User
{
    [GraphQLDescription("The unique username.")]
    public string Username { get; set; }
}

// Types/UserRole.cs
[GraphQLDescription("Available user roles.")]
public enum UserRole
{
    [GraphQLDescription("Full system access.")]
    Administrator,

    [GraphQLDescription("Content moderation access.")]
    Moderator
}

// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [GraphQLDescription("Finds a user by username.")]
    public static User? GetUser(
        [GraphQLDescription("The username to search for.")] string username,
        UserService users)
        => users.FindByName(username);
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("Represents a registered user.");

        descriptor
            .Field(f => f.Username)
            .Description("The unique username.");
    }
}

// Types/UserRoleType.cs
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Description("Available user roles.");

        descriptor
            .Value(UserRole.Administrator)
            .Description("Full system access.");
    }
}
```

In code-first, the `Description()` method takes precedence over all other forms of documentation. This applies even if the provided value is `null` or empty.

</Code>
</ExampleTabs>

# Using XML Documentation Comments

Hot Chocolate can generate descriptions from standard C# XML documentation comments. This lets you maintain a single source of documentation for both your C# code and GraphQL schema.

```csharp
// Types/User.cs
/// <summary>
/// Represents a registered user.
/// </summary>
public class User
{
    /// <summary>
    /// The unique username.
    /// </summary>
    public string Username { get; set; }
}

// Types/UserRole.cs
/// <summary>
/// Available user roles.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Full system access.
    /// </summary>
    Administrator,

    /// <summary>
    /// Content moderation access.
    /// </summary>
    Moderator
}

// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    /// <summary>
    /// Finds a user by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    public static User? GetUser(string username, UserService users)
        => users.FindByName(username);
}
```

## Enabling XML Documentation

To make XML docs available at runtime, enable `GenerateDocumentationFile` in your `.csproj`:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

The `<NoWarn>` element is optional. It suppresses compiler warnings for types without documentation comments.

## Disabling XML Documentation

If you do not want XML comments to appear in the schema:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(opt => opt.UseXmlDocumentation = false);
```

# Priority Order

When both `[GraphQLDescription]` and XML documentation are present, they follow this priority:

1. **`[GraphQLDescription]` attribute** (implementation-first): Used if the value is non-null and non-empty. If null or empty, XML documentation is used as a fallback.
2. **`Description()` method** (code-first): Always takes precedence, even if null or empty.
3. **XML documentation comments**: Used as a fallback when no explicit description is set.

# Custom Naming Conventions

If you use a custom naming convention and XML documentation, pass an `XmlDocumentationProvider` to the convention so descriptions are preserved:

```csharp
// Types/CustomNamingConventions.cs
public class CustomNamingConventions : DefaultNamingConventions
{
    public CustomNamingConventions(
        IDocumentationProvider documentationProvider)
        : base(documentationProvider) { }
}
```

```csharp
// Program.cs
IReadOnlySchemaOptions capturedSchemaOptions;

builder.Services
    .AddGraphQLServer()
    .ModifyOptions(opt => capturedSchemaOptions = opt)
    .AddConvention<INamingConventions>(sp =>
        new CustomNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(
                    capturedSchemaOptions.ResolveXmlDocumentationFileName),
                sp.GetApplicationService<ObjectPool<StringBuilder>>()
                    ?? new NoOpStringBuilderPool())));
```

# Troubleshooting

## Descriptions not appearing in schema

Verify that `GenerateDocumentationFile` is set to `true` in your `.csproj`. Without this, the XML file is not generated and Hot Chocolate has no documentation to read.

## XML docs overridden by empty attribute

In implementation-first, `[GraphQLDescription]` with an empty or null value falls back to XML documentation. In code-first, `Description("")` takes precedence and produces an empty description. Remove the explicit `Description()` call to let XML docs take effect.

## Descriptions missing after adding custom naming convention

When you register a custom `INamingConventions`, you must pass an `XmlDocumentationProvider` to the constructor. Without it, the convention cannot resolve XML documentation.

# Next Steps

- **Need to deprecate fields?** See [Versioning](/docs/hotchocolate/v16/defining-a-schema/versioning).
- **Need to define enums?** See [Enums](/docs/hotchocolate/v16/defining-a-schema/enums).
- **Need to define object types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
