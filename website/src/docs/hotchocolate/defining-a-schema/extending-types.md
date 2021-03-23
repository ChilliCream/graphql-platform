---
title: "Extending Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

In GraphQL we only have one query, mutation, and subscription type. These types can become huge, which makes them hard to maintain. To divide types into separate definitions, GraphQL allows to extend types.

```graphql
type Query {
  foo: String
}

extend type Query {
  bar: String
}
```

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public string GetFoo() => ...
}

[ExtendObjectType(typeof(Query))]
public class QueryExtensions
{
    public string GetBar() => ...
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class QueryType : ObjectType<Query>
{
}

public class QueryTypeExtension : ObjectTypeExtension<Query>
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query")
    }
}

public class Query
{
    public string GetFoo() => ...
}

public class QueryExtensions
{
    public string GetBar() => ...
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```graphql
type Query {
  foo: String
}

extend type Query {
  bar: String
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Extending types with the annotation-based approach

Extending types can be beneficial even with non-root types. Let's say we are building a schema with the annotation-based approach where we use pure C# to describe our types.

Given is the following entity that we do want to extend in our graph with additional fields.

```csharp
public class Session
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Abstract { get; set; }

    public int? TrackId { get; set; }
}
```

We could start adding our GraphQL concerns to this type directly. But often, we want to keep our entity clean from any graph concerns.

To replace the `TrackId` with a field `Track` that returns the `Tack` object we could do the following.

```csharp
[ExtendObjectType(typeof(Session))]
public class SessionResolvers
{
    [BindProperty(nameof(Session.TrackId))]
    public async Task<Track> GetTrackAsync([Parent] Session session) => ...
}
```

We also easily can remove properties that we do not like on our initial type. For instance, let us omit the `Abstract`.

```csharp
[ExtendObjectType(
    typeof(Session),
    IgnoreProperties = new[] { nameof(Session.Abstract) })]
public class SessionResolvers
{
}
```

Further, might we want to be able to add new fields to our entity.

```csharp
[ExtendObjectType(typeof(Session))]
public class SessionResolvers
{
    public async Task<IEnumerable<Speaker>> GetSpeakersAsync([Parent] Session session) => ...
}
```

Moreover, we can extend multiple types at once by extending upon base types or interfaces.

```csharp
[ExtendObjectType(typeof(object))] // <-- we are now extending every type that inhereits from object (essentially every type).
public class SessionResolvers
{
    public string SayHello() => "Hello";
}
```

We can also extend multiple types at once with a type but dedicate specific resolvers to specific types.

```csharp
[ExtendObjectType(typeof(object))] // <-- we are now extending every type that inhereits from object (essentially every type)
public class SessionResolvers
{
    public string Abc([Parent] Session session) => "abc"; // <-- we are only adding this field to the Session type

    public string Def([Parent] Track track) => "def"; // <-- we are only adding this field to the Track type
}
```

Instead of using `typeof(object)` as a selector for extending types you can also use interfaces or other base types.
