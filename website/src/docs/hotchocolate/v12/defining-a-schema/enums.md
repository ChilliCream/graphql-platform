---
title: "Enums"
---

An Enum is a special kind of [scalar](/docs/hotchocolate/v12/defining-a-schema/scalars) that is restricted to a particular set of allowed values. It can be used as both an input and an output type.

```sdl
enum UserRole {
  GUEST,
  DEFAULT,
  ADMINISTRATOR
}

type Query {
  role: UserRole
  usersByRole(role: UserRole): [User]
}
```

# Usage

Given is the schema from above.

When querying a field returning an enum type, the enum value will be serialized as a string.

**Request**

```graphql
{
  role
}
```

**Response**

```json
{
  "data": {
    "role": "STANDARD"
  }
}
```

When using an enum value as an argument, it is represented as a literal and **not** a string.

**Request**

```graphql
{
  usersByRole(role: ADMINISTRATOR) {
    id
  }
}
```

When used as a type for a variable, it is represented as a string in the variables object, since JSON does not offer support for literals.

**Request**

Operation:

```graphql
query ($role: UserRole) {
  usersByRole(role: $role) {
    id
  }
}
```

Variables:

```json
{
  "role": "ADMINISTRATOR"
}
```

# Definition

We can define enums like the following.

<ExampleTabs>
<Implementation>

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

</Implementation>
<Code>

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

Since there could be multiple enum types inheriting from `EnumType<UserRole>`, but differing in their name and values, it is not certain which of these types should be used when we return a `UserRole` CLR type from one of our resolvers.

**Therefore it's important to note that code-first enum types are not automatically inferred. They need to be explicitly specified or registered.**

We can either [explicitly specify the type on a per-resolver basis](/docs/hotchocolate/v12/defining-a-schema/object-types#explicit-types) or we can register the type once globally:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddType<UserRoleType>();
    }
}
```

With this configuration each `UserRole` CLR type we return from our resolvers would be assumed to be a `UserRoleType`.

</Code>
<Schema>

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
    .AddResolver("Query", "user", (context) =>-
    {
        var role = context.ArgumentValue<string>("role");

        // Omitted code for brevity
    })
```

</Schema>
</ExampleTabs>

## Non-enum values

In code-first we can also bind the enum type to any other .NET type, for example a `string`.

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
            .Name("STANDARD");
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

# Binding behavior

In the implementation-first approach all enum values are implicitly included on the schema enum type. The same is true for `T` of `EnumType<T>` when using the code-first approach.

In the code-first approach we can also enable explicit binding, where we have to opt-in enum values we want to include instead of them being implicitly included.

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

> Warning: This changes the binding behavior for all types, not only enum types.

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
<Implementation>

In the implementation-first approach we can ignore values using the `[GraphQLIgnore]` attribute.

```csharp
public enum UserRole
{
    [GraphQLIgnore]
    Guest,
    Standard,
    Administrator
}
```

</Implementation>
<Code>

In the code-first approach we can ignore values using the `Ignore` method on the `IEnumTypeDescriptor`. This is only necessary, if the binding behavior of the enum type is implicit.

```csharp
public class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Ignore(UserRole.Guest);
    }
}
```

</Code>
<Schema>

We do not have to ignore values in the schema-first approach.

</Schema>
</ExampleTabs>

## Including values

In the code-first approach we can explicitly include values using the `Value` method on the `IEnumTypeDescriptor`. This is only necessary, if the binding behavior of the enum type is explicit.

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

Unless specified explicitly, Hot Chocolate automatically infers the names of enums and their values. Per default the name of the enum becomes the name of the enum type. When using `EnumType<T>` in code-first, the name of `T` is chosen as the name for the enum type.

Enum values are automatically formatted to the UPPER_SNAIL_CASE according to the GraphQL specification:

- `Guest` becomes `GUEST`
- `HeadOfDepartment` becomes `HEAD_OF_DEPARTMENT`

If we need to we can override these inferred names.

<ExampleTabs>
<Implementation>

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

</Implementation>
<Code>

The `Name` method on the `IEnumTypeDescriptor` / `IEnumValueDescriptor` allows us to specify an explicit name.

```csharp
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
<Schema>

Simply change the names in the schema.

</Schema>
</ExampleTabs>

This would produce the following `Role` schema enum type:

```sdl
enum Role {
  VISITOR,
  STANDARD,
  ADMINISTRATOR
}
```
