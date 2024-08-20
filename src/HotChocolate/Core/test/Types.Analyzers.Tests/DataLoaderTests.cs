namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<ILookup<int, Entity>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<Entity> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_GenericBatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<IReadOnlyDictionary<int, T>> GetEntityByIdAsync<T>(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_IDictionary_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static async Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }

                public static int CreateLookupKey(string key)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_From_OtherType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static async Task<IDictionary<int, Entity2>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }

                public static KeyValuePair<int, Entity2> CreateLookupKey(Entity1 key)
                    => default!;
            }

            public class Entity1
            {
                public int Id { get; set; }

                public Entity2? Entity2 { get; set; }
            }

            public class Entity2
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_Nullable_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }
}
