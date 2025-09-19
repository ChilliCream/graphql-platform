---
title: "Versioning"
---

Whilst we could version our GraphQL API similar to REST, i.e. `/graphql/v1`, it is not a best practice and often unnecessary.

Many changes to a GraphQL schema are non-breaking. We can freely add new types and extend existing types with new fields. This does not break existing queries.
However removing a field or changing its nullability does.

Instead of removing a field immediately and possibly breaking existing consumers of our API, fields can be marked as deprecated in our schema. This signals to consumers that the field will be removed in the future and they need to adapt before then.

```sdl
type Query {
  users: [User] @deprecated("Use the `authors` field instead")
  authors: [User]
}

```

# Deprecating fields

Fields can be deprecated like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [GraphQLDeprecated("Use the `authors` field instead")]
    public User[] GetUsers()
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
        descriptor
            .Field("users")
            .Deprecated("Use the `authors` field instead")
            .Resolve(context =>
            {
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
          users: [User] @deprecated(""Use the `authors` field instead"")
        }
    ");
```

</Schema>
</ExampleTabs>

> Note: It is currently not possible to deprecate input values, such as arguments.
