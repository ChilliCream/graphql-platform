namespace HotChocolate.Types.Introspection;

public class BM25SearchProviderTests
{
    [Fact]
    public async Task SearchAsync_Should_ReturnResults_When_QueryMatchesField()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("product", first: 10, after: null, minScore: null);

        // assert
        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.True(r.Score >= 0f && r.Score <= 1f);
            Assert.NotNull(r.Cursor);
        });
    }

    [Fact]
    public async Task SearchAsync_Should_ReturnEmpty_When_NoMatch()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("xyznonexistent", first: 10, after: null, minScore: null);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Throw_When_FirstIsZero()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act & assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.SearchAsync("product", first: 0, after: null, minScore: null).AsTask());
    }

    [Fact]
    public async Task SearchAsync_Should_Throw_When_FirstIsNegative()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act & assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.SearchAsync("product", first: -1, after: null, minScore: null).AsTask());
    }

    [Fact]
    public async Task SearchAsync_Should_LimitResults_When_FirstIsSmall()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("product", first: 1, after: null, minScore: null);

        // assert
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_Should_NormalizeScores_When_ResultsReturned()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("product", first: 10, after: null, minScore: null);

        // assert
        Assert.NotEmpty(results);
        // The first result should have the highest score, normalized to 1.0.
        Assert.Equal(1.0f, results[0].Score);

        // All scores should be in [0, 1].
        Assert.All(results, r => Assert.InRange(r.Score!.Value, 0f, 1f));
    }

    [Fact]
    public async Task SearchAsync_Should_FilterByMinScore_When_MinScoreProvided()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("product", first: 100, after: null, minScore: 0.5f);

        // assert
        Assert.All(results, r => Assert.True(r.Score >= 0.5f));
    }

    [Fact]
    public async Task SearchAsync_Should_Paginate_When_AfterCursorProvided()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // Get first page.
        var firstPage = await provider.SearchAsync("product", first: 1, after: null, minScore: null);

        // act - Get second page using cursor.
        var secondPage = await provider.SearchAsync(
            "product", first: 1, after: firstPage[0].Cursor, minScore: null);

        // assert
        if (secondPage.Count > 0)
        {
            // The second page result should be different from the first.
            Assert.NotEqual(firstPage[0].Coordinate, secondPage[0].Coordinate);
        }
    }

    [Fact]
    public async Task SearchAsync_Should_SortByScoreDescending()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var results = await provider.SearchAsync("product", first: 100, after: null, minScore: null);

        // assert
        for (var i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Score >= results[i].Score);
        }
    }

    [Fact]
    public async Task SearchAsync_Should_BeThreadSafe_When_CalledConcurrently()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act - Trigger concurrent index builds.
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => provider.SearchAsync("product", first: 10, after: null, minScore: null).AsTask())
            .ToArray();

        var allResults = await Task.WhenAll(tasks);

        // assert - All should return the same results.
        for (var i = 1; i < allResults.Length; i++)
        {
            Assert.Equal(allResults[0].Count, allResults[i].Count);
        }
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_ReturnEmptyPath_When_CoordinateIsRootType()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Query"), maxPaths: 5);

        // assert
        Assert.NotEmpty(paths);
        // Path from root to itself should be short (just the type coordinate).
        Assert.True(paths[0].Count <= 1);
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_ReturnPath_When_TypeIsReachableFromRoot()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Product"), maxPaths: 5);

        // assert
        Assert.NotEmpty(paths);
        // The path should contain at least the Product type.
        Assert.True(paths[0].Count >= 1);
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_IncludeFieldCoordinate_When_CoordinateHasMember()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Product", "name"), maxPaths: 5);

        // assert
        Assert.NotEmpty(paths);
        // The path should start with the field coordinate.
        Assert.Equal(new SchemaCoordinate("Product", "name"), paths[0][0]);
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_ReturnEmpty_When_MaxPathsIsZero()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Product"), maxPaths: 0);

        // assert
        Assert.Empty(paths);
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_ReturnSortedByLength()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Product"), maxPaths: 10);

        // assert
        for (var i = 1; i < paths.Count; i++)
        {
            Assert.True(paths[i - 1].Count <= paths[i].Count);
        }
    }

    [Fact]
    public async Task GetPathsToRootAsync_Should_LimitResults_When_MaxPathsIsSmall()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act
        var paths = await provider.GetPathsToRootAsync(
            new SchemaCoordinate("Product"), maxPaths: 1);

        // assert
        Assert.True(paths.Count <= 1);
    }

    [Fact]
    public async Task SearchAsync_Should_ThrowArgumentNullException_When_QueryIsNull()
    {
        // arrange
        var schema = CreateTestSchema();
        var provider = new BM25SearchProvider(schema);

        // act & assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.SearchAsync(null!, first: 10, after: null, minScore: null).AsTask());
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_SchemaIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new BM25SearchProvider(null!));
    }

    private static Schema CreateTestSchema()
    {
        return SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("product")
                .Description("Gets a product")
                .Type<ProductType>()
                .Resolve(new Product("Test", 9.99m))
                .Argument("id", a => a.Type<NonNullType<IdType>>()))
            .AddType<ProductType>()
            .AddType<CategoryType>()
            .AddType<ProductStatusType>()
            .ModifyOptions(o =>
            {
                o.StrictValidation = false;
                o.EnableSemanticIntrospection = false;
            })
            .Create();
    }

    private record Product(string Name, decimal Price);

    private sealed class ProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Description("A product in the store");
            descriptor.Field(p => p.Name)
                .Description("The product name")
                .Type<NonNullType<StringType>>();
            descriptor.Field(p => p.Price)
                .Description("The product price")
                .Type<NonNullType<DecimalType>>();
            descriptor.Field("category")
                .Type<CategoryType>()
                .Resolve(new Category("Electronics"));
        }
    }

    private record Category(string Name);

    private sealed class CategoryType : ObjectType<Category>
    {
        protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
        {
            descriptor.Name("Category");
            descriptor.Description("A product category");
            descriptor.Field(c => c.Name)
                .Description("The category name")
                .Type<NonNullType<StringType>>();
        }
    }

    private enum ProductStatus
    {
        Active,
        Discontinued
    }

    private sealed class ProductStatusType : EnumType<ProductStatus>
    {
        protected override void Configure(IEnumTypeDescriptor<ProductStatus> descriptor)
        {
            descriptor.Name("ProductStatus");
            descriptor.Value(ProductStatus.Active).Name("ACTIVE");
            descriptor.Value(ProductStatus.Discontinued).Name("DISCONTINUED");
        }
    }
}
