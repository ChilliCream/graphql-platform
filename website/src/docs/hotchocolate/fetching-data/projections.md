---
title: Projections
---

Every GraphQL request specifies exactly what data should be returned. Over or under fetching can be reduced
or even eliminated. HotChocolate projections leverage this concept and directly projects incoming queries
to the database.

Projections operate on `IQueryable` by default, but it is possible to create custom providers for projections
to support a specific database driver.

```graphql
{
  users {
    email
    address {
      street
    }
  }
}
```

```sql
SELECT "u"."Email", "a"."Id" IS NOT NULL, "a"."Street"
FROM "Users" AS "u"
LEFT JOIN "Address" AS "a" ON "u"."AddressId" = "a"."Id"
```

# Getting Started

Filtering is part of the `HotChocolate.Data` package. You can add the dependency with the `dotnet` cli

```bash
  dotnet add package HotChocolate.Data
```

To use projections with your GraphQL endpoint you have to register projections on the schema:

```csharp
services.AddGraphQLServer()
  // Your schmea configuration
  .AddProjections();
```

Projections can be registered on a field. A middleware will apply the selected fields on the result.
Support for `IQueryable` comes out of the box.
The projection middleware will create a projection for the whole subtree of its field. Only fields that
are members of a type will be projected. Fields that define a customer resolver cannot be projected
to the database. If the middleware encounters a field that specifies `UseProjection()` this field will be skipped.

> ⚠️ **Note:** If you use more than middleware, keep in mind that **ORDER MATTERS**. The correct order is UsePaging > UseProjection > UseFiltering > UseSorting

**Code First**

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.GetPersons(default)).UseProjection();
    }
}

public class Query
{
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository) =>
        repository.GetPersons();
}
```

**Pure Code First**

The field descriptor attribute `[UseProjection]` does apply the extension method `UseProjection()` on the field descriptor.

```csharp
public class Query
{
    [UseSorting]
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository)
    {
        repository.GetPersons();
    }
}
```

# FirstOrDefault / SingleOrDefault

If you want to limit the response to a single result, you would have to declare a resolver.
Without returning an `IQueryable<>` you lose the ability to use filtering.

There are two extensions you can use to leverage `collection.FirstOrDefault()` and `.collection.SingleOrDefault()` to
the GraphQL layer. The extensions will rewrite the response type to the element type of the collection apply the behavior.

```csharp
    public class Query
    {
        [UseFirstOrDefault]
        [UseProjection]
        [UseFiltering]
        public IQueryable<User> GetUsers([ScopedService] SomeDbContext someDbContext)
        {
            return someDbContext.Users;
        }
    }
```

```sdl
type Query {
  users(where: UserFilterInput): User
}

type User {
  id: Int!
  name: String!
  email: String!
}
```

# Sorting Filtering and Paging

Projections can be used together with sorting, filtering and paging. The order of the middlewares must be correct.
Make sure to have the following order: UsePaging > UseProjection > UseFiltering > UseSorting

Filtering and sorting can be projected over relations. Projections **cannot** project paging over relations.

```csharp
public class Query
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([ScopedService] SomeDbContext someDbContext)
    {
        return someDbContext.Users;
    }
}

public class User
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    [UseFiltering]
    [UseSorting]
    public ICollection<Address> Addresses { get; set; }
}
```

```graphql
{
  users(
    where: { name: { eq: "ChilliCream" } }
    order: [{ name: DESC }, { email: DESC }]
  ) {
    nodes {
      email
      addresses(where: { street: { eq: "Sesame Street" } }) {
        street
      }
    }
    pageInfo {
      endCursor
      hasNextPage
      hasPreviousPage
      startCursor
    }
  }
}
```

```sql
SELECT "t"."Email", "t"."Id", "a"."Street", "a"."Id"
FROM (
    SELECT "u"."Email", "u"."Id", "u"."Name"
    FROM "Users" AS "u"
    WHERE "u"."Name" = @__p_0
    ORDER BY "u"."Name" DESC, "u"."Email" DESC
    LIMIT @__p_1
) AS "t"
LEFT JOIN "Address" AS "a" ON "t"."Id" = "a"."UserId"
ORDER BY "t"."Name" DESC, "t"."Email" DESC, "t"."Id", "a"."Id"
```

# Always Project Fields

Resolvers on types often access data of the parent. e.g. uses the `Email` member of the parent to fetch some
related data from another service. With projections, this resolver could only work when the user also queries
for the `email` field. To ensure a field is always projected you have to use `IsProjected(true)`.

**Code First**

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(
        IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Feild(x => x.Email).IsProjected(true);
        descriptor.Field("messages")
            .Type<MessageType>()
            .Resolver(
                async ctx =>
                {
                    var dataloader = ctx.DataLoader<MessageDataLoader>();
                    var mail = ctx.Parent<User>().Email;
                    return await dataloader.LoadAsync(
                        mail,
                        ctx.RequestAborted);
                });
    }
}
```

**Pure Code First**

The field descriptor attribute `[IsProjected]` does apply the extension method `IsProjected()` on the field descriptor.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    [IsProjected(true)]
    public string Email { get; set; }
    public Address Address { get; set; }
}

[ExtendObjectType(nameof(User))]
public class UserTypeExtension
{
    public Message GetMessagesAsync(
        [Parent] User user,
        MessageDataLoader dataloader,
        CancellationToken cancellationToken) =>
        dataloader.LoadAsync(user.Email, cancellationToken);
}
```

```graphql
{
  users {
    address {
      street
    }
  }
}
```

```sql
SELECT "u"."Email", "a"."Id" IS NOT NULL, "a"."Street"
FROM "Users" AS "u"
LEFT JOIN "Address" AS "a" ON "u"."AddressId" = "a"."Id"
```

# Exclude fields

If a projected field is requested, the whole subtree is processed. Sometimes you want to opt out of projections.
The projections middleware skips a field in two cases. Either the visitor encounters a fields that is a `UseProjection` field
itself, or it defines `IsProjected(false)`.

**Code First**

```csharp
public class UserType : ObjectType<User>
{
    protected override void Configure(
        IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.Email).IsProjected(false);
    }
}


public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
}
```

**Pure Code First**

The field descriptor attribute `[UseProjection]` does apply the extension method `UseProjection()` on the field descriptor.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    [IsProjected(false)]
    public string Email { get; set; }
    public Address Address { get; set; }
}
```

```graphql
{
  users {
    email
    address {
      street
    }
  }
}
```

```sql
SELECT "a"."Id" IS NOT NULL, "a"."Street"
FROM "Users" AS "u"
LEFT JOIN "Address" AS "a" ON "u"."AddressId" = "a"."Id"
```
