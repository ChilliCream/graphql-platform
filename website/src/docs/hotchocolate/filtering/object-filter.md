---
title: Filtering - Object Filter
---

In this example, we look at the filter configuration of an object filter.

An object filter is generated for all nested objects. The object filter can also be used to filter over database relations.
For each nested object, filters are generated.

As an example we will use the following model:

```csharp
public class User
{
    public Address Address {get;set;}
}

public class Address
{
    public string Street {get;set;}

    public bool IsPrimary {get;set;}
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
  address: Address
}

type Address {
  isPrimary: Boolean
  street: String
}

input UserFilter {
  address: AddressFilter
  AND: [UserFilter!]
  OR: [UserFilter!]
}

input AddressFilter {
  is_primary: Boolean
  is_primary_not: Boolean
  street: String
  street_contains: String
  street_ends_with: String
  street_in: [String]
  street_not: String
  street_not_contains: String
  street_not_ends_with: String
  street_not_in: [String]
  street_not_starts_with: String
  street_starts_with: String
  AND: [AddressFilter!]
  OR: [AddressFilter!]
}
```

### ObjectOperationDescriptor

The example above showed that configuring the operations is optional.
If you want to have access to the actual field input types or allow only a subset of comparable filters for a given property, you can configure the operation over the `IFilterInputTypeDescriptor<User>`

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Object(x => x.Address);
    }
}
```

**Configuring a custom nested filter type:**

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Object(x => x.Address).AllowObject<AddressFilterType>();
    }
}

public class AddressFilterType : FilterInputType<Address>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<Address> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Filter(x => x.IsPrimary);
    }
}

// Or Inline

public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Object(x => x.Address)
          .AllowObject(
            y => y.BindFieldsExplicitly().Filter(z => z.IsPrimary));
    }
}


```
