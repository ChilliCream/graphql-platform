---
title: Filtering
---

**What are filters?**

With Hot Chocolate filters, you can expose complex filter objects through your GraphQL API that translates to native database queries.

The default filter implementation translates filters to expression trees and applies these on `IQueryable`.

# Overview

Filters by default work on `IQueryable` but you can also easily customize them to use other interfaces.

Hot Chocolate by default will inspect your .NET model and infer the possible filter operations from it.

The following type would yield the following filter operations:

```csharp
public class Foo
{
    public string Bar { get; set; }
}
```

```graphql
input FooFilter {
  bar: String
  bar_contains: String
  bar_ends_with: String
  bar_in: [String]
  bar_not: String
  bar_not_contains: String
  bar_not_ends_with: String
  bar_not_in: [String]
  bar_not_starts_with: String
  bar_starts_with: String
  AND: [FooFilter!]
  OR: [FooFilter!]
}
```

**So how can we get started with filters?**

Getting started with filters is very easy, especially if you do not want to explicitly define filters or customize anything.

Hot Chocolate will infer the filters directly from your .Net Model and then use a Middleware to apply filters to `IQueryable<T>` or `IEnumerable<T>` on execution.

> ⚠️ **Note:** If you use more than middleware, keep in mind that **ORDER MATTERS**.

> ⚠️ **Note:** Be sure to install the `HotChocolate.Types.Filters` NuGet package.

In the following example, the person resolver returns the `IQueryable` representing the data source. The `IQueryable` represents a not executed database query on which Hot Chocolate can apply filters.

**Code First**

The next thing to note is the `UseFiltering` extension method which adds the filter argument to the field and a middleware that can apply those filters to the `IQueryable`. The execution engine will, in the end, execute the `IQueryable` and fetch the data.

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
    public IQueryable<Person> GetPersons([Service]IPersonRepository repository)
    {
        repository.GetPersons();
    }
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

**Schema First**

> ⚠️ **Note:** Schema first does currently not support filtering!

# Customizing Filters

A `FilterInputType<T>` defines a GraphQL input type, that Hot Chocolate uses for filtering. You can customize these similar to a normal input type. You can change the name of the type; add, remove, or change operations or directive; and configure the binding behavior. To define and customize a filter we must inherit from `FilterInputType<T>` and configure it like any other type by overriding the `Configure` method.

```csharp
public class PersonFilterType
    : FilterInputType<Person>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<Person> descriptor)
    {
        descriptor
            .BindFieldsExplicitly()
            .Filter(t => t.Name)
            .BindOperationsExplicitly()
            .AllowEquals().Name("equals").And()
            .AllowContains().Name("contains").And()
            .AllowIn().Name("in");
    }
}
```

The above filter type defines explicitly which fields allow filtering and what operations these filters allow. Additionally, the filter type changes the name of the equals operation of the filter of the field `Name` to `equals`.

To make use of the configuration in this filter type, you can provide it to the `UseFiltering` extension method as the generic type argument.

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

# Sorting

Like with filter support you can add sorting support to your database queries.

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.GetPerson(default))
            .Type<ListType<NonNullType<PersonType>>>();
            .UseSorting()
    }
}
```

> ⚠️ **Note**: Be sure to install the `HotChocolate.Types.Sorting` NuGet package.

If you want to combine for instance paging, filtering, and sorting make sure that the order is like follows:

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.GetPerson(default))
            .UsePaging<PersonType>()
            .UseFiltering()
            .UseSorting();
    }
}
```

**Why is order important?**

Paging, filtering, and sorting are modular middlewares that form the field resolver pipeline.

The above example forms the following pipeline:

`Paging -> Filtering -> Sorting -> Field Resolver`

The paging middleware will first delegate to the next middleware, which is filtering.

The filtering middleware will also first delegate to the next middleware, which is sorting.

The sorting middleware will again first delegate to the next middleware, which is the actual field resolver.

The field resolver will call `GetPerson` which returns in this example an `IQueryable<Person>`. The queryable represents a not yet executed database query.

After the resolver has been executed and puts its result onto the middleware context the sorting middleware will apply for the sort order on the query.

After the sorting middleware has been executed and updated the result on the middleware context the filtering middleware will apply its filters on the queryable and updates the result on the middleware context.

After the paging middleware has been executed and updated the result on the middleware context the paging middleware will slice the data and execute the queryable which will then actually pull in data from the data source.

So, if we, for instance, applied paging as our last middleware the data set would have been sliced first and then filtered which in most cases is not what we actually want.

# Filter & Operations Kinds

You can break down filtering into different kinds of filters that then have different operations.
The filter kind is bound to the type. A string is fundamentally something different than an array or an object.
Each filter kind has different operations that you can apply to it. Some operations are unique to a filter and some operations are shared across multiple filter
e.g. A string filter has string specific operations like `Contains` or `EndsWith` but still shares the operations `Equals` and `NotEquals` with the boolean filter.

## Filter Kinds

Hot Chocolate knows following filter kinds

| Kind       | Operations                                                                                                                                                           |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| String     | Equals, In, EndsWith, StartsWith, Contains, NotEquals, NotIn, NotEndsWith, NotStartsWith, NotContains                                                                |
| Bool       | Equals, NotEquals                                                                                                                                                    |
| Object     | Equals                                                                                                                                                               |
| Array      | Some, Any, All, None                                                                                                                                                 |
| Comparable | Equals, In, GreaterThan, GreaterThanOrEqual, LowerThan, LowerThanOrEqual, NotEquals, NotIn, NotGreaterThan, NotGreaterThanOrEqual, NotLowerThan, NotLowerThanOrEqual |

## Operations Kinds

Hot Chocolate knows following operation kinds

