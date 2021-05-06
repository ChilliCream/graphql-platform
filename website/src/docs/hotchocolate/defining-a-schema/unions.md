---
title: "Unions"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

A Union is very similar to an interface, except that there are no common fields between the specified types.

Unions are defined in the schema as follows.

```sdl
type TextContent {
  text: String!
}

type ImageContent {
  imageUrl: String!
}

union PostContent = TextContent | ImageContent
```

Learn more about unions [here](https://graphql.org/learn/schema/#union-types).

<!--
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
</ExampleTabs> -->
