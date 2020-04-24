### Boolean Filter

In this example, we look at the filter configuration of Boolean filter.
As an example, we will use the following model:

```csharp
public class User
{
    public bool IsOnline {get;set;}
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
  isOnline: Boolean
}

input UserFilter {
  isOnline: Boolean
  isOnline_not: Boolean
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

## BooleanOperationDescriptor

The example above showed that configuring the operations is optional.
If you want to have access to the actual field input types or allow only a subset of Boolean filters for a given property, you can configure the operation over the `IFilterInputTypeDescriptor<User>`

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Filter(x => x.Name)
            .AllowEquals().And()
            .AllowNotEquals();
    }
}
```
