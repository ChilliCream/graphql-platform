using GreenDonut;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public static class DataLoaders
{
    [DataLoader(Lookups = [nameof(CreateLookupKey)])]
    public static async Task<IReadOnlyDictionary<int, string>> GetSomeInfoById(
        IReadOnlyList<int> keys)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    public static int CreateLookupKey(string key)
        => default!;

    public static int CreateLookupKey(Guid key)
        => default!;
}

public sealed class SomeInfoByIdDataLoader1
    : global::GreenDonut.DataLoaderBase<int, string>
    , ISomeInfoByIdDataLoader
{
    private readonly global::System.IServiceProvider _services;

    public SomeInfoByIdDataLoader1(
        global::System.IServiceProvider services,
        global::GreenDonut.IBatchScheduler batchScheduler,
        global::GreenDonut.DataLoaderOptions options)
        : base(batchScheduler, options)
    {
        _services = services ??
            throw new global::System.ArgumentNullException(nameof(services));

        PromiseCacheObserver
            .Create<int, string>(DataLoaders.CreateLookupKey, this)
            .Accept(this);

        PromiseCacheObserver
            .Create<int, Guid, string>(DataLoaders.CreateLookupKey, this)
            .Accept(this);
    }

    protected override async ValueTask FetchAsync(IReadOnlyList<int> keys, Memory<Result<string>> results, CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await HotChocolate.Types.DataLoaders.GetSomeInfoById(keys).ConfigureAwait(false);
    }

    private void CopyResults1(
        IReadOnlyList<int> keys,
        Span<Result<string?>> results,
        IReadOnlyDictionary<int, string> resultMap)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            if (resultMap.TryGetValue(keys[i], out var value))
            {
                results[i] = new Result<string?>(value);
            }
            else
            {
                results[i] = new Result<string?>(default(string));
            }
        }
    }

    private void CopyResults2(
        IReadOnlyList<int> keys,
        Span<Result<string[]>> results,
        ILookup<int, string> resultMap)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (resultMap.Contains(key))
            {
                var items = resultMap[key];
                results[i] = Result<string[]>.Resolve(items.ToArray());
            }
            else
            {
                results[i] = Result<string[]>.Resolve([]);
            }
        }
    }
}