| Kind                   | Operations                                                                                            |
| ---------------------- | ----------------------------------------------------------------------------------------------------- |
| Equals                 | Compares the equality of input value and property value                                               |
| NotEquals              | negation of Equals                                                                                    |
| In                     | Checks if the property value is contained in a given list of input values                             |
| NotIn                  | negation of In                                                                                        |
| GreaterThan            | checks if the input value is greater than the property value                                          |
| NotGreaterThan         | negation of GreaterThan                                                                               |
| GreaterThanOrEquals    | checks if the input value is greater than or equal to the property value                              |
| NotGreaterThanOrEquals | negation of GreaterThanOrEquals                                                                       |
| LowerThan              | checks if the input value is lower than the property value                                            |
| NotLowerThan           | negation of LowerThan                                                                                 |
| LowerThanOrEquals      | checks if the input value is lower than or equal to the property value                                |
| NotLowerThanOrEquals   | negation of LowerThanOrEquals                                                                         |
| EndsWith               | checks if the property value ends with the input value                                                |
| NotEndsWith            | negation of EndsWith                                                                                  |
| StartsWith             | checks if the property value starts with the input value                                              |
| NotStartsWith          | negation of StartsWith                                                                                |
| Contains               | checks if the input value is contained in the property value                                          |
| NotContains            | negation of Contains                                                                                  |
| Some                   | checks if at least one element in the collection exists                                               |
| Some                   | checks if at least one element of the property value meets the condition provided by the input value  |
| None                   | checks if no element of the property value meets the condition provided by the input value            |
| All                    | checks if all least one element of the property value meets the condition provided by the input value |

## Boolean Filter

In this example, we look at the filter configuration of a Boolean filter.
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

### Boolean Operation Descriptor

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

## Comparable Filter

In this example, we look at the filter configuration of a comparable filter.

A comparable filter is generated for all values that implement IComparable except string and boolean.
e.g. `csharp±enum`, `csharp±int`, `csharp±DateTime`...

As an example, we will use the following model:

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

### Comparable Operation Descriptor

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

## String Filter

In this example, we look at the filter configuration of a String filter.
As an example, we will use the following model:

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

### String Operation Descriptor

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

## Object Filter

In this example, we look at the filter configuration of an object filter.

Hot Chocolate generated object filters for all objects. Since Version 11, Hot Chocolate also generates filter types for nested objects. You can also use object filters to filter over database relations.

As an example, we will use the following model:

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

### Object Operation Descriptor

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

// or inline

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

## List Filter

In this example, we look at the filter configuration of a list filter.

Hot Chocolate can also generate filters for IEnumerables. Like object filter, Hot Chocolate generates filters for the whole object tree. List filter addresses scalars and object values differently.
In the case the field is a scalar value, Hot Chocolate creates and object type to address the different operations of this scalar. e.g. If you specify filters for a list of strings, Hot Chocolate creates an object type that contains all operations of the string filter.
In case the list holds a complex object, it generates an object filter for this object instead.

Hot Chocolate implicitly generates filters for all properties that implement `IEnumerable`.
e.g. `csharp±string[]`, `csharp±List<Foo>`, `csharp±IEnumerable<Bar>`...

As an example, we will use the following model:

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
  element_not_contains: String46
  element_not_ends_with: String
  element_not_in: [String]
  element_not_starts_with: String
  element_starts_with: String
  OR: [ISingleFilterOfStringFilter!]
}
```

### Array Operation Descriptor

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

# Naming Conventions

\_Hot Chocolate already provides two naming schemes for filters. If you would like to define your own naming scheme or extend existing ones have a look at the documentation of TODO:Link-Filtering

## Snake Case

**Configuration**
You can configure the Snake Case with the `UseSnakeCase` extension method convention on the `IFilterConventionDescriptor`

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.UseSnakeCase()
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => x.UseSnakeCase())
```

```graphql
input FooBarFilter {
  AND: [FooBarFilter!]
  nested: String
  nested_contains: String
  nested_ends_with: String
  nested_in: [String]
  nested_not: String
  nested_not_contains: String
  nested_not_ends_with: String
**Change the name of an operation**
  nested_not_in: [String]
  nested_not_starts_with: String
  nested_starts_with: String
  OR: [FooBarFilter!]
}

input FooFilter {
  AND: [FooFilter!]
  bool: Boolean
  bool_not: Boolean
  comparable: Short
  comparableEnumerable_all: ISingleFilterOfInt16Filter
  comparableEnumerable_any: Boolean
  comparableEnumerable_none: ISingleFilterOfInt16Filter
  comparableEnumerable_some: ISingleFilterOfInt16Filter
  comparable_gt: Short
  comparable_gte: Short
  comparable_in: [Short!]
  comparable_lt: Short
  comparable_lte: Short
  comparable_not: Short
  comparable_not_gt: Short
  comparable_not_gte: Short
  comparable_not_in: [Short!]
  comparable_not_lt: Short
  comparable_not_lte: Short
  object: FooBarFilter
  OR: [FooFilter!]
}

input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  element: Short
  element_gt: Short
  element_gte: Short
  element_in: [Short!]
  element_lt: Short
  element_lte: Short
  element_not: Short
  element_not_gt: Short
  element_not_gte: Short
  element_not_in: [Short!]
  element_not_lt: Short
  element_not_lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```

## Pascal Case

**Configuration**
You can configure the Pascal Case with the `UsePascalCase` extension method convention on the `IFilterConventionDescriptor`

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.UsePascalCase()
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => x.UsePascalCase())
```

```graphql
input FooBarFilter {
  AND: [FooBarFilter!]
  Nested: String
  Nested_Contains: String
  Nested_EndsWith: String
  Nested_In: [String]
  Nested_Not: String
  Nested_Not_Contains: String
  Nested_Not_EndsWith: String
  Nested_Not_In: [String]
  Nested_Not_StartsWith: String
  Nested_StartsWith: String
  OR: [FooBarFilter!]
}

input FooFilter {
  AND: [FooFilter!]
  Bool: Boolean
  Bool_Not: Boolean
  Comparable: Short
  ComparableEnumerable_All: ISingleFilterOfInt16Filter
  ComparableEnumerable_Any: Boolean
  ComparableEnumerable_None: ISingleFilterOfInt16Filter
  ComparableEnumerable_Some: ISingleFilterOfInt16Filter
  Comparable_Gt: Short
  Comparable_Gte: Short
  Comparable_In: [Short!]
  Comparable_Lt: Short
  Comparable_Lte: Short
  Comparable_Not: Short
  Comparable_Not_Gt: Short
  Comparable_Not_Gte: Short
  Comparable_Not_In: [Short!]
  Comparable_Not_Lt: Short
  Comparable_Not_Lte: Short
  Object: FooBarFilter
  OR: [FooFilter!]
}

