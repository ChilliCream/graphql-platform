---
title: "Lists"
---

GraphQL allows us to return lists of elements from our fields.

```sdl
type Query {
  users: [User]
}
```

Clients can query list fields like any other field.

```graphql
{
  users {
    id
    name
  }
}
```

Querying a list field will result in an ordered list containing elements with the specified sub-selection of fields.

Learn more about lists [here](https://graphql.org/learn/schema/#lists-and-non-null).

# Usage

Lists can be defined like the following.

<ExampleTabs>
<Implementation>

If our field resolver returns a list type, e.g. `IEnumerable<T>` or `IQueryable<T>`, it will automatically be treated as a list type in the schema.

```csharp
public class Query
{
    public List<User> GetUsers()
    {
        // Omitted code for brevity
    }
}
```

</Implementation>
<Code>

If our field resolver returns a list type, e.g. `IEnumerable<T>` or `IQueryable<T>`, it will automatically be treated as a list type in the schema.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Resolve(context =>
            {
                List<User> users = null;

                // Omitted code for brevity

                return users;
            });
    }
}
```

We can also be more explicit by specifying a `ListType<Type>` as the return type.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Type<ListType<UserType>>()
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

```sdl
type Query {
  users: [User]
}
```

</Schema>
</ExampleTabs>
