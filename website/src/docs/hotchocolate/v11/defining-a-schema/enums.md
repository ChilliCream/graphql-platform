---
title: "Enums"
---

An Enum is a special kind of [scalar](/docs/hotchocolate/v11/defining-a-schema/scalars) that is restricted to a particular set of allowed values.

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
    .AddResolver("Query", "user", (context) =>
    {
        var role = context.ArgumentValue<string>("role");

        // Omitted code for brevity
    })
```

</Schema>
</ExampleTabs>
