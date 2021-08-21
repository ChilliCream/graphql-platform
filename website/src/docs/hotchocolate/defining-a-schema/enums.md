---
title: "Enums"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

An Enum is a special kind of [scalar](/docs/hotchocolate/defining-a-schema/scalars) that is restricted to a particular set of allowed values.

```sdl
enum UserRole {
  GUEST,
  DEFAULT,
  ADMINISTRATOR
}
```

Learn more about enums [here](https://graphql.org/learn/schema/#enumeration-types).

# Usage

We can define enums like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public enum UserRole
{
    Guest,
    Standard,
    Administrator
}

public class Query
{
    public User[] GetUsers(UserRole role)
    {
        // Omitted code for brevity
    }
}
```

We can also specify different names for the type and values to be used in the schema.

```csharp
[GraphQLName("NewUserRole")]
public enum UserRole
{
    Guest,
    [GraphQLName("Default")]
    Standard,
    Administrator
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public enum UserRole
{
    Guest,
    Standard,
    Administrator
}

public class UserRoleType : EnumType<UserRole>
{
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Argument("role", a => a.Type<UserRoleType>())
            .Resolve(context =>
            {
                var role = context.ArgumentValue<UserRole>("role");

                // Omitted code for brevity
            });
    }
}
```

We can also specify different names for the type and values to be used in the schema.

```csharp
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Name("NewUserRole");

        descriptor
            .Value(UserRole.Standard)
            .Name("Default");
    }
}
```

We can also bind the enumeration type to any other .NET type.

```csharp
public class UserRoleType : EnumType<string>
{
    protected override void Configure(IEnumTypeDescriptor<string> descriptor)
    {
        // we need to specify a name or otherwise we will get a conflict
        // with the built-in StringType
        descriptor.Name("UserRole");

        descriptor
            .Value("Default")
            .Name("Standard");
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Argument("role", a => a.Type<UserRoleType>())
            .Resolve(context =>
            {
                var role = context.ArgumentValue<string>("role");

                // Omitted code for brevity
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          user(role: UserRole): User
        }

        enum UserRole {
          GUEST,
          DEFAULT,
          ADMINISTRATOR
        }
    ")
    .AddResolver("Query", "user", (context) =>
    {
        var role = context.ArgumentValue<string>("role");

        // Omitted code for brevity
    })
```

</ExampleTabs.Schema>
</ExampleTabs>

# Binding behavior

In the Annotation-based approach all enum values are implicitly included on the schema enum type. The same is true for `T` of `EnumType<T>` when using the Code-first approach.

In the Code-first approach we can also enable explicit binding, where we have to opt-in enum values we want to include instead of them being implicitly included.

<!-- todo: this should not be covered in each type documentation, rather once in a server configuration section -->

We can configure our preferred binding behavior globally like the following.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

> ⚠️ Note: This changes the binding behavior for all types, not only enum types.

We can also override it on a per type basis:

```csharp
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.BindValues(BindingBehavior.Implicit);

        // We could also use the following methods respectively
        // descriptor.BindValuesExplicitly();
        // descriptor.BindValuesImplicitly();
    }
}
```

## Ignoring values

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach we can ignore values using the `[GraphQLIgnore]` attribute.

```csharp
public enum UserRole
{
    [GraphQLIgnore]
    Guest,
    Standard,
    Administrator
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we can ignore values using the `Ignore` method on the `IEnumTypeDescriptor`. This is only necessary, if the binding behavior of the enum type is implicit.

```csharp
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Ignore(UserRole.Guest);
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

We do not have to ignore values in the Schema-first approach.

</ExampleTabs.Schema>
</ExampleTabs>

## Including values

In the Code-first approach we can explicitly include values using the `Value` method on the `IEnumTypeDescriptor`. This is only necessary, if the binding behavior of the enum type is explicit.

```csharp
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.BindValuesExplicitly();

        descriptor.Value(UserRole.Guest);
    }
}
```

# Naming

Unless specified explicitly, Hot Chocolate automatically infers the names of enums and their values. Per default the name of the enum becomes the name of the enum type. When using `EnumType<T>` in Code-first, the name of `T` is chosen as the name for the enum type.

Enum values are automatically formatted to the UPPER_SNAIL_CASE according to the GraphQL specification:

- `Guest` becomes `GUEST`
- `HeadOfDepartment` becomes `HEAD_OF_DEPARTMENT`

If we need to we can override these inferred names.

<ExampleTabs>
<ExampleTabs.Annotation>

The `[GraphQLName]` attribute allows us to specify an explicit name.

```csharp
[GraphQLName("Role")]
public enum UserRole
{
    [GraphQLName("VISITOR")]
    Guest,
    Standard,
    Administrator
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

The `Name` method on the `IEnumTypeDescriptor` / `IEnumValueDescriptor` allows us to specify an explicit name.

```csharp-
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Name("Role");

        descriptor.Value(UserRole.Guest).Name("VISITOR");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply change the names in the schema.

</ExampleTabs.Schema>
</ExampleTabs>

This would produce the following `Role` schema enum type:

```sdl
enum Role {
  VISITOR,
  STANDARD,
  ADMINISTRATOR
}
```
