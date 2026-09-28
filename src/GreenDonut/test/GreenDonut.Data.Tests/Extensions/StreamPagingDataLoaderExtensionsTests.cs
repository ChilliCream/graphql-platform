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
}
