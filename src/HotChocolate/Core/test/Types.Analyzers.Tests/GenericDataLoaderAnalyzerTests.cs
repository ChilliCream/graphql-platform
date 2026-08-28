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
    public async Task AdditionalInterfaceMembers_Should_NotRaiseHC0127_When_PartialClassImplementsCompilerShapes()
    {
        // arrange
        const string source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IExtendedDataLoader
                : IBatchDataLoader<int, string>, IInheritedDataLoaderRequirements<int> { }

            internal interface IInheritedDataLoaderRequirements<T>
            {
                T Convert<TItem>(TItem item) where TItem : class, new();
                event EventHandler? Changed;
                string this[int index] { get; set; }
                ref int Current { get; }
                string Name { get; init; }
                static abstract int GetCount();
            }

            public sealed partial class ExtendedDataLoader : IExtendedDataLoader
            {
                private int _current;

                int IInheritedDataLoaderRequirements<int>.Convert<TItem>(TItem item) => 0;

                event EventHandler? IInheritedDataLoaderRequirements<int>.Changed
                {
                    add { }
                    remove { }
                }

                string IInheritedDataLoaderRequirements<int>.this[int index]
                {
                    get => string.Empty;
                    set { }
                }

                ref int IInheritedDataLoaderRequirements<int>.Current => ref _current;

                string IInheritedDataLoaderRequirements<int>.Name
                {
                    get => string.Empty;
                    init { }
                }

                public static int GetCount() => 0;
            }

            internal static class Loaders
            {
                [DataLoader<IExtendedDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetExtendedAsync(
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
    public async Task AdditionalInterfaceMembers_Should_RaiseHC0127_When_PartialClassDoesNotImplementCompilerShapes()
    {
        // arrange
        const string source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IMethodDataLoader : IBatchDataLoader<int, string>
            {
                void Refresh();
            }

            internal interface IEventIndexerDataLoader : IBatchDataLoader<int, string>
            {
                event EventHandler? Changed;
                string this[int index] { get; set; }
            }

            internal interface IStaticDataLoader : IBatchDataLoader<int, string>
            {
                static abstract int GetCount();
            }

            internal interface IRefReturnDataLoader : IBatchDataLoader<int, string>
            {
                ref int Current { get; }
            }

            internal interface IGenericConstraintDataLoader : IBatchDataLoader<int, string>
            {
                void Configure<T>() where T : class, new();
            }

            internal interface IInitDataLoader : IBatchDataLoader<int, string>
            {
                string Name { get; init; }
            }

            internal static class Loaders
            {
                [DataLoader<IMethodDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetMethodAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IEventIndexerDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetEventIndexerAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IStaticDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetStaticAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IRefReturnDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetRefReturnAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IGenericConstraintDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetGenericConstraintAsync(IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IInitDataLoader>]
                internal static Task<IReadOnlyDictionary<int, string>> GetInitAsync(IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var snapshot = TestHelper.GetGeneratedSourceSnapshot([source], enableAnalyzers: true);

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AccessModifier_Should_NotRaiseHC0128_When_AttributesAreStackedOrNonPublicInterface()
    {
        // arrange
        const string source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class Loaders
            {
                [DataLoader]
                [DataLoader<IBatchDataLoader<int, string>>(
                    AccessModifier = DataLoaderAccessModifier.PublicInterface)]
                internal static Task<IReadOnlyDictionary<int, string>> GetOldAndGenericAsync(
                    IReadOnlyList<int> keys,
                    CancellationToken cancellationToken)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>]
                [DataLoader<IBatchDataLoader<int, string>>(
                    AccessModifier = DataLoaderAccessModifier.PublicInterface)]
                internal static Task<IReadOnlyDictionary<int, string>> GetMultipleGenericAsync(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader<IBatchDataLoader<int, string>>(
                    AccessModifier = DataLoaderAccessModifier.Public)]
                internal static Task<IReadOnlyDictionary<int, string>> GetPublicAsync(
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
