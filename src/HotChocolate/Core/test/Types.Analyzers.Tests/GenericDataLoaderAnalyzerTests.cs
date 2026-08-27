namespace HotChocolate.Types;

public class GenericDataLoaderAnalyzerTests
{
    [Fact]
    public async Task InvalidGenericDataLoaderContracts_RaiseExpectedDiagnostics()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface INotDataLoader { }

            internal interface IAmbiguousDataLoader
                : IBatchDataLoader<int, string>, ICacheDataLoader<int, string>
            {
            }

            internal static class TestClass
            {
                [DataLoader<INotDataLoader>]
                internal static Task<string> GetInvalidTypeAsync(int key) => default!;

                [DataLoader<IAmbiguousDataLoader>]
                internal static Task<string> GetAmbiguousTypeAsync(int key) => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetInvalidBatchKeyAsync(int key)
                    => default!;

                [DataLoader<ICacheDataLoader<int, string>>]
                internal static Task<string> GetInvalidCacheKeyAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<ILookup<int, string>> GetInvalidBatchReturnTypeAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<ICacheDataLoader<int, string>>]
                internal static Task<int> GetInvalidCacheReturnTypeAsync(int key) => default!;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MultipleDataLoaderAttributes_RaiseExpectedDiagnostic()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOldThenGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                [DataLoader]
                internal static Task<IReadOnlyDictionary<int, string>> GetGenericThenOldAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetMultipleGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ValidGenericDataLoaderContracts_DoNotRaiseDiagnostics()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityByIdDataLoader : IBatchDataLoader<int, string> { }

            internal static class TestClass
            {
                [DataLoader<IEntityByIdDataLoader>]
                internal static ValueTask<IDictionary<int, string>> GetByIdAsync(
                    IReadOnlyList<int> keys)
                    => default;

                [DataLoader<ICacheDataLoader<int, string>>]
                internal static ValueTask<string> GetByKeyAsync(int key) => default;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