input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  Element: Short
  Element_Gt: Short
  Element_Gte: Short
  Element_In: [Short!]
  Element_Lt: Short
  Element_Lte: Short
  Element_Not_Gt: Short
  Element_Not: Short
  Element_Not_Gte: Short
  Element_Not_In: [Short!]
  Element_Not_Lt: Short
  Element_Not_Lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```

# Customizing Filter

Hot Chocolate provides different APIs to customize filtering. You can write custom filter input types, customize the inference behavior of .NET Objects, customize the generated expression, or create a custom visitor, and attach your exotic database.

**As this can be a bit overwhelming the following questionnaire might help:**

|                                                                                                                                         |                                 |
| --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| _You do not want all the generated filters and only allow a specific set of filters in a specific case?_                                | Custom&nbsp;FilterInputType     |
| _You want to change the name of a field or a whole type?_                                                                               | Custom&nbsp;FilterInputType     |
| _You want to change the name of the `where` argument?_                                                                                  | Filter Conventions ArgumentName |
| _You want to configure how *Hot Chocolate* generates the name and the description of filters in globally? e.g. `PascalCaseFilterType`?_ | Filter&nbsp;Conventions         |
| _You want to configure what the different types of filters are allowed globally?_                                                       | Filter&nbsp;Conventions         |
| _Your database provider does not support certain operations of `IQueryable`_                                                            | Filter&nbsp;Conventions         |
| _You want to change the naming of a specific lar filter type? e.g._ `foo_contains` _should be_ `foo_like`                               | Filter&nbsp;Conventions         |
| _You want to customize the expression a filter is generating: e.g._ `_equals` _should not be case sensitive?_                           | Expression&nbsp;Visitor&nbsp;   |
| _You want to create your own filter types with custom parameters and custom expressions? e.g. GeoJson?_                                 | Filter&nbsp;Conventions         |
| _You have a database client that does not support `IQueryable` and wants to generate filters for it?_                                   | Custom&nbsp;Visitor             |

# Custom&nbsp;FilterInputType

Under the hood, filtering is based on top of normal Hot Chocolate input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal filter input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

```csharp
public class User
{
    public string Name {get; set; }

    public string LastName {get; set; }
}

public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {

    }
}
```

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>` and adds the configuration interface for the filters. By default, Hot Chocolate generates filters for all properties of the type.
If you do want to specify the filters by yourself you can change this behavior with `BindFields`, `BindFieldsExplicitly` or `BindFieldsImplicitly`.

```csharp
public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {
       descriptor.BindFieldsExplicitly();
       descriptor.Filter(x => x.Name);
    }
}
```

