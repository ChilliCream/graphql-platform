---
title: Filtering
---

# What is filtering

With _Hot Chocolate_ filters, you can expose complex filter objects through your GraphQL API that translates to native database queries. The default filter implementation translates filters to expression trees that are applied to `IQueryable`.
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

Filtering is part of the `HotChocolate.Data` package. You can add the dependency with the `dotnet` cli

```bash
  dotnet add package HotChocolate.Data
```

To use filtering you need to register it on the schema:

```csharp
services.AddGraphQLServer()
  // Your schmea configuration
  .AddFiltering();
```

Hot Chocolate will infer the filters directly from your .Net Model and then use a Middleware to apply filters to `IQueryable<T>` or `IEnumerable<T>` on execution.

> ⚠️ **Note:** If you use more than middleware, keep in mind that **ORDER MATTERS**. The correct order is UsePaging > UseProjections > UseFiltering > UseSorting

**Code First**

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.GetPersons(default))
            .Type<ListType<NonNullType<PersonType>>>()
            .UseFiltering();
    }
}

public class Query
{
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository) =>
        repository.GetPersons();
}
```

**Pure Code First**

The field descriptor attribute `[UseFiltering]` does apply the extension method `UseFiltering()` on the field descriptor.

```csharp
public class Query
{
    [UseFiltering]
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository)
    {
        repository.GetPersons();
    }
}
```

# Customization

Under the hood, filtering is based on top of normal Hot Chocolate input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>`. By default filters for all fields of the type are generated.
If you do want to specify the filters by yourself you can change this behavior with `BindFields`, `BindFieldsExplicitly` or `BindFieldsImplicitly`.
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
It is also possible to customize the GraphQL field of the operation further. You can change the name, add a description or directive.

```csharp
public class PersonFilterType
    : FilterInputType<Person>
{
    protected override void Configure(IFilterInputTypeDescriptor<Person> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Name).Name("custom_name");
    }
}
```

If you want to limit the operations on a field, you need to declare you own operation type.
Given you want to only allow `eq` and `neq` on a string field, this could look like this

```csharp{7}
public class PersonFilterType
    : FilterInputType<Person>
{
    protected override void Configure(IFilterInputTypeDescriptor<Person> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Name).Type<CustomStringFilterType>();
    }
}

public class CustomerOperationFilterInput : StringOperationFilterInput
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals);
        descriptor.Operation(DefaultFilterOperations.NotEquals);
    }
}
```

```sdl
input PersonFilterInput {
  and: [PersonFilterInput!]
  or: [PersonFilterInput!]
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

**Code First**

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.GetPerson(default))
            .Type<ListType<NonNullType<PersonType>>>();
            .UseFiltering<PersonFilterType>()
    }
}
```

**Pure Code First**

```csharp
public class Query
{
    [UseFiltering(typeof(PersonFilterType))]
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository)
    {
        repository.GetPersons();
    }
}
```

# Filter Types

## Boolean Filter

Defined the filter operations of a `bool` field.

```csharp
public class User
{
    public bool IsOnline {get;set;}
}

public class Query : ObjectType
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
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

Defines filters for `IComparables` like: `bool`, `byte`, `shot`, `int`, `long`, `float`, `double` `decimal`, `Guid`, `DateTime`, `DateTimeOffset` and `TimeSpan`

```csharp
public class User
{
    public int LoggingCount { get; set;}
}

public class Query : ObjectType
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
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
  loggingCount: ComparableOperationInt32FilterInput
}
```

## String Filter

Defines filters for `string`

```csharp
public class User
{
    public string Name {get;set;}
}

public class Query : ObjectType
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

type User {
  name: String!
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
public enum UserKind {
  Admin,
  Moderator,
  NormalUser
}

public class User
{
    public UserKind kind {get;set;}
}

public class Query : ObjectType
{
    [UseFiltering]
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
}

```

```sdl
type Query {
  users(where: UserFilterInput): [User]
}

input UserKindOperationFilterInput {
  eq: UserKind
  neq: UserKind
  in: [UserKind!]
  nin: [UserKind!]
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  kind: UserKindOperationFilterInput
}
```

## Object Filter

An object filter is generated for all nested objects. The object filter can also be used to filter over database relations.
For each nested object, filters are generated.

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
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
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
    public IQueryable<User> GetUsers([Service]UserService users) =>
      users.AsQueryable();
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
By default a new convention is empty. To add the default behaviour you have to add `AddDefaults`.

```csharp
public class CustomConvention
    : FilterConvention
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
  .AddConvention<IFilterConvention>(new FilterConvention(x => x.AddDefaults()))
```

Often you just want to extend the default behaviour of filtering. If this is the case, you can also use `FilterConventionExtension`

```csharp
public class CustomConventionExtension
    : FilterConventionExtension
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
  .AddConvention<IFilterConvention>(new FilterConventionExtension(x => /*config*/))
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
    protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.Name).Description("This is the name");
    }
}

public class CustomStringOperationFilterInput : StringOperationFilterInput
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
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
