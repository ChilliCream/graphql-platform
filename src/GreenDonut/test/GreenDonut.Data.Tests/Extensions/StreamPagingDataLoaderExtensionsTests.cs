using System.Linq.Expressions;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamPagingDataLoaderExtensionsTests
{
    [Fact]
    public void With_Should_ThrowArgumentNullException_When_DataLoaderIsNull()
    {
        // arrange
        IDataLoader<int, StreamPage<string>> dataLoader = null!;

        // act
        var exception = Record.Exception(() => dataLoader.With(new PagingArguments(first: 5)));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void With_Should_ReturnSameBranch_When_ArgumentsAreEquivalent()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, string>();
        var argumentsA = new PagingArguments(first: 5, after: "abc");
        var argumentsB = new PagingArguments(first: 5, after: "abc");

        // act
        var branchA = dataLoader.With(argumentsA);
        var branchB = dataLoader.With(argumentsB);

        // assert
        Assert.Same(branchA, branchB);
    }

    [Fact]
    public void With_Should_ReturnDifferentBranch_When_IncludeItemsDiffers()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, string>();
        var withItems = new PagingArguments(first: 5);
        var withoutItems = withItems with { IncludeItems = false };

        // act
        var branchWithItems = dataLoader.With(withItems);
        var branchWithoutItems = dataLoader.With(withoutItems);

        // assert
        Assert.NotSame(branchWithItems, branchWithoutItems);
    }

    [Fact]
    public void With_Should_ReturnDifferentBranch_When_IncludeItemsDiffers_And_NoOtherArgumentsSet()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, string>();
        var withItems = new PagingArguments();
        var withoutItems = withItems with { IncludeItems = false };

        // act
        var branchWithItems = dataLoader.With(withItems);
        var branchWithoutItems = dataLoader.With(withoutItems);

        // assert
        Assert.NotSame(branchWithItems, branchWithoutItems);
    }

    [Fact]
    public void With_Should_ReturnSameBranch_When_IncludeItemsExplicitlyTrueMatchesDefault()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, string>();
        var defaultArguments = new PagingArguments(first: 5);
        var explicitTrueArguments = defaultArguments with { IncludeItems = true };

        // act
        var defaultBranch = dataLoader.With(defaultArguments);
        var explicitTrueBranch = dataLoader.With(explicitTrueArguments);

        // assert
        Assert.Same(defaultBranch, explicitTrueBranch);
    }

    [Fact]
    public void Select_Should_ReturnSameDataLoader_When_SelectorIsNull()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();

        // act
        // Explicit type arguments disambiguate this call from the fully generic
        // GreenDonutSelectionDataLoaderExtensions.Select overload.
        var result = dataLoader.Select<int, Entity>(null);

        // assert
        Assert.Same(dataLoader, result);
    }

    [Fact]
    public void Select_Should_BranchDataLoader_When_SelectorProvided()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();

        // act
        // Explicit type arguments disambiguate this call from the fully generic
        // GreenDonutSelectionDataLoaderExtensions.Select overload.
        var branch = dataLoader.Select<int, Entity>(x => x);

        // assert
        Assert.NotSame(dataLoader, branch);
    }

    [Fact]
    public void Where_Should_ReturnSameDataLoader_When_PredicateIsNull()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();

        // act
        // Explicit type arguments disambiguate this call from the fully generic
        // GreenDonutPredicateDataLoaderExtensions.Where overload.
        var result = dataLoader.Where<int, Entity>(null);

        // assert
        Assert.Same(dataLoader, result);
    }

    [Fact]
    public void Where_Should_BranchDataLoader_When_PredicateProvided()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();

        // act
        var branch = dataLoader.Where(x => x.Name == "abc");

        // assert
        Assert.NotSame(dataLoader, branch);
    }

    [Fact]
    public void OrderBy_Should_ReturnSameDataLoader_When_SortDefinitionIsNull()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();

        // act
        // Explicit type arguments disambiguate this call from the fully generic
        // GreenDonutSortingDataLoaderExtensions.OrderBy overload.
        var result = dataLoader.OrderBy<int, Entity>(null);

        // assert
        Assert.Same(dataLoader, result);
    }

    [Fact]
    public void OrderBy_Should_BranchDataLoader_When_SortDefinitionProvided()
    {
        // arrange
        var dataLoader = new StreamPageDataLoader<int, Entity>();
        var sortDefinition = new SortDefinition<Entity>().AddAscending(x => x.Name);

        // act
        var branch = dataLoader.OrderBy(sortDefinition);

        // assert
        Assert.NotSame(dataLoader, branch);
    }

    [Fact]
    public async Task FetchAsync_Should_ReceiveBranchStateAsPagingArgumentsAndQueryContext_When_LoaderIsBranched()
    {
        // arrange
        var dataLoader = new CapturingStreamPageDataLoader<int, Entity>();
        var pagingArguments = new PagingArguments(first: 5, after: "abc");
        Expression<Func<Entity, Entity>> selector = x => x;
        Expression<Func<Entity, bool>> predicate = x => x.Name == "abc";
        var sortDefinition = new SortDefinition<Entity>().AddAscending(x => x.Name);
        var expectedContext = new QueryContext<Entity>(selector, predicate, sortDefinition);
        var branch = dataLoader
            .With(pagingArguments)
            .Select(selector)
            .Where(predicate)
            .OrderBy(sortDefinition);

        // act
        await branch.LoadAsync(1, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(pagingArguments, dataLoader.CapturedPagingArguments);
        Assert.Equal(expectedContext.ComputeHash(), dataLoader.CapturedQueryContext!.ComputeHash());
    }

    [Theory]
    [InlineData(PagingFlag.IncludeTotalCount)]
    [InlineData(PagingFlag.EnableRelativeCursors)]
    [InlineData(PagingFlag.NullOrdering)]
    public async Task FetchAsync_Should_ReceiveEachBranchOwnPagingArguments_When_BranchesDifferOnlyInOneFlag(
        PagingFlag flag)
    {
        // arrange
        var dataLoader = new CapturingStreamPageDataLoader<int, Entity>();
        var baseArguments = new PagingArguments(first: 5);
        var variantArguments = flag switch
        {
            PagingFlag.IncludeTotalCount => baseArguments with { IncludeTotalCount = true },
            PagingFlag.EnableRelativeCursors => baseArguments with { EnableRelativeCursors = true },
            PagingFlag.NullOrdering => baseArguments with { NullOrdering = NullOrdering.NativeNullsFirst },
            _ => throw new ArgumentOutOfRangeException(nameof(flag))
        };
        var baseBranch = dataLoader.With(baseArguments);
        var variantBranch = dataLoader.With(variantArguments);

        // act
        await baseBranch.LoadAsync(1, TestContext.Current.CancellationToken);
        var pagingArgumentsFromBaseBranch = dataLoader.CapturedPagingArguments;
        await variantBranch.LoadAsync(2, TestContext.Current.CancellationToken);
        var pagingArgumentsFromVariantBranch = dataLoader.CapturedPagingArguments;

        // assert
        Assert.NotSame(baseBranch, variantBranch);
        Assert.Equal(baseArguments, pagingArgumentsFromBaseBranch);
        Assert.Equal(variantArguments, pagingArgumentsFromVariantBranch);
    }

    public enum PagingFlag
    {
        IncludeTotalCount,
        EnableRelativeCursors,
        NullOrdering
    }

    public class Entity
    {
        public string Name { get; set; } = null!;
    }

    private sealed class StreamPageDataLoader<TKey, TValue>()
        : DataLoaderBase<TKey, StreamPage<TValue>>(new AutoBatchScheduler(), new DataLoaderOptions())
        where TKey : notnull
    {
        protected override ValueTask FetchAsync(
            IReadOnlyList<TKey> keys,
            Memory<Result<StreamPage<TValue>?>> results,
            DataLoaderFetchContext<StreamPage<TValue>> context,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("This DataLoader only verifies branching behavior.");
    }

    // Records the paging state a fetch actually receives so tests can assert the branch's
    // paging arguments and query context reach FetchAsync unchanged.
    private sealed class CapturingStreamPageDataLoader<TKey, TValue>()
        : DataLoaderBase<TKey, StreamPage<TValue>>(new AutoBatchScheduler(), new DataLoaderOptions())
        where TKey : notnull
    {
        public PagingArguments? CapturedPagingArguments { get; private set; }

        public QueryContext<TValue>? CapturedQueryContext { get; private set; }

        protected override ValueTask FetchAsync(
            IReadOnlyList<TKey> keys,
            Memory<Result<StreamPage<TValue>?>> results,
            DataLoaderFetchContext<StreamPage<TValue>> context,
            CancellationToken cancellationToken)
        {
            CapturedPagingArguments = context.GetPagingArguments();
            CapturedQueryContext = context.GetQueryContext<StreamPage<TValue>, TValue>();

            var span = results.Span;
            for (var i = 0; i < keys.Count; i++)
            {
                span[i] = StreamPage<TValue>.Empty;
            }

            return default;
        }
    }
}
