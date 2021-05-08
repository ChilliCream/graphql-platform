---
title: "Arguments"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

GraphQL allows us to specify field arguments to

```sdl
type Query {
  user(username: String!): User
}
```

Learn more about arguments [here](https://user-images.githubusercontent.com/45513122/117534240-d70e9100-aff0-11eb-9973-bc5ddb2b5c3c.png).

# Definition

Arguments can be defined like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>
