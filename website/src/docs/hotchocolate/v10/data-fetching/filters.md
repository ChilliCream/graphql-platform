---
title: Filter and Sorting Support
---

**What are filters?**

With the Hot Chocolate filters you are able to expose complex filter object through your GraphQL API that translate to native database queries.

The default filter implementation translates filters to expression trees that are applied on `IQueryable`.

# Using Filters

Filters by default work on `IQueryable` but you can also easily customize them to use other interfaces.

Hot Chocolate by default will inspect your .NET model and infer from that the possible filter operations.

The following type would yield the following filter operations:

```csharp
public class Foo
{
    public string Bar { get; set; }
}
```

```sdl
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

Getting started with filters is very easy and if you do not want to explicitly define filters or customize anything then filters are super easy to use, lets have a look at that.

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

> ⚠️ **Note**: Be sure to install the `HotChocolate.Types.Filters` NuGet package.

In the above example the person resolver just returns the `IQueryable` representing the data source. The `IQueryable` represents a not executed database query on which we are able to apply filters.

The next thing to note is the `UseFiltering` extension method which adds the filter argument to the field and a middleware that can apply those filters to the `IQueryable`. The execution engine will in the end execute the `IQueryable` and fetch the data.

# Customizing Filters

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

In order to apply this filter type we just have to provide the `UseFiltering` extension method with the filter type as type argument.

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

# AND / OR Filter

There are two built in fields.

- `AND`: Every condition has to be valid
- `OR` : At least one condition has to be valid

Example:

```graphql
query {
  posts(
    first: 5
    where: { OR: [{ title_contains: "Doe" }, { title_contains: "John" }] }
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

**⚠️ OR does not work when you use it like this: **

```graphql
query {
  posts(
    first: 5
    where: { title_contains: "John", OR: { title_contains: "Doe" } }
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

In this case the filters are applied like `title_contains: "John" AND title_contains: "Doe"`

# Customizing Filter Transformation

With our filter solution you can write your own filter transformation which is fairly easy once you wrapped your head around transforming graphs with visitors.

We provide a `FilterVisitorBase` which is the base of our `QueryableFilterVisitor` and it is basically just implementing an new visitor that walks the filter graph and translates it into any other query syntax.

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

Example:

```graphql
query {
  person(order_by: { name: DESC }) {
    name
    age
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

Example:

```graphql
query {
  person(order_by: { name: DESC }) {
    edges {
      node {
        id
        name
      }
    }
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

So, if we for instance applied paging as our last middleware the data set would have been sliced first and then filtered which in most cases is not what we actually want.
