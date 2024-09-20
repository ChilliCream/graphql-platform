---
title: Filtering
---

With Hot Chocolate filters, you can expose complex filter objects through your GraphQL API that translates to native database queries. The default filter implementation translates filters to expression trees that are applied to `IQueryable`.
Hot Chocolate by default will inspect your .NET model and infer the possible filter operations from it.
Filters use `IQueryable` (`IEnumerable`) by default, but you can also easily customize them to use other interfaces.

The following type would yield the following filter operations:

```csharp
public class Foo
{
    public string Bar { get; set; }
}
```

```sdl
input FooFilterInput {
  and: [FooFilterInput!]
  or: [FooFilterInput!]
  name: StringOperationFilterInput
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String]
  nin: [String]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}
```

# Getting started

Filtering is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

To use filtering you need to register it on the schema:

```csharp
services.AddGraphQLServer()
  // Your schema configuration
  .AddFiltering();
```

Hot Chocolate will infer the filters directly from your .Net Model and then use a Middleware to apply filters to `IQueryable<T>` or `IEnumerable<T>` on execution.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

</Implementation>
<Code>

```csharp
public class Query
{
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetUsers(default))
            .Type<ListType<NonNullType<UserType>>>()
            .UseFiltering();
    }
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support filtering!

</Schema>
</ExampleTabs>

> ⚠️ **Note:** If you use more than one middleware, keep in mind that **ORDER MATTERS**. The correct order is UsePaging > UseProjections > UseFiltering > UseSorting

# Customization

Under the hood, filtering is based on top of normal Hot Chocolate input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>`. By default filters for all fields of the type are generated.
If you do want to specify the filters by yourself you can change this behavior with `BindFields`, `BindFieldsExplicitly` or `BindFieldsImplicitly`.
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
It is also possible to customize the GraphQL field of the operation further. You can change the name, add a description or directive.

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name).Name("custom_name");
    }
}
```

If you want to limit the operations on a field, you need to declare you own operation type.
Given you want to only allow `eq` and `neq` on a string field, this could look like this

```csharp {7}
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name).Type<CustomStringFilterType>();
    }
}

public class CustomerOperationFilterInput : StringOperationFilterInput
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
    }
}
```

```sdl
input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  name: CustomerOperationFilterInput
}

