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

            internal interface INotDataLoader : IDataLoader<int, string> { }

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
    public async Task GenericDataLoaderMethods_RaiseExpectedDiagnostics()
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
                [DataLoader<IBatchDataLoader<int, string>>]
                private static Task<IReadOnlyDictionary<int, string>> GetGenericAsync<T>(
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
    public async Task ByRefGenericDataLoaderReturns_RaiseExpectedDiagnostics()
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
                private static Task<IReadOnlyDictionary<int, string>> s_result = default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static ref Task<IReadOnlyDictionary<int, string>> GetByRefAsync(
                    IReadOnlyList<int> keys)
                    => ref s_result;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static ref readonly Task<IReadOnlyDictionary<int, string>> GetByRefReadonlyAsync(
                    IReadOnlyList<int> keys)
                    => ref s_result;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenericDataLoaderParameterModifiers_RaiseExpectedDiagnostics()
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
                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetRefKeyAsync(
                    ref IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetInKeyAsync(
                    in IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOutKeyAsync(
                    out IReadOnlyList<int> keys)
                {
                    keys = default!;
                    return default!;
                }

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetRefReadonlyKeyAsync(
                    ref readonly IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetRefParameterAsync(
                    IReadOnlyList<int> keys,
                    ref int value)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetInParameterAsync(
                    IReadOnlyList<int> keys,
                    in int value)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetOutParameterAsync(
                    IReadOnlyList<int> keys,
                    out int value)
                {
                    value = default;
                    return default!;
                }

                [DataLoader<IBatchDataLoader<int, string>>]
                internal static Task<IReadOnlyDictionary<int, string>> GetRefReadonlyParameterAsync(
                    IReadOnlyList<int> keys,
                    ref readonly int value)
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

    [Fact]
    public async Task DuplicateGenericDataLoaderTypes_RaiseExpectedDiagnostics()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : IBatchDataLoader<int, string> { }

            internal static class FirstLoaders
            {
                [DataLoader<IEntityLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetFirstAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }

            internal static class SecondLoaders
            {
                [DataLoader<IEntityLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetSecondAsync(
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
    public async Task GenericDataLoaderAdditionalMembersWithoutPartialClass_RaisesExpectedDiagnostic()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : IBatchDataLoader<int, string>
            {
                string Description { get; }
            }

            internal static class Loaders
            {
                [DataLoader<IEntityLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
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
    public async Task GenericDataLoaderAdditionalMembersImplementedByPartialClass_DoNotRaiseDiagnostic()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : IBatchDataLoader<int, string>
            {
                string Description { get; }
            }

            public sealed partial class EntityByIdDataLoader
            {
                public string Description => "Entity loader";
            }

            internal static class Loaders
            {
                [DataLoader<IEntityLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
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
    public async Task GenericDataLoaderAdditionalMembersImplementedByNamedPartialClass_DoNotRaiseDiagnostic()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : IBatchDataLoader<int, string>
            {
                string Description { get; }
            }

            public sealed partial class CustomEntityDataLoader
            {
                public string Description => "Entity loader";
            }

            internal static class Loaders
            {
                [DataLoader<IEntityLoader>("CustomEntity")]
                internal static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
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
    public async Task PublicInterfaceAccessModifier_RaisesExpectedDiagnostic()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class Loaders
            {
                [DataLoader<IBatchDataLoader<int, string>>(
                    AccessModifier = DataLoaderAccessModifier.PublicInterface)]
                internal static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
