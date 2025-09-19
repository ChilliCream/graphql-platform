---
title: Sorting
---

# What is sorting

Ordering results of a query dynamically is a common case. With Hot Chocolate sorting, you can expose a sorting argument that abstracts the complexity of ordering logic.
With little configuration, your GraphQL API has sorting capabilities, which translates to native database queries.
The default sort implementation translates sorting statements to expression trees that are applied to `IQueryable`.
Hot Chocolate by default will inspect your .NET model and infer the possible filter operations from it.
Sorting uses `IQueryable` (`IEnumerable`) by default, but you can also easily customize them to use other interfaces.

The following type would yield the following sorting operation

```csharp
    public class User
    {
        public string Name { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
    }

```

```sdl
type Query {
  users(order: [UserSortInput]): [User]
}

type User {
  name: String!
  address: Address!
}

input AddressSortInput {
  street: SortEnumType
}

input UserSortInput {
  name: SortEnumType
  address: AddressSortInput
}

enum SortEnumType {
  ASC
  DESC
}
```

# Getting started

Sorting is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

To use sorting you need to register it on the schema:

```csharp
services.AddGraphQLServer()
  // Your schema configuration
  .AddSorting();
```

Hot Chocolate will infer the sorting types directly from your .Net Model and then use a Middleware to apply the order to `IQueryable<T>` or `IEnumerable<T>` on execution.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseSorting]
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
        descriptor.Field(f => f.GetUsers(default)).UseSorting();
    }
}

public class Query
{
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support sorting!

</Schema>
</ExampleTabs>

> ⚠️ **Note:** If you use more than one middleware, keep in mind that **ORDER MATTERS**. The correct order is UsePaging > UseProjections > UseFiltering > UseSorting

The type can be sorted using the `order` field in the query:

```graphql
query {
  users(order: [{ name: ASC }]) {
    name
    address {
      street
    }
  }
}
```

Properties of nested objects can be sorted as well:

```graphql
query {
  users(order: [{ address: { street: ASC } }]) {
    name
    address {
      street
    }
  }
}
```

Note that it is possible to sort on a field and then by another field:

```graphql
query {
  users(order: [{ name: ASC }, { address: { street: DESC } }]) {
    name
    address {
      street
    }
  }
}
```

# Customization

Under the hood, sorting is based on top of normal Hot Chocolate input types. You can easily customize them with a very familiar fluent interface. The sorting input types follow the same `descriptor` scheme as you are used to from the normal input types. Just extend the base class `SortInputType<T>` and override the descriptor method.

`ISortInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>`. By default, operations are generated for all fields of the type.
Members that are collections are skipped because you cannot order based on lists.
If you do want to specify the sorting types by yourself, you can change this behavior with `BindFields`, `BindFieldsExplicitly`, or `BindFieldsImplicitly`.
When fields are bound implicitly, meaning sorting is added for all valid properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
It is also possible to customize the GraphQL field of the operation further. You can change the name or add a description or directive.

```csharp
public class UserSortType : SortInputType<User>
{
    protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name).Name("custom_name");
    }
}
```

If you want to change the sorting operations on a field, you need to declare your own operation enum type.

```csharp {7}
public class UserSortType : SortInputType<User>
{
    protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name).Type<AscOnlySortEnumType>();
    }
}

public class AscOnlySortEnumType : DefaultSortEnumType
{
    protected override void Configure(ISortEnumTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultSortOperations.Ascending);
    }
}

```

```sdl
type Query {
  users(order: [UserSortInput]): [User]
}

type User {
  name: String!
  address: Address!
}

input UserSortInput {
  name: AscOnlySortEnumType
}

enum AscOnlySortEnumType {
  ASC
}
```

To apply this sorting type, we just have to provide it to the `UseSorting` extension method as the generic type argument.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [UseSorting(typeof(UserSortType))]
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
        descriptor.Field(f => f.GetUsers(default)).UseSorting<UserSortType>();
    }
}

public class Query
{
    public IQueryable<User> GetUsers([Service] IUserRepository repository)
        => repository.GetUsers();
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support sorting!

</Schema>
</ExampleTabs>

# Sorting Conventions

If you want to change the behavior of sorting globally, you want to create a convention for sorting. The sorting convention comes with a fluent interface that is close to a type descriptor.

## Get Started

To use a sort convention, you have to extend `SortConvention` and override the `Configure` method. Alternatively, you can directly configure the convention over the constructor argument.
You then have to register your custom convention on the schema builder with `AddConvention`.
By default, a new convention is empty. To add the default behavior, you have to add `AddDefaults`.

```csharp
public class CustomConvention
    : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
    }
}

services.AddGraphQLServer()
    .AddConvention<ISortConvention, CustomConvention>();
// or
services.AddGraphQLServer()
    .AddConvention<ISortConvention>(new Convention(x =>
        x.AddDefaults()))
```

Often you just want to extend the default behavior of sorting. If this is the case, you can also use `SortConventionExtension`

```csharp
public class CustomConventionExtension
    : SortConventionExtension
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
      // config
    }
}

services.AddGraphQLServer()
    .AddConvention<ISortConvention, CustomConventionExtension>();
// or
services.AddGraphQLServer()
    .AddConvention<ISortConvention>(new SortConventionExtension(x =>
    {
        // config
    }))
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
  users(example_argument_name: [UserSortInput]): [User]
}
```

## Binding of SortTypes

`SortInputType`'s **cannot** just be registered on the schema. You have to bind them to the runtime type on the convention.

### SortInputType bindings

By default, only the `string` type is bound explicitly. If you want to configure sorting globally, you are free to bind additional types.

**Configuration**

```csharp
public class CustomSortInputType
    : SortInputType<User>
{
    protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.Name("CustomSortInputType");
    }
}
public class CustomConvention
    : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults().BindRuntimeType<User, CustomSortInputType>();
    }
}
```

**Result**

```sdl

type Query {
  users(order: [CustomSortInputType!]): [User]
}

type User {
  name: String!
}

input CustomSortInputType {
  name: SortEnumType
}

enum SortEnumType {
  ASC
  DESC
}
```

### Default bindings

For fields all fields where no explicit binding is found, a default is applied. This default is `DefaultSortEnumType`.
This can be configured with the method `DefaultBinding`.

**Configuration**

```csharp
public class CustomConvention : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults().DefaultBinding<AscOnlySortEnumType>();
    }
}
```

**Result**

```sdl
type Query {
  users(order: [UserSortInput]): [User]
}

type User {
  logonCount: Int!
}

input UserSortInput {
  logonCount: AscOnlySortEnumType
}

enum AscOnlySortEnumType {
  ASC
}
```

## Extend Types

### SortEnumType

When you build extensions for sorting, you may want to modify or extend the `DefaultSortEnumType`.

```csharp
descriptor.ConfigureEnum<DefaultSortEnumType>(
    x => x.Operation(CustomOperations.NULL_FIRST).Name("NULL_FIRST));
```

```sdl
enum SortEnumType {
  ASC
  DESC
  NULL_FIRST
}
```

### SortType

In case you want to change a specific sort type, you can do this too.
You can use `Configure<TSortType>()` to alter the configuration of a type.

```csharp
descriptor.Configure<CustomSortInputType>(
    x => x.Description("This is my custom description"));
```

```sdl
"This is my customer description"
input CustomSortInputType {
  name: SortEnumType
}
```
