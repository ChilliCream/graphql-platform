---
id: filters
title: Filtering
---

**What are filters?**

With _Hot Chocolate_ filters, you can expose complex filter objects through your GraphQL API that translate to native database queries.

The default filter implementation translates filters to expression trees that are applied to `IQueryable`.

## Overview

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


## Customizing Filters

The filter objects can be customized and you can rename and remove operations from it or define operations explicitly.

Filters are input objects and are defined through a `FilterInputType<T>`. In order to define and customize a filter we have to inherit from `FilterInputType<T>` and configure it like any other type.

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

The above type defines explicitly for what fields filter operations are allowed and what filter operations are allowed. Also the filter renames the equals filter to `equals`.

In order to apply this filter type we just have to provide the `UseFiltering` extension method with the filter type as type the argument.

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

## Sorting

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


**Why is the order important?**

Paging, filtering and sorting are modular middleware which form the field resolver pipeline.

The above example basically forms the following pipeline:

`Paging -> Filtering -> Sorting -> Field Resolver`

The paging middleware will first delegate to the next middleware, which is filtering.

The filtering middleware will also first delegate to the next middleware, which is sorting.

The sorting middleware will again first delegate to the next middleware, which is the actual field resolver.

The field resolver will call `GetPerson` which returns in this example an `IQueryable<Person>`. The queryable represents a not yet executed database query.

After the resolver has been executed and put its result onto the middleware context the sorting middleware will apply the sort order on the query.

After the sorting middleware has been executed and updated the result on the middleware context the filtering middleware will apply its filters on the queryable and updates the result on the middleware context.

After the paging middleware has been executed and updated the result on the middleware context the paging middleware will slice the data and execute the queryable which will then actually pull in data from the data source.

So, if we for instance applied paging as our last middleware the data set would have been sliced first and then filtered which in most cases is not what we acually want.
