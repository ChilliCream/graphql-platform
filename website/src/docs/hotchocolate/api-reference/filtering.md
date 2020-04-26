---
title: Filtering
---

**What are filters?**

With _Hot Chocolate_ filters, you can expose complex filter objects through your GraphQL API that translates to native database queries.

The default filter implementation translates filters to expression trees that are applied to `IQueryable`.

# Overview

Filters by default work on `IQueryable` but you can also easily customize them to use other interfaces.

_Hot Chocolate_ by default will inspect your .NET model and infer the possible filter operations from it.

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

> ⚠️ **Note:** If you use more than middleware, keep in mind that **ORDER MATTERS** _Why order matters_ <<Add link >>

> ⚠️ **Note:** Be sure to install the `HotChocolate.Types.Filters` NuGet package.

In the following example, the person resolver returns the `IQueryable` representing the data source. The `IQueryable` represents a not executed database query on which _Hot Chocolate_ can apply filters.

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

The filter objects can be customized and you can rename and remove operations from it or define operations explicitly.

Filters are input objects and are defined through a `FilterInputType<T>`. To define and customize a filter we have to inherit from `FilterInputType<T>` and configure it like any other type.

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

The above type defines explicitly for what fields filter operations are allowed and what filter operations are allowed. Also, the filter renames the equals filter to `equals`.

To apply this filter type we just have to provide it to the `UseFiltering` extension method with as the generic type argument.

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

If you want to combine for instance paging, filtering and sorting make sure that the order is like follows:

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

Paging, filtering and sorting are modular middlewares that form the field resolver pipeline.

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

Filtering can be broken down into different kinds of filters that then have different operations.
The filter kind is bound to the type. A string is fundamentally something different than an array or an object.
Each filter kind has different operations that can be applied to it. Some operations are unique to a filter and some operations are shared across multiple filters.
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

_Hot Chococlate_ already provides two naming schemes for filters. If you would like to define your own naming scheme or extend existing ones have a look at the documentation of <<LINk FILTER CONVENTIONS>>

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

---

# Customizing Filter

Hot Chocolate provides different APIs to customize filtering. You can write custom filter input types, customize the inference behavior of .NET Objects, customize the generated expression, or create a custom visitor and attach your exotic database.

**As this can be a bit overwhelming the following questionnaire might help:**

|                                                                                                                            |                                 |
| -------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| _You do not want all of the generated filters and only allow a particular set of filters in a specific case?_              | Custom&nbsp;FilterInputType     |
| _You want to change the name of a field or a whole type?_                                                                  | Custom&nbsp;FilterInputType     |
| _You want to change the name of the `where` argument?_                                                                     | Filter Conventions ArgumentName |
| _You want to configure how the name and the description of filters are generated in general? e.g. `PascalCaseFilterType`?_ | Filter&nbsp;Conventions         |
| _You want to configure what filters are allowed in general?_                                                               | Filter&nbsp;Conventions         |
| \_Your database provider does not support certain operations of `IQueryable`                                               | Filter&nbsp;Conventions         |
| _You want to change the naming of a particular filter type? e.g._ `foo_contains` _should be_ `foo_like`                    | Filter&nbsp;Conventions         |
| _You want to customize the expression a filter is generating: e.g._ `_equals` _should not be case sensitive?_              | Expression&nbsp;Visitor&nbsp;   |
| _You want to create your own filter types with custom parameters and custom expressions? e.g. GeoJson?_                    | Filter&nbsp;Conventions         |
| _You have a database client that does not support `IQueryable` and wants to generate filters for it?_                      | Custom&nbsp;Visitor             |

# Custom&nbsp;FilterInputType

Under the hood, filtering is based on top of normal _Hot Chocolate_ input types. You can easily customize them with a very familiar fluent interface. The filter input types follow the same `descriptor` scheme as you are used to from the normal filter input types. Just extend the base class `FilterInputType<T>` and override the descriptor method.

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

`IFilterInputTypeDescriptor<T>` supports most of the methods of `IInputTypeDescriptor<T>` and adds the configuration interface for the filters. By default filters for all fields of the type are generated.
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

To add or customize a filter you have to use `Filter(x => x.Foo)` for scalars `List(x => x.Bar)` for lists and `Object(x => x.Baz)` for nested objects.
These methods will return fluent interfaces to configure the filter for the selected field.

