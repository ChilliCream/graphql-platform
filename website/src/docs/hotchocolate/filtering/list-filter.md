---
title: Filtering - List filter
---
## List Filter

In this example, we look at the filter configuration of a list filter.

List filters are generated for all nested IEnumerables. The array filter addresses scalars and object values differently.
In the case of a scalar, an object type is generated to address the different operations of this scalar. If a list of strings is filtered, an object type is created to address all string operations.
In case the list contains a complex object, an object filter for this object is generated.

A list filter is generated for all properties that implement IEnumerable.
e.g. `csharp±string[]`, `csharp±List<Foo>`, `csharp±IEnumerable<Bar>`...

As an example we will use the following model:

```csharp
public class User
{
    public string[] Roles {get;set;}

    public IEnumerable<Address> Addresses {get;set;}
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
  addresses: [Address]
  roles: [String]
}

type Address {
  isPrimary: Boolean
  street: String
}

input UserFilter {
  addresses_some: AddressFilter
  addresses_all: AddressFilter
  addresses_none: AddressFilter
  addresses_any: Boolean
  roles_some: ISingleFilterOfStringFilter
  roles_all: ISingleFilterOfStringFilter
  roles_none: ISingleFilterOfStringFilter
  roles_any: Boolean
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

input ISingleFilterOfStringFilter {
  AND: [ISingleFilterOfStringFilter!]
  element: String
  element_contains: String
  element_ends_with: String
  element_in: [String]
  element_not: String
  element_not_contains: String
  element_not_ends_with: String
  element_not_in: [String]
  element_not_starts_with: String
  element_starts_with: String
  OR: [ISingleFilterOfStringFilter!]
}
```

### ArrayOperationDescriptor

The example above showed that configuring the operations is optional.
If you want to have access to the actual field input types or allow only a subset of array filters for a given property, you can configure the operation over the `IFilterInputTypeDescriptor<User>`

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.List(x => x.Addresses)
            .AllowSome().And()
            .AlloAny().And()
            .AllowAll().And()
            .AllowNone();
        descriptor.List(x => x.Roles)
            .AllowSome().And()
            .AlloAny().And()
            .AllowAll().And()
            .AllowNone();
    }
}
```
