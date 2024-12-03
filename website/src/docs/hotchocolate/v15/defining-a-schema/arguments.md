---
title: "Arguments"
---

GraphQL allows us to specify arguments on a field and access their values in the field's resolver.

```sdl
type Query {
  user(id: ID!): User
}
```

Clients can specify arguments like the following.

```graphql
{
  user(id: "123") {
    username
  }
}
```

Often times arguments will be specified using variables.

```graphql
query ($userId: ID!) {
  user(id: $userId) {
    username
  }
}
```

Learn more about arguments [here](https://graphql.org/learn/schema/#arguments) and variables [here](https://graphql.org/learn/queries/#variables).

# Usage

Arguments can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    public User GetUser(string username)
    {
        // Omitted code for brevity
    }
}
```

We can also change the name of the argument used in the schema.

```csharp
public class Query
{
    public User GetUser([GraphQLName("name")] string username)
    {
        // Omitted code for brevity
    }
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("user")
            .Argument("username", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var username = context.ArgumentValue<string>("username");

                // Omitted code for brevity
            });
    }
}
```

We can also access nullable values through an `Optional<T>`.

```csharp
var username = context.ArgumentOptional<string>("username");

if (username.HasValue)
{
    // use username.Value
}
```

</Code>
<Schema>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          user(username: String!): User
        }
    ")
    .AddResolver("Query", "user", (context) =>
    {
        var username = context.ArgumentValue<string>("username");

        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>

Arguments can be made required by using the non-null type. Learn more about [non-null](/docs/hotchocolate/v15/defining-a-schema/non-null)

If we need to provide complex objects as arguments, we can use [input object types](/docs/hotchocolate/v15/defining-a-schema/input-object-types).
