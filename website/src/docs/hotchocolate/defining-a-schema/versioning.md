---
title: "Versioning"
---

GraphQL versioning works differently as the versioning you know from REST.
While nothing stops you from versioning a GraphQL API like a REST API, it is not best practice to do so and most often is not needed.

A GraphQL API can evolve. Many changes to a GraphQL Schema are non-breaking changes. 
You are free to add fields to a type for example. This does not break existing queries.

If you remove fields or change the nullability of a field, the contract with existing queries is broken.
In GraphQL it is possible to deprecate fields. 
You can mark a field as deprecated to signal API consumers that a field is obsolete and will be removed in the future.

**Annotation Based**
```csharp
public class Query 
{
    [Deprecated("Use `persons` field instead")]
    public User[] GetUsers() { ... } 

    public User[] GetPersons() { ... } 
}
```

**Code First**
```csharp
public class Query : QueryType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(x => x.GetUsers()).Deprecated("Use `persons` field instead");
    }
}
```

**Schema First**
```sdl
type Query {
    users: [Users] @deprecated("Use `persons` field instead")
    persons: [Users] 
}
```
