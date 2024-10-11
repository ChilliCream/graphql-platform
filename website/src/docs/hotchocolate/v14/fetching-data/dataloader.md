---
title: "DataLoader"
---

> If you want to read more about data loaders in general, you can head over to Facebook's [GitHub repository](https://github.com/facebook/dataloader).

Every data fetching technology suffers the _n+1_ problem.
The difference between GraphQL and e.g. REST is, that the _n+1_ problem occurs on the server, rather than on the client.
The clear benefit is, that we only have to deal with this problem once on the server, rather than on every client.

To depict the issue that data loaders solve in this context, let assume we have this schema:

```sdl
type Query {
  person(id: ID): Person
}

type Person {
  id: ID
  name: String
  friends: [Person]
}
```

The above schema allows to fetch a person by its internal identifier and each person has a list of friends that is represented by a list of persons.

A query against the above schema could look like the following:

```graphql
{
  a: person(id: "a") {
    name
  }

  b: person(id: "b") {
    name
  }
}
```

The above request fetches two persons in one go without the need to call the backend twice. The problem with the GraphQL backend is that field resolvers are atomic and do not have any knowledge about the query as a whole. So, a field resolver does not know that it will be called multiple times in parallel to fetch similar or equal data from the same data source.

The idea of a dataloader is to batch these two requests into one call to the database.

Let's look at some code to understand what data loaders are doing. First, let's have a look at how we would write our field resolver without data loaders:

```csharp
public async Task<Person> GetPersonAsync(
    string id,
    IPersonRepository repository,
    CancellationToken cancellationToken)
    => await repository.GetPersonByIdAsync(id);
```

The above example would result in two calls to the person repository that would then fetch the persons one by one from our data source.

If you think that through you see that each GraphQL request would cause multiple requests to our data source resulting in sluggish performance and unnecessary round-trips to our data source.

This means that we reduced the round-trips from our client to our server with GraphQL but still have the round-trips between the data sources and the service layer.

With data loaders we can now centralize the data fetching and reduce the number of round trips to our data source.

Instead of fetching the data from the repository directly, we fetch the data from the data loader.
The data loader batches all the requests together into one request to the database.

```csharp
// This is one way of implementing a data loader. You will find the different ways of declaring
// data loaders further down the page.
public class PersonByIdDataLoader : BatchDataLoader<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonBatchDataLoader(
        IPersonRepository repository,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _repository = repository;
    }

    protected override async Task<IReadOnlyDictionary<string, Person>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // instead of fetching one person, we fetch multiple persons
        var persons =  await _repository.GetPersonByIdsAsync(keys);
        return persons.ToDictionary(x => x.Id);
    }
}

public class Query
{
    public async Task<Person?> GetPerson(
        string id,
        PersonByIdDataLoader personById,
        CancellationToken cancellationToken)
        => await personById.LoadAsync(id, cancellationToken);
}
```

# Execution

With a data loader, you can fetch entities with a key.
These are the two generics you have in the class data loaders:

```csharp
public class BatchDataLoader<TId, TEntity>
```

`TId` is used as an identifier of `TEntity`. `TId` is the type of the values you put into `LoadAsync`.

The execution engine of Hot Chocolate tries to batch as much as possible.
It executes resolvers until the queue is empty and then triggers the data loader to resolve the data for the waiting resolvers.

# Data Consistency

Dataloader do not only batch calls to the database, they also cache the database response.
A data loader guarantees data consistency in a single request.
If you load an entity with a data loader in your request more than once, it is given that these two entities are equivalent.

Data loaders do not fetch an entity if there is already an entity with the requested key in the cache.

# Types of Data loaders

In Hot Chocolate you can declare data loaders in two different ways.
You can separate the data loading concern into separate classes or you can use a delegate in the resolver to define data loaders on the fly.
Below you will find the different types of data loaders with examples for class and delegate definition.

## Batch DataLoader

> One - To - One, usually used for fields like `personById` or one to one relations

The batch data loader collects requests for entities and sends them as a batch request to the data source. Moreover, the data loader caches the retrieved entries within a request.

The batch data loader gets the keys as `IReadOnlyList<TKey>` and returns an `IReadOnlyDictionary<TKey, TValue>`.

### Class

```csharp
public class PersonByIdDataLoader : BatchDataLoader<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonBatchDataLoader(
        IPersonRepository repository,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _repository = repository;
    }

    protected override async Task<IReadOnlyDictionary<string, Person>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // instead of fetching one person, we fetch multiple persons
        var persons =  await _repository.GetPersonByIdsAsync(keys);
        return persons.ToDictionary(x => x.Id);
    }
}

public class Query
{
    public async Task<Person?> GetPersonAsync(
        string id,
        PersonByIdDataLoader personById,
        CancellationToken cancellationToken)
        => await personById.LoadAsync(id, cancellationToken);
}
```