```graphql
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

To add or customize a filter you must use `Filter(x => x.Foo)` for scalars `List(x => x.Bar)` for lists and `Object(x => x.Baz)` for nested objects.
These methods will return fluent interfaces to configure the filter for the selected field.

A field has different filter operations that you can configure. You will find more about filter types and filter operations here TODO:Link
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
Operations on fields can again be bound implicitly or explicitly. By default, Hot Chocolate generates operations for all fields of the type.
If you do want to specify the operations by yourself you can change this behavior with `BindFilters`, `BindFiltersExplicitly` or `BindFiltersImplicitly`.

It is also possible to customize the GraphQL field of the operation further. You can change the name, add a description or directive.

```csharp
public class UserFilterType
    : FilterInputType<User>
{
    protected override void Configure( IFilterInputTypeDescriptor<User> descriptor) {
       // descriptor.BindFieldsImplicitly(); <- is already the default
       descriptor.Filter(x => x.Name)
          .BindFilterExplicitly()
          .AllowContains()
            .Description("Checks if the provided string is contained in the `Name` of a User")
            .And()
          .AllowEquals()
            .Name("exits_with_name")
            .Directive("name");
       descriptor.Ignore(x => x.Bar);
    }
}
```

```graphql
input UserFilter {
  exits_with_name: String @name
  """
  Checks if the provided string is contained in the `Name` of a User
  """
  name_contains: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

**API Documentation**

| Method                                                                           | Description                                                                                                                                     |
| -------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `csharp±BindFields(BindingBehavior bindingBehavior)`                             | Defines the filter binding behavior. `Explicitly`or `Implicitly`. Default is `Implicitly`                                                       |
| `csharp±BindFieldsExplicitly`                                                    | Defines that all filters have to be specified explicitly. This means that only the filters are applied that are added with `Filter(x => x.Foo)` |
| `csharp±BindFieldsImplicitly`                                                    | The filter type will add filters for all compatible fields.                                                                                     |
| `csharp±Description(string value)`                                               | Adds explanatory text of the `FilterInputType<T>` that can be accessed via introspection.                                                       |
| `csharp±Name(NameString value)`                                                  | Defines the _GraphQL_ name of the `FilterInputType<T>`.                                                                                         |
| `csharp±Ignore( Expression<Func<T, object>> property);`                          | Ignore the specified property.                                                                                                                  |
| `csharp±Filter( Expression<Func<T, string>> property)`                           | Defines a string filter for the selected property.                                                                                              |
| `csharp±Filter( Expression<Func<T, bool>> property)`                             | Defines a bool filter for the selected property.                                                                                                |
| `csharp±Filter( Expression<Func<T, IComparable>> property)`                      | Defines a comparable filter for the selected property.                                                                                          |
| `csharp±Object<TObject>( Expression<Func<T, TObject>> property)`                 | Defines a object filter for the selected property.                                                                                              |
| `csharp±List( Expression<Func<T, IEnumerable<string>>> property)`                | Defines an array string filter for the selected property.                                                                                       |
| `csharp±List( Expression<Func<T, IEnumerable<bool>>> property)`                  | Defines an array bool filter for the selected property.                                                                                         |
| `csharp±List( Expression<Func<T, IEnumerable<IComparable>>> property)`           | Defines an array comarable filter for the selected property.                                                                                    |
| `csharp±Filter<TObject>( Expression<Func<T, IEnumerable<TObject>>> property)`    | Defines an array object filter for the selected property.                                                                                       |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive `directiveInstance` to the type                                                                                                   |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive of type `TDirective` to the type                                                                                                  |
| `csharp±Directive<TDirective>(NameString name, params ArgumentNode[] arguments)` | Add directive of type `TDirective` to the type                                                                                                  |

# Filter Conventions

The customization of filters with `FilterInputTypes<T>` works if you only want to customize one specific filter type.
If you want to change the behavior of all filter types, you want to create a convention for your filters. The filter convention comes with a fluent interface that is close to a type descriptor.
You can see the convention as a configuration object that holds the state that is used by the type system or the execution engine.

## Get Started

To use a filter convention, you can extend `FilterConvention` and override the `Configure` method. Alternatively, you can directly configure the convention over the constructor argument.
You then must register your custom convention on the schema builder with `AddConvention`.

```csharp
public class CustomConvention
    : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor) { }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => /* Config */));
```

## Convention Descriptor Basics

In this section, we will take a look at the basic features of the filter convention.
The documentation will reference often to `descriptor`. Imagine this `descriptor` as the parameter of the Configure method of the filter convention in the following context:

```csharp {5}
public class CustomConvention
    : FilterConvention
{
    protected override void Configure(
        IFilterConventionDescriptor descriptor
    ) { }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
```

<br/>

### Argument Name

With the convention descriptor, you can easily change the argument name of the `FilterInputType`.

**Configuration**

```csharp
descriptor.ArgumentName("example_argument_name");
```

**Result**

```graphql
type Query {
  users(example_argument_name: UserFilter): [User]
}
```

### Change Name of Scalar List Type Element

You can change the name of the element of the list type.

**Configuration**

```csharp
descriptor.ElementName("example_element_name");
```

**Result**

```graphql
input ISingleFilterOfInt16Filter {
  AND: [ISingleFilterOfInt16Filter!]
  example_element_name: Short
  example_element_name_gt: Short
  example_element_name_gte: Short
  example_element_name_in: [Short!]
  example_element_name_lt: Short
  example_element_name_lte: Short
  example_element_name_not: Short
  example_element_name_not_gt: Short
  example_element_name_not_gte: Short
  example_element_name_not_in: [Short!]
  example_element_name_not_lt: Short
  example_element_name_not_lte: Short
  OR: [ISingleFilterOfInt16Filter!]
}
```

### Configure Filter Type Name Globally

You can change the way Hot Chocolate names the types by supplying a delegate.

This delgate must be of the following type:

```csharp
public delegate NameString GetFilterTypeName(
    IDescriptorContext context,
    Type entityType);
```

**Configuration**

```csharp
descriptor.TypeName((context,types) =>
    context.Naming.GetTypeName(entityType, TypeKind.Object) + "Custom");
```

**Result**

```graphql
type Query {
  users(where: UserCustom): [User]
}
```

### Configure Filter Description Globally

To change the way filter types are named, you have to exchange the factory.

You have to provide a delegate of the following type:

```csharp
public delegate string GetFilterTypeDescription(
    IDescriptorContext context,
    Type entityType);
```

**Configuration**

```csharp
descriptor.TypeName((context,types) =>
    context.Naming.GetTypeDescription(entityType, TypeKind.Object); + "Custom");
```

**Result**

```graphql
"""
Custom
"""
input UserFilter {
  AND: [UserFilter!]
  isOnline: Boolean
  isOnline_not: Boolean
  OR: [UserFilter!]
}
```

### Reset Configuration

Hot Chocolate shippes with well-defined defaults. To start from scratch, you need to call `Reset()`first.

**Configuration**

```csharp
descriptor.Reset();
```

**Result**

> **⚠ Note:** You will need to add a complete configuration, otherwise the filter will not work as desired!

## Describe with convention

With the filter convention descriptor, you have full control over what filters are inferred, their names, operations, and a lot more.
The convention provides a familiar interface to the type configuration. We recommended to first take a look at `Filter & Operations` to understand the concept of filters. This will help you understand how the filter configuration works.

Filtering has two core components at its heart. First, you have the inference of filters based on .NET types. The second part is an interceptor that translates the filters to the desired output and applies it to the resolver pipeline. These two parts can (and have to) be configured completely independently. With this separation, it is possible to easily extend the behavior. The descriptor is designed to be extendable by extension methods.

**It's fluent**

Filter conventions are a completely fluent experience. You can write a whole configuration as a chain of method calls.
This provides a very clean interface, but can, on the other hand, get messy quickly. We recommend using indentation to keep the configuration comprehensible.
You can drill up with `And()`.

```csharp
 descriptor.Operation(FilterOperationKind.Equals).Description("has to be equal");
 descriptor.Operation(FilterOperationKind.NotEquals).Description("has not to be equal");
 descriptor.Type(FilterKind.Comparable).Operation(FilterOperationKind.NotEquals).Description("has to be comparable and not equal")


 descriptor
    .Operation(FilterOperationKind.Equals)
        .Description("has to be equal")
        .And()
    .Operation(FilterOperationKind.NotEquals)
        .Description("has not to be equal")
        .And()
    .Type(FilterKind.Comparable)
        .Operation(FilterOperationKind.NotEquals)
            .Description("has to be comparable and not equal")
```

### Configuration of the type system

In this section, we will focus on the generation of the schema. If you are interested in changing how filters translate to the database, you have to look here TODO:Link

#### Configure Filter Operations

There are two ways to configure Operations.

You can configure a default configuration that applies to all operations of this kind. In this case the configuration for `FilterOperationKind.Equals` would be applied to all `FilterKind` that specify this operation.

```csharp
 descriptor.Operation(FilterOperationKind.Equals)
```

If you want to configure a more specific Operation e.g. `FilterOperationKind.Equal` of kind `FilterKind.String`, you can override the default behavior.

```csharp
 descriptor.Type(FilterKind.String).Operation(FilterOperationKind.Equals)
```

The operation descriptor allows you to configure the name, the description or even ignore an operation completely

In this example, we will look at the following input type:

```graphql
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

##### Change the name of an operation

To change the name of an operation you need to specify a delegate of the following type:

```csharp
public delegate NameString CreateFieldName(
    FilterFieldDefintion definition,
    FilterOperationKind kind);
```

**Configuration**

```csharp {1, 6}
 // (A)
 // specifies that all not equals operations should be extended with _nada
 descriptor
    .Operation(FilterOperationKind.NotEquals)
        .Name((def, kind) => def.Name + "_nada" );
 // (B)
 // specifies that the not equals operations should be extended with _niente.
 // this overrides (A)
 descriptor
    .Type(FilterKind.Comparable)
        .Operation(FilterOperationKind.NotEquals)
            .Name((def, kind) => def.Name + "_niente" )
```

**result**

```graphql {8,18}
input UserFilter {
  loggingCount: Int
  loggingCount_gt: Int
  loggingCount_gte: Int
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  loggingCount_niente: Int   <-- (B)
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  name_nada: String  <-- (A)
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

##### Change the description of an operation

In the same way, you can configure names you can also configure the description of operations.
You can either set the description for all operations of this kind or only for a specific one in combination with a filter kind.

**Configuration**

```csharp
 descriptor
    .Operation(FilterOperationKind.Equals)
        .Description("has to be equal")
        .And()
    .Operation(FilterOperationKind.NotEquals)
        .Description("has not to be equal")
        .And()
    .Type(FilterKind.Comparable)
        .Operation(FilterOperationKind.NotEquals)
            .Description("has to be comparable and not equal")
```

**result**

```graphql {2-4,11-14, 20-22,27-29}
input UserFilter {
  """
  has to be equal
  """
  loggingCount: Int
  loggingCount_gt: Int
  loggingCount_gte: Int
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  """
  has to be comparable and not equal
  """
  loggingCount_not: Int
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  """
  has to be equal
  """
  name: String
  name_contains: String
  name_ends_with: String
  name_in: [String]
  """
  has not to be equal
  """
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

##### Hide Operations

Hot Chocolate comes preconfigured with a set of operations. If you like to hide operations globally, you can use `Ignore` for it.
If your database provider does not support certain `IQueryable` methods you can just ignore the operation. Ignored operations do not generate filter input types.

There are multiple ways to ignore an operation:

**Configuration**

```csharp
 descriptor
    .Ignore(FilterOperationKind.Equals)
    .Operation(FilterOperationKind.NotEquals)
        .Ignore()
        .And()
    .Type(FilterKind.Comparable)
          .Operation(FilterOperationKind.GreaterThanOrEqual)
          .Ignore();
```

**result**

```graphql {2,4, 8,14,18}
input UserFilter {
  ↵
  loggingCount_gt: Int
  ↵
  loggingCount_in: [Int!]
  loggingCount_lt: Int
  loggingCount_lte: Int
  ↵
  loggingCount_not_gt: Int
  loggingCount_not_gte: Int
  loggingCount_not_in: [Int!]
  loggingCount_not_lt: Int
  loggingCount_not_lte: Int
  ↵
  name_contains: String
  name_ends_with: String
  name_in: [String]
  ↵
  name_not_contains: String
  name_not_ends_with: String
  name_not_in: [String]
  name_not_starts_with: String
  name_starts_with: String
  AND: [UserFilter!]
  OR: [UserFilter!]
}
```

##### Configure Implicit Filter

The default binding behavior of Hot Chocolate is implicit. Filter types are no exception.
This first may seem like magic, but unfortunately, there is none. It is just code. With `AddImplicitFilter` you can add this pinch of magic to your extension too.
Hot Chocolate creates the filters as it builds the input type. The type iterates over a list of factories sequentially and tries to create a definition for each property. The first factory that can handle the property wins and creates a definition for the filter.

To configure you have to use the following delegate:

```csharp
    public delegate bool TryCreateImplicitFilter(
        IDescriptorContext context,
        Type type,
        PropertyInfo property,
        IFilterConvention filterConventions,
        [NotNullWhen(true)] out FilterFieldDefintion? definition);
```

| parameter           | type                        | description                                                                                               |
| ------------------- | --------------------------- | --------------------------------------------------------------------------------------------------------- |
| _context_           | `IDescriptorContext`        | The context of the type descriptor                                                                        |
| _type_              | `Type`                      | The type of the property. `Nullable<T>` is already unwrapped (typeof(T))                                  |
| _property_          | `PropertyInfo`              | The property                                                                                              |
| _filterConventions_ | `IFilterConvention`         | The instance of the `IFilterContention`.                                                                  |
| _definition_        | `out FilterFieldDefintion?` | The generated definition for the property. Return null if the current factory cannot handle the property. |

If you just want to build your extension for implicit bindings, you can just out a custom `FilterFieldDefinition`.

It makes sense to encapsulate that logic in a FilterFieldDescriptor though. You can reuse this descriptor also for the fluent configuration interface.

**Example**

```csharp
private static bool TryCreateStringFilter(
    IDescriptorContext context,
    Type type,
    PropertyInfo property,
    IFilterConvention filterConventions,
    [NotNullWhen(true)] out FilterFieldDefintion? definition)
{
    if (type == typeof(string))
    {
        var field = new StringFilterFieldDescriptor(context, property, filterConventions);
        definition = field.CreateDefinition();
        return true;
    }

    definition = null;
    return false;
}
```

##### Creating a fluent filter extension

Hot Chocolate provides fluent interfaces for all its APIs. If you want to create an extension that integrates seamlessly with Hot Chocolate it makes sense to also provide fluent interfaces. It makes sense to briefly understand how `Type -> Descriptor -> Definition` work. You can read more about it here //TODO LINK

Here a quick introduction:

_Type_

A type is a description of a GraphQL Type System Object. Hot Chocolate builds types during schema creation. Types specify how a GraphQL Type looks like. It holds, for example, the definition, fields, interfaces, and all life cycle methods. Type do only exist on startup; they do not exist on runtime.

_Type Definition_

Each type has a definition that describes the type. It holds, for example, the name, description, the CLR type and the field definitions. The field definitions describe the fields that are on the type.

_Type Descriptor_

A type descriptor is a fluent interface to describe the type over the definition. The type descriptor does not have access to the type itself. It operates solely on the definition.

In the case of filtering, this works nearly the same. The `FilterInputType` is just an extension of the `InputObjectType`. It also has the same _Definition_. The `FilterInputType` stores `FilterOperationField` on this definition. These are extensions of the normal `InputField`'s and extend it by a `FilterOperationKind`.

With a normal `InputTypeDescriptor` you declare a field by selecting a member. The filter descriptor works a little differently. You declare the `FilterKind` of a member by selecting it and then you declare the operations on this filter. These operations are the input field configuration.

```csharp
InputTypeDescriptor<User> inputDesc;
inputDesc.Field(x => x.Name)
            .Description("This is the name")


FilterInputTypeDescriptor<User> inputDesc;
inputDesc.Filter(x => x.Name).AllowEqual().Description("This is the name")
```

We have a few case studies that will show you how you can change the inference:

1. String "\_like" shows an example of how you can easily add a "\_like" operation to the string filter
2. DateTime "from", "to"
3. NetTopologySuite

> The configuration you see in this case study only shows how you add an operation to an already-existing filter. After this, the job is only half way done. To create a working filter, you must also change the expression visitor. Check the documentation for //TODO: ExpressionVisitor

##### Case Study: String "\_like"

**Situation**
The customer has requested a full-text search of the description field of a product. The product owner has promised the feature to the customer two sprints ago and it has still not been shipped. The UX guru of your company has, slightly under pressure, worked out a solution, and together with the frontend team they have already build a prototype. In the heat of the moment, they did not read the user story correctly and, unfortunately, realized last minute that the current filtering API does not fit their needs. The customer does also has to be able to create complex search queries. `This%Test` should match `This is a Test`. As you come back from lunch a hysterical product owner explains the situation to you. To you, it is immediately clear that this can be easily done by using the SQL `like` operator.

In your codebase you use the `UseFiltering` middleware extensively. In some cases, you also have customized filter types. To cover all possible cases you need

1. Implicit Binding: `[UseFiltering]` should automagically create the "\_like" filter for every string filter
2. Explicity Binding: `desc.Filter(x => x.Description).AllowLike())`
3. Expression Visitor: You want to directly filter on the database. You use EF Core.

**Implicit Binding**
With the conventions, it is easy to add operations on already existing filters. We will first look into the configuration for filter inference and in a second step into the code first extension.

You just need to navigate to the filter you like to modify. `descriptor.Type(FilterKind.String)`. Just add the operation you need with `.Operation(FilterOperationKind.Like)`. The next step is to add factories for the name and the description.

Altogether this looks like this:

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
      descriptor
          .Type(FilterKind.String)
            .Operation(FilterOperationKind.GreaterThanOrEqual)
                .Name((def, kind) => def.Name + "_like" );
                .Description("Full text search. Use % as a placeholder for any symbol");
    }
}
```

**Explicit Binding**
By extending the filter descriptor of the string filter you can add a fluent extension that seamlessly integrated with the Hot Chocolate API.

//TODO: currently there `StringFilterOperationDescriptor` requires `StringFilterFieldDescriptor` instead of `StringFilterFieldDescriptor` and there is no way to `Allow<T>`
//TODO: TYPO ! FilterFieldDefintion
//TODO: Move RewriteType to convention .
//TODO: Move up CreateFieldName

```csharp
public static class StringLikeFilterExtension
{
    public static IStringFilterOperationDescriptor AllowLike(
        IStringFilterFieldDescriptor descriptor)
    {
        return descriptor.Allow(
            FilterOperationKind.ArrayAll,
            (ctx, definition) =>
            {
                var operation = new FilterOperation(
                    typeof(string), FilterOperationKind.ArrayAll, definition.Property);

                return StringFilterOperationDescriptor.New(
                    ctx,
                    descriptor,
                    ctx.GetFilterConvention().CreateFieldName(FilterOperationKind.ArrayAll),
                    ctx.GetFilterConvention().RewriteType(FilterOperationKind.ArrayAll),
                    operation);
            }
        )
    }
}
```

---

##### Case Study: DateTime "from", "to"

**Situation**

1. Implicit Binding: `[UseFiltering]` should automagically create `DateTimeFilter` and the corresponding "\_from" and "\_to".
2. Explicity Binding: `desc.Filter(x => x.OrderedAt).AllowFrom().AllowTo())`
3. Expression Visitor: You want to directly filter on the database. You use EF Core.

**Configuration**

It is slightly more complex to create a custom filter than just modifying existing operations. There are a few different parts that must come together to make this work. Implicit and Explicit Bindings are coming together in this example.

Let's start with the configuration of the convention. By splitting the configuration up into a set of extension methods that can be applied to the convention, it is possible to easily replace sub-components of the extension. e.g. some users might want to use an expression visitor, some others might want to use MognoDB Native.

- `UseDateTimeFilter` adds support for date-time filters and registers the expression visitor for it. Abstraction for `UseDateTimeFilterImplicitly().UseDateTimeExpression()`

- `UseDateTimeFilterImplicitly` only registers the configuration of the schema building part of the extension

- `UseDateTimeExpression` only registers the expression visitor configuration.

With this separation, a user that prefers to use a custom visitor, can just register the types and skip the expression visitor configuration

TODO: UseExpressionVisitor should return expression visitor if it already exists
TODO: Reference Definition from Filter Operation instead of property. This way we could reduce complexity further and improve extensibility

```csharp
public static class DateTimeFilterConventionExtensions
{
    public static IFilterConventionDescriptor UseDateTimeFilter(
        this IFilterConventionDescriptor descriptor) =>
            descriptor.UseDateTimeFilterImplicitly()
                .UseDateTimeFilterExpression();

    public static IFilterConventionDescriptor UseDateTimeFilterImplicitly(
        this IFilterConventionDescriptor descriptor) =>
            descriptor.AddImplicitFilter(TryCreateDateTimeFilter)
                .Type(FilterKind.DateTime)
                .Operation(FilterOperationKind.GreaterThanOrEquals)
                    .Name((def, _) => def.Name + "_from")
                    .Description("")
                    .And()
                .Operation(FilterOperationKind.LowerThanOrEquals)
                    .Name((def, _) => def.Name + "_to")
                    .Description("")
                    .And()
                .And();

    public static IFilterConventionDescriptor UseDateTimeFilterExpression(
        this IFilterConventionDescriptor descriptor) =>
            descriptor.UseExpressionVisitor()
                .Kind(FilterKind.DateTime)
                    .Operation(FilterOperationKind.LowerThanOrEquals)
                        .Handler(ComparableOperationHandlers.LowerThanOrEquals).And()
                    .Operation(FilterOperationKind.GreaterThanOrEquals)
                    .Handler(ComparableOperationHandlers.GreaterThanOrEquals).And()
                    .And()
                  .And();
}
```

**Create Date Time Filter Implicitly**

`DateTime` is a new filter. Hot Chocolate is only aware of its existence because of the delegate passed to `AddImplicitFilter`

```csharp
private static bool TryCreateDateTimeFilter(
    IDescriptorContext context,
    Type type,
    PropertyInfo property,
    IFilterConvention filterConventions,
    [NotNullWhen(true)] out FilterFieldDefintion? definition)
{
    if (type == typeof(DateTime))
    {
        var field = new DateTimeFilterFieldDescriptor(
          context, property, filterConventions);
        definition = field.CreateDefinition();
        return true;
    }

    definition = null;
    return false;
}
```

TODO: make filters name based
**Filter Field**

A filter field is a collection of operations. It holds the configuration of the different operations like _“from”_ and _“to”_. In classic Hot Chocolate fashion there is a descriptor that describes these collections. Hot Chocolate provides the base class `FilterFieldDescriptorBase` you can use as an extension point. There is quite a lot of boilerplate code you need to write. e.g. it makes sense to define an interface for the descriptor.
You find an example here: //TODO LINK

For the explicit binding, we need to override `CreateOperationDefinition`. In case the filter is bound implicitly, this method is invoked for each operation.
TODO: I think there is an issue with AllowNotEndsWith.

```csharp
// We override this method for implicity binding
protected override FilterOperationDefintion CreateOperationDefinition(
    FilterOperationKind operationKind) =>
        CreateOperation(operationKind).CreateDefinition();
```

For the implicit binding, we only need to add the methods `AllowFrom` and `AllowTo`.

```csharp
// The following to methods are for adding the filters explicitly
public IDateTimeFilterOperationDescriptor AllowFrom() =>
    GetOrCreateOperation(FilterOperationKind.GreaterThanOrEqual);

public IDateTimeFilterOperationDescriptor AllowTo() =>
    GetOrCreateOperation(FilterOperationKind.LowerThanOrEqual);

// This is just a little helper that reduces code duplication
private DateTimeFilterOperationDescriptor GetOrCreateOperation(
    FilterOperationKind operationKind) =>
        Filters.GetOrAddOperation(operationKind,
            () => CreateOperation(operationKind));
```

All the methods described above call `CreateOperation`. This method creates the operation descriptor. The `FitlerOperation` that is created here, will also be available for the expression visitor.

```csharp
// This helper method creates the operation.
private DateTimeFilterOperationDescriptor CreateOperation(
    FilterOperationKind operationKind)
    {
        // This operation is also available in execution.
        var operation = new FilterOperation(
            typeof(DateTime),
            Definition.Kind,
            operationKind,
            Definition.Property);

        return DateTimeOffsetFilterOperationDescriptor.New(
            Context,
            this,
            CreateFieldName(operationKind),
            RewriteType(operationKind),
            operation,
            FilterConvention);
    }
```

**Filter Operation**

In this example; there are two filter operations _"form"_ and _"to"_. The configuration with a descriptor combines explicit and implicit binding. As a base class, you can use `FilterOperationDescriptorBase`.
Here is the interface that is used in this example:

```csharp
public interface IDateTimeFilterOperationDescriptor
        : IDescriptor<FilterOperationDefintion>
        , IFluent
    {
        /// Define filter operations for another field.
        IDateTimeFilterFieldDescriptor And();

        /// Specify the name of the filter operation.
        IDateTimeFilterOperationDescriptor Name(NameString value);

        /// Specify the description of the filter operation.
        IDateTimeFilterOperationDescriptor Description(string value);

        /// Annotate the operation filter field with a directive.
        IDateTimeFilterOperationDescriptor Directive<T>(T directiveInstance)
            where T : class;
        IDateTimeFilterOperationDescriptor Directive<T>()
            where T : class, new();
        IDateTimeFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
```

You can find the implementation of this interface here: //TODO link

**Filter Type Extension**
The last missing piece to complete the integration into Hot Chocolate is an extension of `FilterInputType<T>`. This can again be done as a extension method.

```csharp
public IStringFilterFieldDescriptor Filter(
    Expression<Func<T, string>> property)
{
    if (property.ExtractMember() is PropertyInfo p)
    {
        return Fields.GetOrAddDescriptor(p,
            () => new StringFilterFieldDescriptor(Context, p));
    }

    throw new ArgumentException(
        FilterResources.FilterInputTypeDescriptor_OnlyProperties,
        nameof(property));
}
```

//TODO Open this api

---

##### Case Study: Filters for NetTopologySuite

**Situation**

> **Note:** If you are searching for `NetTopologySuite`, they are already implemented. Have a look at//TODO LINK

1. Implicit Binding: `[UseFiltering]` should automagically create `Point` and the corresponding "\_distance"
2. Explicity Binding: `desc.Filter(x => x.Location).AllowDistance()`
3. Expression Visitor: You want to directly filter on the database. You use EF Core.

Things are different in this case, as there is no longer a 1:1 mapping of input type to method or property. Imagine you want to fetch all bakeries that are near you. In C# you would write something like `dbContext.Bakeries.Where(x => x.Location.Distance(me.Location) < 5)`. This cannot be translated to a _GraphQL_ input type directly.

A _GraphQL_ query might look like this.

```graphql
{
  bakeries(
    where: { location: { distance: { from: { x: 32, y: 15 }, is_lt: 5 } } }
  ) {
    name
  }
}
```

_GraphQL_ input fields cannot have arguments. To work around this issue a data structure is needed that combines the filter payload and the operation. The input type for this example has the following structure.

```csharp
public class FilterDistance
{

    public FilterDistance(
        FilterPointData from)
    {
        From = from;
    }
    /// contains the x and y coordinates.
    public FilterPointData From { get; }

    public double Is { get; set; }
}
```

```graphql
input FilterDistanceInput {
  from: FilterPointDataInput!
  is: Float
  is_gt: Float
  is_gte: Float
  is_lt: Float
  is_lte: Float
  is_in: Float
  is_not: Float
  is_not_gt: Float
  is_not_gte: Float
  is_not_lt: Float
  is_not_lte: Float
  is_not_in: Float
}
```

//TODO: Add skip / inopfield!

Hot Chocolate would generate nested filters for the payload property "From" by default. This can be avoided by declaring the field as input payload.

```csharp
public class DistanceFilterType
    : FilterInputType<FilterDistance>
{
    protected override void Configure(
        IFilterInputTypeDescriptor<FilterDistance> descriptor)
    {
        descriptor.Input(x => x.From);
        descriptor.Filter(x => x.Is);
    }
}
```

**Convention & Implicit Factory & Type Descriptor**

The configuration of the convention, the implicit type factory and the descirptors are very similar to the the two examples before. To not bloat the documentation with duplication we just refere to these two examples and to the reference implementation here //TODO LINK

---

## Translating Filters

Hot Chocolate can translate incoming filters requests directly onto collections or even on to the database. In the default implementation, the output of this translation is a Linq expression that can be applied to `IQueryable` and `IEnumerable`. You can choose to change the expression that is generated or can even create custom output. Hot Chocolate is using visitors to translate input objects. [You find more information about visitors here.](TODO://ADDLINK).

### Expression Filters

Filter conventions make it easier to change how an expression should be generated. There are three different extension points you can use to change the behavior of the expression visitor. You do not have to worry about the visiting of the input object itself.

##### Describe the Expression Visitor

The expression visitor descriptor is accessible through the filter convention. By calling `UseExpressionVisitor` on the convention descriptor you gain access. The expression visitor has the default set of expressions preconfigured.

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(
        IFilterConventionDescriptor descriptor)
    {
        descriptor.UseExpressionVisitor()
    }
}
```

The descriptor provides a fluent interface that is very similar to the one of the convention descriptor itself. You have to specify what _operation_ on which _filter kind_ you want to configure. You can drill with `Kind` and `Operation` and go back up by calling `And()`:

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(
        IFilterConventionDescriptor descriptor)
    {
        descriptor
            .UseExpressionVisitor()
                .Kind(FilterKind.String)
                    .Operation(FilterKind.Equals)
                    .And()
                .And()
                .Kind(FilterKind.Comparable)
                    .Operation(FilterKind.In)
    }
}
```

**Visitation Flow**

The expression visitor starts as any other visitor at the node you pass in. Usually, this is the node object value node of the filter argument. It then starts the visitation. Every time the visitor _enters_ or _leaves_ an object field, it looks for a matching configuration. If there is no special _enter_ behavior of a field, the visitor generates the expression for the combination of _kind_ and _operation_.

The next two paragraphs show how the algorithm works in detail.

_Enter_

On _entering_ a field, the visitor tries to get a `FilterFieldEnter` delegate for the `FilterKind` of the current field. If a delegate was found, executed, and the execution return true, the `Enter` method returns the _action_ specified by the delegate. In all other cases, the visitor tries to execute an `OperationHandler` for the combination `FilterKind` and `OperationKind`. If the handler returns true, the expression returned by the handler is added to the context.

1. Let _field_ be the field that is visited
1. Let _kind_ be the `FilterKind` of _field_
1. Let _operation_ be the `FilterOperationKind` of _field_
1. Let _convention_ be the `FilterConvention` used by this visitor
1. Let _enterField_ be the `FilterFieldEnter` delegate for _kind_ on _convention_
1. If _enterField_ is not null:
   1. Let _action_ be the visitor action of _enterField_
   1. If _enterField_ returns true:
      1. **return** _action_
1. Let _operationHander_ be the `FilterOperationHandler` delegate for (_kind_, _operation_) on _convention_
1. If _operationHandler_ is not null:
   1. Let _expression_ be the expression generated by _operationHandler_
   1. If _enterField_ returns true:
      1. enqueue _expression_
1. **return** `SkipAndLeave`

_Leave_

On _entering_ a field, the visitor tries to get and execute a `FilterFieldLeave` delegate for the `FilterKind` of the current field.

1. Let _field_ be the field that is visited
1. Let _kind_ be the `FilterKind` of _field_
1. Let _operation_ be the `FilterOperationKind` of _field_
1. Let _convention_ be the `FilterConvention` used by this visitor
1. Let _leaveField_ be the `FilterFieldLeave` delegate for _kind_ on _convention_
1. If _leaveField_ is not null:
   1. Execute _leaveField_

**Operations**

The operation descriptor provides you with the method `Handler`. With this method, you can configure, how the expression for the _operation_ of this filter _kind_ is generated. You have to pass a delegate of the following type:

```csharp
public delegate bool FilterOperationHandler(
    FilterOperation operation,
    IInputType type,
    IValueNode value,
    IQueryableFilterVisitorContext context,
    [NotNullWhen(true)]out Expression? result);
```

This delegate might seem intimidating first, but it is not bad as it looks. If this delegate `true` the `out Expression?` is enqueued on the filters. This means that the visitor will pick it up as it composes the filters.

| Parameter                                | Description                             |
| ---------------------------------------- | --------------------------------------- |
| `FilterOperation operation`              | The operation of the current field      |
| `IInputType type`                        | The input type of the current field     |
| `IValueNode value`                       | The AST value node of the current field |
| `IQueryableFilterVisitorContext context` | The context that builds up the state    |
| `out Expression? result`                 | The generated expression                |

Operations handlers can be configured like the following:

```csharp {10,13}
public class CustomConvention : FilterConvention
{
    protected override void Configure(
        IFilterConventionDescriptor descriptor)
    {
        descriptor
            .UseExpressionVisitor()
                .Kind(FilterKind.String)
                    .Operation(FilterKind.Equals)
                        .Handler(YourVeryOwnHandler.HandleEquals)
                    .And()
                    .Operation(FilterKind.NotEquals)
                        .Handler(YourVeryOwnHandler.HandleNotEquals)
    }
}
```

TODO: add example

**Kind**

There are two extension points on each _filter kind_. You can alter the _entering_ of a filter and the _leaving_.

**Enter**
You can configure the entering with the following delegate:

```csharp
public delegate bool FilterFieldEnter(
    FilterOperationField field,
    ObjectFieldNode node,
    IQueryableFilterVisitorContext context,
    [NotNullWhen(true)]out ISyntaxVisitorAction? action);
```

If this field returns _true_ the filter visitor will continue visitation with the specified _action_ in the out parameter `action`. [Check out the visitor documentation for all possible actions](http://addlinkshere).
If the field does not return true and a visitor action, the visitor will continue and search for a _operation handler_. After this, the visitor will continue with `SkipAndLeave`.

| Parameter                                | Description                          |
| ---------------------------------------- | ------------------------------------ |
| `FilterOperationField field`             | The current field                    |
| `ObjectFieldNode node`                   | The object node of the current field |
| `IQueryableFilterVisitorContext context` | The context that builds up the state |
| `out ISyntaxVisitorAction? action`       | The visitor action                   |

**Leave**
You can configure the entering with the following delegate:

```csharp
public delegate void FilterFieldLeave(
    FilterOperationField field,
    ObjectFieldNode node,
    IQueryableFilterVisitorContext context);
```

| Parameter                                | Description                          |
| ---------------------------------------- | ------------------------------------ |
| `FilterOperationField field`             | The current field                    |
| `ObjectFieldNode node`                   | The object node of the current field |
| `IQueryableFilterVisitorContext context` | The context that builds up the state |
