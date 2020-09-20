---
path: "/blog/2018/09/02/hot-chocolate-0.4.5"
date: "2018-09-02"
title: "GraphQL - Hot Chocolate 0.4.5"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

With version 0.4.5 we closed a lot of spec gaps and refined the schema configuration API.

We now are finished with implementing the query validation rules. The following rules were added since version 0.4.0:

- Argument Names [111](https://github.com/ChilliCream/hotchocolate/issues/111)
- Fragments Must Be Used [116](https://github.com/ChilliCream/hotchocolate/issues/116)
- Fragment Name Uniqueness [113](https://github.com/ChilliCream/hotchocolate/issues/113)
- Leaf Field Selections [110](https://github.com/ChilliCream/hotchocolate/issues/110)
- Fragments On Composite Types [115](https://github.com/ChilliCream/hotchocolate/issues/115)
- Fragment spreads must not form cycles [118](https://github.com/ChilliCream/hotchocolate/issues/118)
- Fragment spread target defined [117](https://github.com/ChilliCream/hotchocolate/issues/117)
- Fragment spread is possible [119](https://github.com/ChilliCream/hotchocolate/issues/119)
- Fragment Spread Type Existence [114](https://github.com/ChilliCream/hotchocolate/issues/114)
- Input Object Field Names [121](https://github.com/ChilliCream/hotchocolate/issues/121)
- Input Object Required Fields [123](https://github.com/ChilliCream/hotchocolate/issues/123)
- Input Object Field Uniqueness [122](https://github.com/ChilliCream/hotchocolate/issues/122)
- Directives Are Defined [124](https://github.com/ChilliCream/hotchocolate/issues/124)
- Values of Correct Type [120](https://github.com/ChilliCream/hotchocolate/issues/120)

We now also support the `@deprectaed` directive when using schema-first.

Furthermore, we fixed a lot of bugs around schema-first. So, at the moment code-first is still the most viable way to create a schema,but we are working hard to get both flavours on par.

Apart from that we now allow for non-terminating errors within a field-resolver.

```csharp
public IEnumerable<ICharacter> GetCharacter(string[] characterIds, IResolverContext context)
{
    foreach (string characterId in characterIds)
    {
        ICharacter character = _repository.GetCharacter(characterId);
        if (character == null)
        {
            context.ReportError(
                "Could not resolve a character for the " +
                $"character-id {characterId}.");
        }
        else
        {
            yield return character;
        }
    }
}
```

If you want to share resolver logic between types in your schema you can now do that with shared resolvers which can be bound to fields:

```csharp
public class PersonResolvers
{
    public Task<IEnumerable<Person>> GetFriends(Person person, [Service]IPersonRepository repository)
    {
        return repository.GetFriendsAsync(person.FriendIds);
    }
}

public class PersonType : ObjectType<Person>
{
    protected override void Configure(IObjectDescriptor<Person> desc)
    {
        desc.Field(t => t.FriendIds).Ignore();
        desc.Field<PersonResolver>(t => t.GetFriends(default, default));
    }
}
```

## What Comes Next

With version 0.5 we will focus on subscriptions and custom directives.

Custom will allow for writing field resolver middlewares that alter or replace the default execution behaviour.

Subscriptions is one of our last spec gaps.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