A field has different filter operations that can be configured. You will find more about filter types and filter operations here <<LINK>>
When fields are bound implicitly, meaning filters are added for all properties, you may want to hide a few fields. You can do this with `Ignore(x => Bar)`.
Operations on fields can again be bound implicitly or explicitly. By default, operations are generated for all fields of the type.
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
| `csharp±Name(NameString value)`                                                  | Defines the graphql name of the `FilterInputType<T>`.                                                                                           |
| `csharp±Ignore( Expression<Func<T, object>> property);`                          | Ignore the specified property.                                                                                                                  |
| `csharp±Filter( Expression<Func<T, string>> property)`                           | Defines a string filter for the selected property.                                                                                              |
| `csharp±Filter( Expression<Func<T, bool>> property)`                             | Defines a bool filter for the selected property.                                                                                                |
| `csharp±Filter( Expression<Func<T, IComparable>> property)`                      | Defines a comarable filter for the selected property.                                                                                           |
| `csharp±Object<TObject>( Expression<Func<T, TObject>> property)`                 | Defines a object filter for the selected property.                                                                                              |
| `csharp±List( Expression<Func<T, IEnumerable<string>>> property)`                | Defines a array string filter for the selected property.                                                                                        |
| `csharp±List( Expression<Func<T, IEnumerable<bool>>> property)`                  | Defines a array bool filter for the selected property.                                                                                          |
| `csharp±List( Expression<Func<T, IEnumerable<IComparable>>> property)`           | Defines a array comarable filter for the selected property.                                                                                     |
| `csharp±Filter<TObject>( Expression<Func<T, IEnumerable<TObject>>> property)`    | Defines a array object filter for the selected property.                                                                                        |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive `directiveInstance` to the type                                                                                                   |
| `csharp±Directive<TDirective>(TDirective directiveInstance)`                     | Add directive of type `TDirective` to the type                                                                                                  |
| `csharp±Directive<TDirective>(NameString name, params ArgumentNode[] arguments)` | Add directive of type `TDirective` to the type                                                                                                  |

---

# Filter Conventions

The customization of filters with `FilterInputTypes<T>` works if you only want to customize one specific filter type.
If you want to change the behavior of all filter types, you want to create a convention for your filters. The filter convention comes with a fluent interface that is close to a type descriptor.
You can see the convention as a configuration object that holds the state that is used by the type system or the execution engine.

## Get Started

To use a filter convention you can extend `FilterConvention` and override the `Configure` method. Alternatively, you can directly configure the convention over the constructor argument.
You then have to register your custom convention on the schema builder with `AddConvention`.

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
    }
}

SchemaBuilder.New().AddConvention<CustomConvention>();
//
SchemaBuilder.New().AddConvention(new FilterConvention(x => /*Config*/));
```

## Convention Descriptor Basics

In this section, we will take a look at the basic features of the filter convention.
The documentation will reference often to `descriptor`. Imagine this `descriptor` as the parameter of the Configure method of the filter convention in the following context:

```csharp
public class CustomConvention : FilterConvention
{
    protected override void Configure(
       /**highlight-start**/
        IFilterConventionDescriptor descriptor
      /**highlight-end**/
     )
     {}
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

To change the way filter types are named, you have to exchange the factory.

You have to provide a delegate of the following type:

```csharp
public delegate NameString GetFilterTypeName(
    IDescriptorContext context,
    Type entityType);
```

**Configuration**

```csharp
descriptor.TypeName(
    (context,types) => context.Naming.GetTypeName(entityType, TypeKind.Object) + "Custom");
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
descriptor.TypeName(
    (context,types) => context.Naming.GetTypeDescription(entityType, TypeKind.Object); + "Custom");
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

By default, all predefined values are configured. To start from scratch, you need to call `Reset()`first.

**Configuration**

```csharp
descriptor.Reset();
```

**Result**

> **⚠ Note:** You will need to add a complete configuration, otherwise the filter will not work as desired!

---

## Describe with convention

With the filter convention descriptor, you have full control over what filters are inferred, their names, operations, and a lot more.
The convention provides a familiar interface to the type configuration. It is recommended to first take a look at `Filter & Operations` to understand the concept of filters. This will help you understand how the filter configuration works.

Filtering has two core components at its heart. First, you have the inference of filters based on .NET types. The second component is an interceptor that translates the filters to the desired output and applies it to the resolver pipeline. These two parts can (and have to) be configured completely independently. With this separation, it is possible to easily extend the behavior. The descriptor is designed to be extendable by extension methods.

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

In this section, we will focus mainly on the generation of the schema. If you are interested in changing how filters are translated to the database, you have to look here <<INSERT LINK HERE>>

#### Configure Filter Operations

Operations can be configured in two ways.

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

```csharp{1, 6}
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

```graphql{8,18}
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

```graphql{2-4,11-14, 20-22,27-29}
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

_Hot Chocolate_ comes preconfigured with a set of operations. If you like to hide operations globally, you can use `Ignore` for it.
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

```graphql{2,4, 8,14,18}
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

The default binding behavior of _Hot Chocolate_ is implicit. Filter types are no exception.
This first may seem like magic, but unfortunately, there is none. It is just code. With `AddImplicitFilter` you can add this pinch of magic to your extension too.
The filters are created as the type is generated. For each property of a model, a list of factories is sequentially asked to create a definition. The first that can handle the property wins and creates a definition for the filter.

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
It makes sense to encapsulate that logic in a FilterFieldDescriptor though. This descriptor can the

