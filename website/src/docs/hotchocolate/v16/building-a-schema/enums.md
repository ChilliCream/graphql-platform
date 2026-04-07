---
title: "Enums"
---

A GraphQL enum is a special scalar restricted to a fixed set of allowed values. Enums work as both input and output types. In C#, a standard `enum` maps directly to a GraphQL enum type.

**GraphQL schema**

```graphql
enum UserRole {
  GUEST
  STANDARD
  ADMINISTRATOR
}

type Query {
  role: UserRole
  usersByRole(role: UserRole): [User]
}
```

**Client query**

```graphql
{
  usersByRole(role: ADMINISTRATOR) {
    id
  }
}
```

When an enum appears in a JSON response or in a variables object, it is represented as a string (`"ADMINISTRATOR"`). When used directly in a query argument, it is a literal without quotes (`ADMINISTRATOR`).

# Defining an Enum Type

Hot Chocolate picks up any C# `enum` that appears in a resolver's return type or parameters and exposes it as a GraphQL enum.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserRole.cs
public enum UserRole
{
    Guest,
    Standard,
    Administrator
}

// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static User[] GetUsersByRole(UserRole role)
    {
        // ...
    }
}
```

No extra registration is needed. The source generator discovers `UserRole` through the resolver parameter.

</Implementation>
<Code>

```csharp
// Types/UserRole.cs
public enum UserRole
{
    Guest,
    Standard,
    Administrator
}

// Types/UserRoleType.cs
public class UserRoleType : EnumType<UserRole>
{
}
```

Code-first enum types are not automatically inferred because multiple `EnumType<UserRole>` subclasses could exist with different configurations. Register the type explicitly or specify it on a per-field basis:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddType<UserRoleType>();
```

</Code>
</ExampleTabs>

# Naming Conventions

Hot Chocolate converts C# enum member names to `UPPER_SNAKE_CASE` following the GraphQL convention:

| C# member          | GraphQL value        |
| ------------------ | -------------------- |
| `Guest`            | `GUEST`              |
| `HeadOfDepartment` | `HEAD_OF_DEPARTMENT` |

The enum type name defaults to the C# type name (`UserRole`).

## Overriding Names

Use `[GraphQLName]` to set an explicit name on the type or individual values.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserRole.cs
[GraphQLName("Role")]
public enum UserRole
{
    [GraphQLName("VISITOR")]
    Guest,
    Standard,
    Administrator
}
```

</Implementation>
<Code>

```csharp
// Types/UserRoleType.cs
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Name("Role");

        descriptor.Value(UserRole.Guest).Name("VISITOR");
    }
}
```

</Code>
</ExampleTabs>

Both approaches produce the following schema:

```graphql
enum Role {
  VISITOR
  STANDARD
  ADMINISTRATOR
}
```

# Ignoring Values

You can exclude individual enum members from the GraphQL schema.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserRole.cs
public enum UserRole
{
    [GraphQLIgnore]
    Internal,
    Guest,
    Standard,
    Administrator
}
```

</Implementation>
<Code>

```csharp
// Types/UserRoleType.cs
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Ignore(UserRole.Internal);
    }
}
```

</Code>
</ExampleTabs>

# Binding to Non-Enum Types

In code-first, you can bind an enum type to any .NET type, such as `string`.

```csharp
// Types/UserRoleType.cs
public class UserRoleType : EnumType<string>
{
    protected override void Configure(IEnumTypeDescriptor<string> descriptor)
    {
        descriptor.Name("UserRole");

        descriptor
            .Value("Default")
            .Name("STANDARD");
    }
}
```

This is useful when enum values come from configuration or a database rather than a compile-time C# enum.

# Next Steps

- **Need to define output types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need nullable or required fields?** See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null).
- **Need to document enum values?** See [Documentation](/docs/hotchocolate/v16/defining-a-schema/documentation).
- **Need to deprecate an enum value?** See [Versioning](/docs/hotchocolate/v16/defining-a-schema/versioning).
