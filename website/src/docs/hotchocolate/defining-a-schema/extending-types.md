---
title: "Extending Types"
---

In GraphQL we only have one query, mutation and subscription type. These types can become hugh which makes them hard to maintain. In order to divide types into separate definitions GraphQL allows to extend types.

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

# Extending types with annotation-based approach

Extending types can be extremely useful even with other types in the schema. Lets say we are building a schema with the annotation based approach where we use pure C# to describe out types.

Let us assume we have the following entity that we do not want to extend in our Graph with additional fields.

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

We could just start adding our GraphQL concerns to this type. But often we want to get out entity clean from any Graph concerns.

If we wanted for instance to remove the `TrackId` and replace it with a field `Track` that returns a `Tack` object we could just do the following.

```csharp
[ExtendObjectType(typeof(Session))]
public class SessionResolvers
{
    [BindProperty(nameof(Session.TrackId))]
    public async Task<Track> GetTrackAsync([Parent] Session session) => ...
}
```

We also easily can remove properties that we do not like on our initial type. For instance lets omit the `Abstract`.

```csharp
[ExtendObjectType(
    typeof(Session),
    IgnoreProperties = new[] { nameof(Session.Abstract) })]
public class SessionResolvers
{
}
```

Further, might we want to be able to just add new fields to our entity.

```csharp
[ExtendObjectType(typeof(Session))]
public class SessionResolvers
{
    public async Task<IEnumerable<Speaker>> GetSpeakersAsync([Parent] Session session) => ...
}
```

Moreover, we are able to extend multiple types at once by extending upon base types.

```csharp
[ExtendObjectType(typeof(object))] // <-- we are now extending every type that implements object.
public class SessionResolvers
{
    public string SayHello() => "Hello";
}
```

We can also extend multiple types at once with a type but dedicate specific resolver to specific types.

```csharp
[ExtendObjectType(typeof(object))] // <-- we are now extending every type that implements object.
public class SessionResolvers
{
    public string Abc([Parent] Session session) => "abc"; // <-- we are only adding this field to the Session type

    public string Def([Parent] Track track) => "def"; // <-- we are only adding this field to the Track type
}
```
