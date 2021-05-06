---
title: Data Loaders
---

If you want to read more about _DataLoader_ in general, you can head over to Facebook's [GitHub repository](https://github.com/facebook/dataloader).

GraphQL is very flexible in the way we can request data. This flexibility also introduces new classes of problems called _n+1_ problem for the GraphQL server developer.

In order to depict the issue that _DataLoader_ solves in this context, let me introduce a little GraphQL schema:

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

Since GraphQL requests are not fixed requests like REST requests, the developer really defines what data he/she wants. This avoids over-fetching data that you do not need and also saves you unnecessary round-trips to the GraphQL backend.

So, a query against the above schema could look like the following:

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

The above request fetches two persons in one go without the need to call the backend twice. The problem for the GraphQL backend is that field resolvers are atomic and do not have any knowledge about the query as a whole. So, a field resolver does not know that it will be called multiple times in parallel to fetch similar or equal data from the same data source.

This basically represents the first case where _DataLoader_ help us by batching requests against our database or backend service. Currently, we allow _DataLoader_ per request and globally.

So, let's look at some code in order to understand what they are doing. First, let's have a look at how we would write our field resolver without _DataLoader_:

```csharp
public async Task<Person> GetPerson(string id, [Service]IPersonRepository repository)
{
    return await repository.GetPersonById(id);
}
```

The above example would result in two calls to the person repository that would then fetch the persons one by one from our data source.

If you think that through you can see that each GraphQL request would cause multiple requests to our data source resulting in sluggish performance and unnecessary round-trips to our data source.

This, means that we reduced the round-trips from our client to our server with GraphQL but multiplied the round-trips between the data sources and the service layer.

With _DataLoader_ we can now centralise our person fetching and reduce the number of round trips to our data source.

In order to use _DataLoader_ with Hot Chocolate we have to add the _DataLoader_ registry. The _DataLoader_ registry basically manages the data loader instances and interacts with the execution engine.

```csharp
services.AddDataLoaderRegistry();
```

Next, we have to create a _DataLoader_ that now acts as intermediary between a field resolver and the data source.

You can either implement a _DataLoader_ as class or just provide us with a delegate that represents the fetch logic.

# Class DataLoader

Let us first look at the class _DataLoader_:

```csharp
public class PersonDataLoader : DataLoaderBase<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonDataLoader(IPersonRepository repository)
      : base(new DataLoaderOptions<string>())
    {
        _repository = repository;
    }

    protected override async Task<IReadOnlyList<Result<Person>>> FetchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        return _repository.GetPersonBatch(keys);
    }
}
```

The _DataLoader_ is now injected by the execution engine as a field resolver argument.

_DataLoader_ have to be injected at field resolver argument level and **NOT** as constructor arguments since the lifetime of a _DataLoader_ is in many cases shorter than the class containing the field resolvers.

```csharp
public Task<Person> GetPerson(string id, [DataLoader]PersonDataLoader personLoader)
{
    return personLoader.LoadAsync(id);
}
```

It is important that you do not have to register a _DataLoader_ with your dependency injection provider. Hot Chocolate will handle the instance management and register all _DataLoader_ automatically with the _DataLoader_ registry that we have added earlier.

Now, person requests in a single execution batch will be batched to the data source.

But there are still some more issues ahead that _DataLoader_ will help us with. For that we should amend our query a little bit.

```graphql
{
  a: person(id: "a") {
    name
    friends {
      name
    }
  }

  b: person(id: "b") {
    name
    friends {
      name
    }
  }
}
```

The above query now drills down into the friends property, which again yields persons.

Let's say our person object is located in a Mongo database and the document would look something like the following:

```json
{
  "id":"a"
  "name":"Foo"
  "friends": [
    "b",
    "c",
    "d"
  ]
}

{
  "id":"b"
  "name":"Bar"
  "friends": [
    "a",
    "c",
    "e"
  ]
}
```

The person with ID `a` is also friends with person `b`. Moreover, `a` is also friends with `c` and `d`. Furthermore, `b` is friends with `a` and also friends with `c` and `e`.
The best case now would be that we only fetch `c`, `d` and `e` since we have already fetched `a` and `b`.

This is the second problem class the _DataLoader_ utility helps us with since the _DataLoader_ contains a cache and holds the resolved instances by default for the duration of your request.

# Delegate DataLoader

With the class _DataLoader_ you have full control of how the _DataLoader_ works. But in many cases this control is not needed. We have specified four classes of _DataLoaders_ that can be specified as delegate.

## Batch DataLoader

The batch _DataLoader_ collects requests for entities per processing level and send them as a batch request to the data source. Moreover, the _DataLoader_ caches the retrieved entries within a request.

The batch _DataLoader_ gets the keys as `IReadOnlyList<TKey>` and returns a `IReadOnlyDictionary<TKey, TValue>`.