### Delegate

```csharp
public Task<Person> GetPersonAsync(
    string id,
    IResolverContext context,
    IPersonRepository repository,
    CancellationToken cancellationToken)
{
    return context.BatchDataLoader<string, Person>(
            async (keys, ct) =>
            {
                var result = await repository.GetPersonByIds(keys);
                return result.ToDictionary(x => x.Id);
            })
        .LoadAsync(id, cancellationToken);
}
```

### Source Generated

```csharp
public static class PersonDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<string, Person>> GetPersonByIdAsync(
      IReadOnlyList<string?> ids,
      IPersonRepository repository,
      CancellationToken cancellationToken)
    {
        var persons = await repository.GetPersonByIdsAsync(ids, cancellationToken);
        return persons.ToDictionary(x => x.Id);
    }
}

public class Query
{
    public async Task<Person> GetPersonAsync(
        string id,
        IPersonByIdDataLoader personById,
        CancellationToken cancellationToken)
        => await personById.LoadAsync(id, cancellationToken);
}
```

_An example with the **Batch DataLoader** can be found [here](https://github.com/ChilliCream/graphql-workshop/blob/master/code/complete/GraphQL/DataLoader/TrackByIdDataLoader.cs)._

## Group DataLoader

> One - To - Many, usually used for fields like `personsByLastName` or one to many relations

The group data loader is also a batch data loader but instead of returning one entity per key, it returns multiple entities per key. As with the batch data loader retrieved collections are cached within a request.

The group data loader gets the keys as `IReadOnlyList<TKey>` and returns an `ILookup<TKey, TValue>`.

### Class

```csharp
public class PersonsByLastNameDataloader
    : GroupedDataLoader<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonsByLastNameDataloader(
        IPersonRepository repository,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _repository = repository;
    }

    protected override async Task<ILookup<string, Person>> LoadGroupedBatchAsync(
        IReadOnlyList<string> names,
        CancellationToken cancellationToken)
    {
        var persons = await _repository.GetPersonsByLastNameAsync(names, cancellationToken);
        return persons.ToLookup(x => x.LastName);
    }
}

public class Query
{
    public async Task<IEnumerable<Person>> GetPersonByLastName(
        string lastName,
        PersonsByLastNameDataloader personsByLastName,
        CancellationToken cancellationToken)
        => await personsByLastName.LoadAsync(lastName, cancellationToken);
}
```

### Delegate

```csharp
public Task<IEnumerable<Person>> GetPersonByLastName(
   string lastName,
   IResolverContext context,
   IPersonRepository repository,
    CancellationToken cancellationToken)
{
    return context.GroupDataLoader<string, Person>(
            async (keys, ct) =>
            {
                var result = await repository.GetPersonsByLastName(keys);
                return result.ToLookup(t => t.LastName);
            })
        .LoadAsync(lastName, cancellationToken);
}
```

### Source Generated

```csharp
public static class PersonDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<string, Person[]>> GetPersonsByLastNameAsync(
        IReadOnlyList<string?> lastNames,
        IPersonRepository repository,
        CancellationToken cancellationToken)
    {
        var persons = await repository.GetPersonsByLastNameAsync(lastNames, cancellationToken);
        return persons.GroupBy(x => x.LastName).ToDictionary(x => x.Key, x => x.ToArray());
    }
}

public class Query
{
    public async Task<IEnumerable<Person>> GetPersonByLastName(
        string id,
        IPersonsByLastNameDataLoader personsByLastName,
        CancellationToken cancellationToken)
        => await personsByLastName.LoadAsync(id, cancellationToken);
}
```

## Cache DataLoader

> No batching, just caching. This data loader is used rarely. You most likely want to use the batch data loader.

The cache data loader is the easiest to implement since there is no batching involved. You can just use the initial `GetPersonById` method. We do not get the benefits of batching with this one, but if in a query graph the same entity is resolved twice we will load it only once from the data source.

### Class

```csharp
public class PersonByIdDataLoader : CacheDataLoader<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonByIdDataLoader(
        IPersonRepository repository,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _repository = repository;
    }

    protected override async Task<Person?> LoadSingleAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await _repository.GetPersonByIdAsync(key, cancellationToken);
    }
}

public class Query
{
    public async Task<Person?> GetPersonAsync(
        string id,
        PersonByIdDataLoader personById,
        CancellationToken cancellationToken)
        => await personById.LoadAsync(id, cancellationToken);
}
```

### Delegate

```csharp
public Task<Person?> GetPersonAsync(
    string id,
    IResolverContext context,
    IPersonRepository repository,
    CancellationToken cancellationToken)
{
    return context.CacheDataLoader<string, Person>(
        "personById",
        keys => repository.GetPersonById(keys))
        .LoadAsync(id, cancellationToken);
}
```

### Source Generated

```csharp
public static class PersonDataLoader
{
    [DataLoader]
    public static async Task<Person?> GetPersonByIdAsync(
        string id,
        IPersonRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetPersonByIdAsync(id, cancellationToken);
}

public class Query
{
    public async Task<Person?> GetPersonAsync(
        string id,
        IPersonByIdDataLoader personById,
        CancellationToken cancellationToken)
        => await personById.LoadAsync(id, cancellationToken);
}
```

# DataLoader with Projections

When you have large objects with many fields, you might want to project only a subset of the fields with a DataLoader. This can be achieved with stateful DataLoader. Source generated DataLoader are stateful by default. For class DataLoader you have to inherit from `StatefulBatchDataLoader<TKey, TValue>`, `StatefulGroupedDataLoader<TKey, TValue>` or `StatefulCacheDataLoader<TKey, TValue>`.

With a stateful DateLoader you can pass on a selection to the DataLoader which is translated into an expression (`LambdaExpression<Func<TValue, TProjection>>`). Within your DataLoader, inject the `ISelectorBuilder` and apply
it to your `IQueryable<T>`.

```csharp
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> ids,
        ISelectorBuilder selector, // selector builder
        CatalogContext context,
        CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(selector, t => t.Id) // apply selector
            .ToDictionaryAsync(t => t.Id, ct);
}
```

In order to apply the selector we provide an extension method called `Select` which applies the `selector` in addition to the key selector. Since the required data might not contain the DataLoader key we have to always provide a key selector as well.

This `ProductByIdDataLoader` is no projectable but will only apply projections if at least one selection passed in from the usage side.

If we would use the `ProductByIdDataLoader` without providing a selection it would just return the full entity.

```csharp
public class Query
{
    public async Task<Product> GetProductAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

However if we provide a selection the DataLoader is branched and will return an entity with only the selected fields.

```csharp
public class Query
{
    public async Task<Product> GetProductAsync(
        int id,
        IProductByIdDataLoader productById,
        ISelection selection,
        CancellationToken cancellationToken)
        => await productById
            .Select(selection)
            .LoadAsync(id, cancellationToken);
}
```

Important to note here is that when using projections we can no longer make use of the cache in the same way as before. When we branch a DataLoader we assign the DataLoader a different partition of the cache. This means that only resolvers will share the same cache partition if their selection is translated into the exact same selector expression.

In addition to the GraphQL selection we can also chain in manual includes to our DataLoader call.

```csharp
public class Query
{
    public async Task<Product> GetProductAsync(
        int id,
        IProductByIdDataLoader productById,
        ISelection selection,
        CancellationToken cancellationToken)
        => await productById
            .Select(selection)
            .Include(t => t.Name)
            .LoadAsync(id, cancellationToken);
}
```

This allows us to make sure that certain data is always included in the projection. Lastly, instead of always including data we can also use requirements when using type extensions.

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    [UsePaging(ConnectionName = "BrandProducts")]
    public static async Task<Connection<Product>> GetProductsAsync(
        [Parent(nameof(Brand.Id))] Brand brand, // id is always required for this parent
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
        => await productService.GetProductsByBrandAsync(brand.Id, pagingArguments, cancellationToken).ToConnectionAsync();
}
```

When extending a type we can describe requirements for our parent which are recognized by the DataLoader. This way we can make sure that the parent always provides the required data for the projection even if the data was not requested by the user.

We can also describe more complex requirements by using a selection-set syntax with the property names of our parent.

```csharp
[Parent("Id Name")]
```

# Stacked DataLoader Calls

This is more like an edge case that is supported than a certain type of data loader. Sometimes we have more complex resolvers that might first fetch data from one data loader and use that to fetch data from the next.

```csharp
public Task<IEnumerable<Customer>> GetCustomers(
    string personId,
    PersonByIdDataLoader personByIdDataLoader,
    CustomerByIdsDataLoader customerByIdsDataloader)
{
    Person person = await personByIdDataLoader.LoadAsync(personId);
    return await customerByIdsDataloader.LoadAsync(person.CustomerIds);
}
```