input CustomerOperationFilterInput {
  and: [CustomerOperationFilterInput!]
  or: [CustomerOperationFilterInput!]
  eq: String
  neq: String
}
```

To apply this filter type we just have to provide it to the `UseFiltering` extension method with as the generic type argument.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseFiltering(typeof(UserFilterType))]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(f => f.GetUsers(default))
            .Type<ListType<NonNullType<UserType>>>();
            .UseFiltering<UserFilterType>()
    }
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support filtering!

</Schema>
</ExampleTabs>

# "and" / "or" Filter

There are two built in fields.

- `and`: Every condition has to be valid
- `or` : At least one condition has to be valid

Example:

```graphql
query {
  posts(
    first: 5
    where: {
      or: [{ title: { contains: "Doe" } }, { title: { contains: "John" } }]
    }
  ) {
    edges {
      node {
        id
        title
      }
    }
  }
}
```

**⚠️ `or` does not work when you use it like this:**

```graphql
query {
  posts(
    first: 5
    where: { title: { contains: "John", or: { title: { contains: "Doe" } } } }
  ) {
    edges {
      node {
        id
        title
      }
    }
  }
}
```

In this case the filters are applied like `title.Contains("John") && title.Contains("Doe")` rather than `title.Contains("John") || title.Contains("Doe")` how you probably intended it.

## Removing "and" / "or"

If you do not want to expose `and` and `or` you can remove these fields with the descriptor API:

```csharp
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.AllowAnd(false).AllowOr(false);
    }
}
```

# Filter Types

## Boolean Filter

Defined the filter operations of a `bool` field.

```csharp
public class User
{
    public bool IsOnline { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input BooleanOperationFilterInput {
  eq: Boolean
  neq: Boolean
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  isOnline: BooleanOperationFilterInput
}
```

## Comparable Filter

Defines filters for `IComparable`s like: `bool`, `byte`, `shot`, `int`, `long`, `float`, `double` `decimal`, `Guid`, `DateTime`, `DateTimeOffset` and `TimeSpan`

```csharp
public class User
{
    public int LoginAttempts { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input ComparableOperationInt32FilterInput {
  eq: Int
  neq: Int
  in: [Int!]
  nin: [Int!]
  gt: Int
  ngt: Int
  gte: Int
  ngte: Int
  lt: Int
  nlt: Int
  lte: Int
  nlte: Int
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  loginAttempts: ComparableOperationInt32FilterInput
}
```

## String Filter

Defines filters for `string`

```csharp
public class User
{
    public string Name { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String]
  nin: [String]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  name: StringOperationFilterInput
}
```

## Enum Filter

Defines filters for C# enums

```csharp
public enum Role {
  Default,
  Moderator,
  Admin
}

public class User
{
    public Role Role { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input RoleOperationFilterInput {
  eq: Role
  neq: Role
  in: [Role!]
  nin: [Role!]
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  kind: RoleOperationFilterInput
}
```

## Object Filter

An object filter is generated for all nested objects. The object filter can also be used to filter over database relations.
For each nested object, filters are generated.

```csharp
public class User
{
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }

    public bool IsPrimary { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input AddressFilterInput {
  and: [AddressFilterInput!]
  or: [AddressFilterInput!]
  street: StringOperationFilterInput
  isPrimary: BooleanOperationFilterInput
}

input BooleanOperationFilterInput {
  eq: Boolean
  neq: Boolean
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String]
  nin: [String]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  address: AddressFilterInput
}
```

## List Filter

List filters are generated for all nested enumerations.

```csharp
public class User
{
    public string[] Roles { get; set; }

    public IEnumerable<Address> Addresses { get; set; }
}

public class Address
{
    public string Street { get; set; }

    public bool IsPrimary { get; set; }
}

public class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input AddressFilterInput {
  and: [AddressFilterInput!]
  or: [AddressFilterInput!]
  street: StringOperationFilterInput
  isPrimary: BooleanOperationFilterInput
}

input BooleanOperationFilterInput {
  eq: Boolean
  neq: Boolean
}

input ListAddressFilterInput {
  all: AddressFilterInput
  none: AddressFilterInput
  some: AddressFilterInput
  any: Boolean
}

input ListStringOperationFilterInput {
  all: StringOperationFilterInput
  none: StringOperationFilterInput
  some: StringOperationFilterInput
  any: Boolean
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String]
  nin: [String]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  roles: ListStringOperationFilterInput
  addresses: ListAddressFilterInput
}
```

# Filter Conventions

If you want to change the behavior filtering globally, you want to create a convention for your filters. The filter convention comes with a fluent interface that is close to a type descriptor.

## Get Started

To use a filter convention you can extend `FilterConvention` and override the `Configure` method. Alternatively, you can directly configure the convention over the constructor argument.
You then have to register your custom convention on the schema builder with `AddConvention`.
By default a new convention is empty. To add the default behavior you have to add `AddDefaults`.

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
    }
}

services.AddGraphQLServer()
    .AddConvention<IFilterConvention, CustomConvention>();
// or
services.AddGraphQLServer()
    .AddConvention<IFilterConvention>(new FilterConvention(x =>
        x.AddDefaults()))
```

Often you just want to extend the default behavior of filtering. If this is the case, you can also use `FilterConventionExtension`

```csharp
public class CustomConventionExtension : FilterConventionExtension
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        // config
    }
}

services.AddGraphQLServer()
    .AddConvention<IFilterConvention, CustomConventionExtension>();
// or
services.AddGraphQLServer()
    .AddConvention<IFilterConvention>(new FilterConventionExtension(x =>
    {
        // config
    }));
```

## Argument Name

With the convention descriptor, you can easily change the argument name of the `FilterInputType`.

**Configuration**

```csharp
descriptor.ArgumentName("example_argument_name");
```

**Result**

```sdl
type Query {
  users(example_argument_name: UserFilter): [User]
}
```

## Binding of FilterTypes

`FilterInputType`'s **cannot** just be registered on the schema. You have to bind them to the runtime type on the convention.

**Configuration**

```csharp
public class UserFilterInput : FilterInputType<User>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.Name).Description("This is the name");
    }
}

public class CustomStringOperationFilterInput : StringOperationFilterInput
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor
            .Operation(DefaultFilterOperations.Equals)
            .Type<StringType>();
        descriptor
            .Operation(DefaultFilterOperations.NotEquals)
            .Type<StringType>();
    }
}

descriptor.BindRuntimeType<string, CustomStringOperationFilterInput >();
descriptor.BindRuntimeType<User, UserFilterInput>();
```

**Result**

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

type User {
  name: String!
}

input CustomStringOperationFilterInput {
  and: [CustomStringOperationFilterInput!]
  or: [CustomStringOperationFilterInput!]
  eq: String
  neq: String
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  "This is the name"
  name: CustomStringOperationFilterInput
}
```

## Extend FilterTypes

Instead of defining your own operation type, you can also just change the configuration of the built
in ones.
You can use `Configure<TFilterType>()` to alter the configuration of a type.

```csharp
  descriptor.Configure<StringOperationFilterInput>(
    x => x.Operation(DefaultFilterOperations.Equals).Description("Equals"))
```

```sdl
input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  "Equals"
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String]
  nin: [String]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}
```
