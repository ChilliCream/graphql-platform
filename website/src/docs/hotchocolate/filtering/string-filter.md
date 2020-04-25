---
title: Filtering - String Filter
---

In this example, we look at the filter configuration of a String filter.
As an example we will use the following model:

```csharp
public class User
{
    public string Name {get;set;}
}

public class Query : ObjectType
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service]UserService users )
      => users.AsQueryable();
}

```

The produced GraphQL SDL will look like the following:

```graphql
type Query {
  users(where: UserFilter): [User]
}

type User {
  name: String
}

input UserFilter {
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  name_not: String
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

## StringOperationDescriptor

The example above showed that configuring the operations is optional.
If you want to have access to the actual field input types or allow only a subset of string filters for a given property, you can configure the operation over the `IFilterInputTypeDescriptor<User>`

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Filter(x => x.Name)
            .AllowEquals().And()
            .AllowNotEquals().And()
            .AllowContains().And()
            .AllowNotContains().And()
            .AllowStartsWith().And()
            .AllowNotStartsWith().And()
            .AllowEndsWith().And()
            .AllowNotEndsWith().And()
            .AllowIn().And()
            .AllowNotIn();
    }
}
```
