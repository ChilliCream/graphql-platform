---
title: "Versioning"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Whilst we could version our GraphQL API similar to REST, i.e. `/graphql/v1`, it is not a best practice and often unnecessary.

Many changes to a GraphQL schema are non-breaking. We can freely add new types and extend existing types with new fields. This does not break existing queries.
However removing a field or changing its nullability does.

Instead of removing a field immediatly and possibly breaking existing queries, GraphQL allows you to mark fields as deprecated. This signals to API consumers that the field will be removed in the future and they can adapt their queries to this change.

```sdl
type Query {
  users: [User] @deprecated("Use the `authors` field instead")
  authors: [User]
}

```

# Definition

We can deprecate fields like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          users: [User] @deprecated(""Use the `authors` field instead"")
        }
    ")
    // Omitted code for brevity

```

</ExampleTabs.Schema>
</ExampleTabs>
