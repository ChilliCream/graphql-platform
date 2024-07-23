using System.Threading.Tasks;

namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<IReadOnlyDictionary<Guid, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<Guid> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_GroupDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<ILookup<Guid, Entity>> GetEntitiesByIdAsync(
                    IReadOnlyList<Guid> entityIds,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static async Task<Entity> GetEntityByIdAsync(
                    Guid entityId,
                    CancellationToken cancellationToken) { }
            }
            """).MatchMarkdownAsync();
    }
}
