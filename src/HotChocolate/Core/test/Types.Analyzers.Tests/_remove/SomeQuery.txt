using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;

[assembly: DataLoaderDefaults(
    ServiceScope = DataLoaderServiceScope.DataLoaderScope,
    AccessModifier = DataLoaderAccessModifier.Public)]

namespace HotChocolate.Types;

[QueryType]
public static class SomeQuery
{
    public static IEntity? GetPerson() => new Person();

    [GraphQLType("CustomEnum")]
    public static ValueTask<object?> GetEnum() => default;

    public static Book GetBook() => new() { Title = "SomeTitle", };

    public static Task<string> WithDataLoader(
        IFoosByIdDataLoader foosById,
        CancellationToken cancellationToken)
    {
        return foosById.LoadAsync("a", cancellationToken);
    }

    [DataLoader]
#pragma warning disable CS1998
    public static async Task<IReadOnlyDictionary<string, string>> GetFoosById56(
#pragma warning restore CS1998
        IReadOnlyList<string> keys,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return default!;
    }

    // should be ignored on the schema
    [DataLoader]
#pragma warning disable CS1998
    public static async Task<string> GetFoosById55(
#pragma warning restore CS1998
        string id,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return "abc";
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
    [DataLoader]
    internal static async Task<IReadOnlyDictionary<string, string>> GetFoosById(
        IReadOnlyList<string> ids,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return await Task.FromResult(ids.ToDictionary(t => t, t => t));
    }

    [DataLoader]
    public static async Task<string> GetFoosById2(
        string id,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return await Task.FromResult("abc");
    }

    [DataLoader(ServiceScope = DataLoaderServiceScope.OriginalScope)]
    public static Task<ILookup<string, string>> GetFoosById3(
        IReadOnlyList<string> ids,
        SomeService someService,
        CancellationToken cancellationToken)
    {
        return default!;
    }

    [DataLoader]
    public static Task<string> GetGenericById(
        IReadOnlyList<string> ids,
        GenericService<GenericService<string>> someService,
        CancellationToken cancellationToken)
    {
        return default!;
    }
}

public class SomeService;

public class GenericService<T>;
