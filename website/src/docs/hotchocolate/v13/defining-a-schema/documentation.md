---
title: Documentation
---

Documentation allows us to enrich our schema with additional information that is useful for a consumer of our API.

In GraphQL we can do this by providing descriptions to our types, fields, etc.

```sdl
type Query {
  "A query field"
  user("An argument" username: String): User
}

"An object type"
type User {
  "A field"
  username: String
}

"An enum"
enum UserRole {
  "An enum value"
  ADMINISTRATOR
}
```

# Usage

We can define descriptions like the following.

<ExampleTabs>
<Implementation>

```csharp
[GraphQLDescription("An object type")]
public class User
{
    [GraphQLDescription("A field")]
    public string Username { get; set; }
}

[GraphQLDescription("An enum")]
public enum UserRole
{
    [GraphQLDescription("An enum value")]
    Administrator
}

public class Query
{
    [GraphQLDescription("A query field")]
    public User GetUser(
        [GraphQLDescription("An argument")] string username)
    {
        // Omitted code for brevity
    }
}
```

If the description provided to the `GraphQLDescriptionAttribute` is `null` or made up of only white space, XML documentation comments are used as a fallback.

Learn more about XML documentation below.

</Implementation>
<Code>

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");
        descriptor.Description("An object type");

        descriptor
            .Field(f => f.Username)
            .Description("A field");
    }
}

public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Name("UserRole");
        descriptor.Description("An enum");

        descriptor
            .Value(UserRole.Administrator)
            .Description("An enum value");
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("user")
            .Description("A query field")
            .Argument("username", a => a.Type<StringType>()
                                        .Description("An argument"))
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

The `Description()` methods take precedence over all other forms of documentation. This is true, even if the provided value is `null` or only white space.

</Code>
<Schema>

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
            """"""
            A query field
            """"""
            user(""An argument"" username: String): User
        }

        """"""
        An object type
        """"""
        type User {
            ""A field""
            username: String
        }

        """"""
        An enum
        """"""
        enum UserRole {
            ""An enum value""
            ADMINISTRATOR
        }
    ")
    // Omitted code for brevity
```

</Schema>
</ExampleTabs>

# XML Documentation

Hot Chocolate provides the ability to automatically generate API documentation from our existing [XML documentation](https://docs.microsoft.com/dotnet/csharp/codedoc).

The following will produce the same schema descriptions we declared above.

```csharp
/// <summary>
/// An object type
/// </summary>
public class User
{
    /// <summary>
    /// A field
    /// </summary>
    public string Username { get; set; }
}

/// <summary>
/// An enum
/// </summary>
public enum UserRole
{
    /// <summary>
    /// An enum value
    /// </summary>
    Administrator
}

public class Query
{
    /// <summary>
    /// A query field
    /// </summary>
    /// <param name="username">An argument</param>
    public User GetUser(string username)
    {
        // Omitted code for brevity
    }
}
```

To make the XML documentation available to Hot Chocolate, we have to enable `GenerateDocumentationFile` in our `.csproj` file.

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

> Note: The `<NoWarn>` element is optional. It prevents the compiler from emitting warnings for missing documentation strings.

If we do not want to include XML documentation in our schema, we can set the `UseXmlDocumentation` property on the schema's `ISchemaOptions`.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(opt => opt.UseXmlDocumentation = false);
```

## With a custom naming convention

If you want to use a custom naming convention and XML documentation, ensure you give the convention an instance of the `XmlDocumentationProvider` as demonstrated below; otherwise the comments won't appear in your schema.

```csharp
public class CustomNamingConventions : DefaultNamingConventions
{
    // Before
    public CustomNamingConventions()
        : base() { }

    // After
    public CustomNamingConventions(IDocumentationProvider documentationProvider)
        : base(documentationProvider) { }
}

// Startup
// Before
.AddConvention<INamingConventions>(sp => new CustomNamingConventions());

// After
IReadOnlySchemaOptions capturedSchemaOptions;

services
    .AddGraphQLServer()
    .ModifyOptions(opt => capturedSchemaOptions = opt)
    .AddConvention<INamingConventions>(sp => new CustomNamingConventions(
        new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(
                capturedSchemaOptions.ResolveXmlDocumentationFileName),
            sp.GetApplicationService<ObjectPool<StringBuilder>>()
              ?? new NoOpStringBuilderPool())));
```
