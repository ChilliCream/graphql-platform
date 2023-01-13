using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

[QueryType]
public class SomeQuery
{
    public IEntity? GetPerson() => new Person();

    [GraphQLType("CustomEnum")]
    public ValueTask<object?> GetEnum() => default;

    public Book GetBook() => new() { Title = "SomeTitle" };

    public Task<string> WithDataLoader(FoosByIdDataLoader foosById)
    {
        return foosById.LoadAsync("a");
    }
}

[MutationType]
public static class SomeMutation
{
    public static string DoSomething() => "abc";
}

[SubscriptionType]
public static class SomeSubscription
{
    public static string OnSomething() => "abc";
}

public static class DataLoaderGen
{
    [DataLoader(Scoped = true)]
    public static async Task<IReadOnlyDictionary<string, string>> GetFoosById(
        IReadOnlyList<string> ids,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return ids.ToDictionary(t => t, t => t);
    }

    [DataLoader(Scoped = true)]
    public static async Task<string> GetFoosById2(
        string id,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return "abc";
    }

    [DataLoader(Scoped = true)]
    public static async Task<ILookup<string, string>> GetFoosById3(
        IReadOnlyList<string> ids,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return default;
    }
}

public class SomeService { }



