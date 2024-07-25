using System.Threading.Tasks;

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
}