```csharp
public Task<Person> GetPerson(string id, IResolverContext context, [Service]IPersonRepository repository)
{
    return context.BatchDataLoader<string, Person>("personByIdBatch", keys => repository.GetPersonBatchAsync(keys)).LoadAsync(id);
}
```

_An example with the **Batch Dataloader** can be found [here](https://github.com/ChilliCream/hotchocolate-examples/blob/master/misc/DataLoader/MessageType.cs)._

## Group DataLoader

The Group _DataLoader_ is also a batch _DataLoader_ but instead of returning one entity per key it returns multiple entities per key. As with the Batch _DataLoader_ retrieved collections are cached within a request.

The Group _DataLoader_ gets the keys as `IReadOnlyList<TKey>` and returns a `ILookup<TKey, TValue>`.

```csharp
public Task<IEnumerable<Person>> GetPersonByCountry(string country, IResolverContext context, [Service]IPersonRepository repository)
{
    return context.GroupDataLoader<string, Person>("personByCountry", keys => repository.GetPersonsByCountry(keys).ToLookup(t => t.Country)).LoadAsync(country);
}
```

_An example with the **Batch Dataloader** can be found [here](https://github.com/ChilliCream/hotchocolate-examples/blob/master/misc/DataLoader/QueryType.cs)._

## Cache DataLoader

The cache _DataLoader_ is basically the easiest to implement since there is no batching involved. So, we can just use the initial `GetPersonById` method. We, do not get the benefits of batching with this one, but if in a query graph the same entity is resolved twice we will load it only once from the data source.

```csharp
public Task<Person> GetPerson(string id, IResolverContext context, [Service]IPersonRepository repository)
{
    return context.CacheDataLoader<string, Person>("personById", keys => repository.GetPersonById(keys)).LoadAsync(id);
}
```

_An example with the **Batch Dataloader** can be found [here](https://github.com/ChilliCream/hotchocolate-examples/blob/master/misc/DataLoader/MessageType.cs)._

## Fetch Once

`FetchOnceAsync` is not really a _DataLoader_ like described by facebook. It rather uses the infrastructure of our _DataLoader_ to provide an easy way to provide cache heavy resource calls that shall only be done once per request.

```csharp
public Task<Person> GetPerson(string id, IResolverContext context, [Service]IPersonRepository repository)
{
    return context.FetchOnceAsync("cachingLoader", () => repository.GetSomeResource());
}
```

# Stacked DataLoader Calls

This is more like an edge case that is supported than a certain type of _DataLoader_. Sometimes we have more complex resolvers that might first fetch data from one _DataLoader_ and use that to fetch data from the next. With the new _DataLoader_ implementation this is supported and under test.

```csharp
public Task<IEnumerable<Customer>> GetCustomers(string personId, IResolverContext context, [Service]IPersonRepository personRepository, [Service]ICustomerRepository customerRepository)
{
    Person person = await context.DataLoader("personLoader", keys => repository.GetPersonById(keys)).LoadAsync(id);
    return await context.DataLoader("customerLoader", keys => repository.GetCustomerById(keys)).LoadAsync(person.CustomerIds);
}
```

# Global DataLoader

Global _DataLoader_ are _DataLoader_ that are shared between requests. This can be useful for certain caching strategies.

In order to add support for global _DataLoader_ you can add a second _DataLoader_ registry. This one has to be declared as singleton. It is important that you declare the global registry first since we use the last registry to register ad-hoc _DataLoader_.

```csharp
services.AddSingleton<IDataLoaderRegistry, DataLoaderRegistry>();
services.AddDataLoaderRegistry();
```

It is important to know that you always have to do `AddDataLoaderRegistry` since this also sets up the batch operation that is needed to hook up the execution engine with the _DataLoader_ registry.

# DataLoader Dependency Injection Support

It is possible to register a DataLoader with the standard dependency injection container. This enables referencing DataLoaders through interfaces.

Here is how we can now register a DataLoader:

```csharp
services.AddDataLoader<IMyDataLoader, MyDataLoader>();
services.AddDataLoader<MyDataLoader>();
services.AddDataLoader<IMyDataLoader>(s => ....);
```

The DataLoaderRegistry is automatically registered when using this.

On the resolver side I can now resolve my DataLoader through an interface:

```csharp
public async Task<string> ResolveSomething(IMyDataLoader dataLoader)
{

}
```

I also do not need to use the `[DataLoader]` attribute I the interface implements IDataLoader.

# Custom Data Loaders and Batch Operations

With the new API we are introducing the `IBatchOperation` interface. The query engine will fetch all batch operations and trigger those once all data resolvers in one batch are running. We have implemented this interface for our _DataLoader_ as well. So, if you want to implement some database batching or integrate a custom _DataLoader_, then this interface is your friend. There is also a look ahead available which will provide you with the fields that have to be fetched.

If you are planning to implement something in this area, get in contact with us and we will provide you with more information.
