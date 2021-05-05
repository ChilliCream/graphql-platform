---
title: "Unions"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Unions are very similar to interfaces. The difference is that members of an unions do not have fields in common.
Unions are useful if you have completely disjunct structured types.

```sdl
type Group {
  id: ID!
  members: [GroupMember]
}

type User {
  userName: String!
}

union GroupMember = User | Group
```

## Querying Unions

Union types do not have fields in common.  
You have to use [Inline Fragments](https://spec.graphql.org/June2018/#sec-Inline-Fragments) to query for fields of a specific implementation.

```graphql
{
  accessControl {
    __typename
    ... on Group {
      id
    }
    ... on User {
      userName
    }
  }
}
```

```json
{
  "accessControl": [
    {
      "__typename": "Group",
      "id": "R3JvdXA6MQ=="
    },
    {
      "__typename": "User",
      "userName": "SpicyChicken404"
    },
    {
      "__typename": "User",
      "userName": "CookingMaster86"
    }
  ]
}
```

## Union Definition

<ExampleTabs>
<ExampleTabs.Annotation>

In the annotation based approach, HotChocolate tries to infer union types from the .Net types.
You can manage the membership of union types with a marker interface.

```csharp
[UnionType("GroupMember")]
public interface IGroupMember
{
}

public class Group : IGroupMember
{
  [Id]
  public Guid Identifier { get; set; }

  public IGroupMember[] Members { get; set; }
}

public class User : IGroupMember
{
  public string UserName { get; set; }
}

public class Query
{
  public IGroupMember[] GetAccessControl([Service]IAccessRepo repo) => repo.GetItems();
}
```

_Configure Services_

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services
          .AddRouting()
          .AddGraphQLServer()
          // HotChocolate will pick up IGroupMember as a UnionType<IGroupMember>
          .AddQueryType<Query>()
          // HotChocolate knows that User and Group implement IGroupMember and will add it to the
          // list of possible types of the UnionType
          .AddType<Group>()
          .AddType<User>()
  }
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

HotChocolate provides a fluent configuration API for union types that is very similar to the `ObjectType` interface.

```csharp
// In case you have a marker interface and want to configure it, you can also just user UnionType<IMarkerInterface>
public class GroupMemberType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        // Configure Type Name
        descriptor.Name("GroupMember");

        // Declare Possible Types
        descriptor.Type<GroupType>();
        descriptor.Type<UserType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

In schema first unions can be declared directly in SDL:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddDocumentFromString(@"
        type Query {
            accessControl: [GroupMember]
        }

        type Group {
            id: ID!
            members: [GroupMember]
        }

        type User {
            userName: String!
        }

        union GroupMember = User | Group
        ")
        .AddResolver(
            "Query",
            "accessControl",
            (context, token) => context.Service<IAccessRepo>().GetItems());
}
```

</ExampleTabs.Schema>
</ExampleTabs>
