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
[GraphQLDescription("Represents a registered user.")]
public class User
{
    [GraphQLDescription("The unique username.")]
    public string Username { get; set; }
}

[GraphQLDescription("Available user roles.")]
public enum UserRole
{
    [GraphQLDescription("Full system access.")]
    Administrator,

    [GraphQLDescription("Content moderation access.")]
    Moderator
}

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

Hot Chocolate generates descriptions from standard C# XML documentation comments. The source generator extracts them at build time, so your C# code is the single source of documentation for both your code and GraphQL schema.

```csharp
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

## Source Generator (Default)

Hot Chocolate's source generator extracts XML documentation comments directly from the source code during compilation. This is the default behavior, no additional project configuration is required.

The source generator reads `<summary>`, `<param>`, `<returns>`, and `<exception>` tags and embeds the extracted text into the generated type configuration. Because this happens at build time, you do not need to ship XML documentation files with your application.

### Supported Tags

| Tag                                 | Usage                                                                |
| ----------------------------------- | -------------------------------------------------------------------- |
| `<summary>`                         | Sets the description of the type, field, or enum value.              |
| `<param name="...">`                | Sets the description of a field argument.                            |
| `<returns>`                         | Appended to the field description under a **Returns** heading.       |
| `<exception cref="..." code="...">` | Appended under an **Errors** heading. Requires the `code` attribute. |
| `<inheritdoc />`                    | Resolves documentation from a base class or interface.               |
| `<inheritdoc cref="..." />`         | Resolves documentation from a specific member.                       |

### Example with Returns and Errors

```csharp
[QueryType]
public static partial class UserQueries
{
    /// <summary>
    /// Finds a user by their unique username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The matching user, or null if not found.</returns>
    /// <exception cref="Exception" code="NOT_AUTHORIZED">
    /// The caller does not have permission to search users.
    /// </exception>
    public static User? GetUser(string username, UserService users)
        => users.FindByName(username);
}
```

This produces a field description that includes the summary, a **Returns** section, and an **Errors** section.

### Disabling Source Generator Documentation

To prevent the source generator from extracting XML documentation, add the `Module` attribute with the `DisableXmlDocumentation` option:

```csharp
using HotChocolate;

[assembly: Module("MyModule", ModuleOptions.Default | ModuleOptions.DisableXmlDocumentation)]
```

When `DisableXmlDocumentation` is set, `[GraphQLDescription]` attributes continue to work. Only the automatic extraction of XML comments is suppressed.

## Runtime XML Documentation

If you are not using the source generator or need XML documentation from referenced assemblies outside the source generator's scope, Hot Chocolate can read XML documentation files at runtime. Enable `GenerateDocumentationFile` in your `.csproj`:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

The `<NoWarn>` element is optional. It suppresses compiler warnings for types without documentation comments.

### Disabling Runtime XML Documentation

If you do not want runtime XML comments to appear in the schema:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(opt => opt.UseXmlDocumentation = false);
```

# Priority Order

When both `[GraphQLDescription]` and XML documentation are present, they follow this priority:

1. **`[GraphQLDescription]` attribute** (implementation-first): Used if the value is non-null and non-empty. If null or empty, XML documentation is used as a fallback.
2. **`Description()` method** (code-first): Always takes precedence, even if null or empty.
3. **XML documentation comments**: Used as a fallback when no explicit description is set.

# Custom Naming Conventions

If you use a custom naming convention and runtime XML documentation, pass an `XmlDocumentationProvider` to the convention so descriptions are preserved. This does not apply when using the source generator, which handles documentation extraction at build time.

```csharp
public class CustomNamingConventions : DefaultNamingConventions
{
    public CustomNamingConventions(
        IDocumentationProvider documentationProvider)
        : base(documentationProvider) { }
}
```

```csharp
IReadOnlySchemaOptions capturedSchemaOptions;

builder
    .AddGraphQL()
    .ModifyOptions(opt => capturedSchemaOptions = opt)
    .AddConvention<INamingConventions>(sp =>
        new CustomNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(
                    capturedSchemaOptions.ResolveXmlDocumentationFileName),
                sp.GetApplicationService<ObjectPool<StringBuilder>>()
                    ?? new NoOpStringBuilderPool())));
```

# Next Steps

- **Need to deprecate fields?** See [Versioning](/docs/hotchocolate/v16/defining-a-schema/versioning).
- **Need to define enums?** See [Enums](/docs/hotchocolate/v16/defining-a-schema/enums).
- **Need to define object types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
