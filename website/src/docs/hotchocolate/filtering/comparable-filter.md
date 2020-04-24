### Comparable Filter

In this example, we look at the filter configuration of a comparable filter.

A comparable filter is generated for all values that implement IComparable except string and boolean.
e.g. `csharp±enum`, `csharp±int`, `csharp±DateTime`...

As an example we will use the following model:

```csharp
public class User
{
    public int LoggingCount {get;set;}
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
  loggingCount: Int
}

input UserFilter {
  loggingCount: Int
  loggingCount_gt: Int
  loggingCount_gte: Int
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  loggingCount_not: Int
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

## ComparableOperationDescriptor

The example above showed that configuring the operations is optional.
If you want to have access to the actual field input types or allow only a subset of comparable filters for a given property, you can configure the operation over the `IFilterInputTypeDescriptor<User>`

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
            .AllowGreaterThan().And()
            .AllowNotGreaterThan().And()
            .AllowGreaterThanOrEqals().And()
            .AllowNotGreaterThanOrEqals().And()
            .AllowLowerThan().And()
            .AllowNotLowerThan().And()
            .AllowLowerThanOrEqals().And()
            .AllowNotLowerThanOrEqals().And()
            .AllowIn().And()
            .AllowNotIn();
    }
}
```
