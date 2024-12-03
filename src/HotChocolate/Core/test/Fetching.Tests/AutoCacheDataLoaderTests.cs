using System.Security.Cryptography.X509Certificates;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

public static class AutoCacheDataLoaderTests
{
    [Fact]
    public static async Task Ensure_RootConnections_Are_Cached()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType(typeof(CatExtensions))
                .AddDataLoader<CatDataLoader>()
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                cats(first: 10) {
                    nodes {
                        name
                        counter
                        friend {
                            name
                            counter
                        }
                    }
                }
            }
            """);

        result.MatchMarkdownSnapshot();
    }

    public class Query
    {
        [UsePaging]
        public Cat[] GetCats() =>
        [
            new Cat("Garfield", "Luna"),
            new Cat("Luna", null),
            new Cat("Sylvester", "Oreo"),
            new Cat("Oreo", "Sylvester")
        ];
    }

    public class Cat(string name, string? friendName)
    {
        public string Name { get; } = name;

        public string? FriendName { get; } = friendName;

        public int Counter { get; set; } = CounterStore.GetNextCatCount();
    }

    [ExtendObjectType<Cat>]
    public static class CatExtensions
    {
        [BindMember(nameof(Cat.FriendName))]
        public static async Task<Cat?> GetFriend([Parent] Cat cat, CatDataLoader catByName)
        {
            return cat.FriendName is null
                ? null
                : await catByName.LoadAsync(cat.FriendName);
        }
    }

    public class CatDataLoader : CacheDataLoader<string, Cat>
    {
        public CatDataLoader(DataLoaderOptions options) : base(options)
        {
            PromiseCacheObserver
                .Create(item => item.Name, this)
                .Accept(this);
        }

        protected override Task<Cat> LoadSingleAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Cat(key, "WRONG"));
        }
    }

    public static class CounterStore
    {
        private static int _catCounter = 0;

        public static int GetNextCatCount() => Interlocked.Increment(ref _catCounter);
    }
}
