using GreenDonut.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Sorting_Should_Work_When_UsedWithNonNullDateTime()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string query = @"
        {
            foos(order: { createdUtc: DESC }) {
                createdUtc
            }
        }
        ";

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Sorting_Should_Work_When_Nested()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string query = @"
        {
            books(order: [{ author: { name: ASC, age: ASC }, title: DESC }]) {
                title
                author {
                    name
                }
            }
        }
        ";

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task QueryContext_Should_SortByExpressionField_When_FieldIsDefinedByExpression()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithExpressionSortField>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string query =
            """
            {
                items(order: [{ total: DESC }]) {
                    name
                }
            }
            """;

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "name": "C"
                  },
                  {
                    "name": "A"
                  },
                  {
                    "name": "B"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task QueryContext_Should_SortNullNavigationAsDefault_When_QueryIsInMemory()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNestedSortField>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string query =
            """
            {
                shelves(order: [{ book: { title: ASC } }]) {
                    name
                }
            }
            """;

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "shelves": [
                  {
                    "name": "empty"
                  },
                  {
                    "name": "first"
                  },
                  {
                    "name": "second"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData("total")]
    [InlineData("ratio")]
    [InlineData("upperTitle")]
    public async Task QueryContext_Should_SortNestedExpression_When_ParentIsNull(string field)
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNestedSortField>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var query =
            $$"""
            {
                shelves(order: [{ book: { {{field}}: DESC } }]) {
                    name
                }
            }
            """;

        // act
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "shelves": [
                  {
                    "name": "second"
                  },
                  {
                    "name": "first"
                  },
                  {
                    "name": "empty"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Sorting_Should_Not_Analyze_Ignored_Field_Type()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithIgnoredUnsupportedField>()
            .AddType<EntityWithIgnoredUnsupportedFieldType>()
            .AddSorting()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(schema);
        var sortType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["EntityWithIgnoredUnsupportedFieldSortInput"]);
        Assert.Collection(sortType.Fields, field => Assert.Equal("name", field.Name));
    }

    [Fact]
    public async Task Sorting_Should_Auto_Ignore_ObjectType_Ignored_Field()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithIgnoredField>()
            .AddType<EntityWithIgnoredFieldType>()
            .AddSorting()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(schema);
        var sortType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["EntityWithIgnoredFieldSortInput"]);
        Assert.Contains(sortType.Fields, field => field.Name == "id");
        Assert.Contains(sortType.Fields, field => field.Name == "name");
        Assert.DoesNotContain(sortType.Fields, field => field.Name == "internalData");
    }

    [Fact]
    public async Task Sorting_Should_Not_Ignore_Explicitly_Bound_Ignored_ObjectType_Field()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithExplicitIgnoredFieldSort>()
            .AddType<EntityWithIgnoredFieldType>()
            .AddSorting()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(schema);
        var sortType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["EntityWithIgnoredFieldSortInput"]);
        Assert.Contains(sortType.Fields, field => field.Name == "internalData");
    }
}

public class Query
{
    [UseSorting]
    public IEnumerable<Foo> Foos() =>
    [
        new Foo { CreatedUtc = new DateTime(2000, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2010, 1, 1, 1, 1, 1) },
        new Foo { CreatedUtc = new DateTime(2020, 1, 1, 1, 1, 1) }
    ];

    [UseSorting]
    public IEnumerable<Book> GetBooks(QueryContext<Book> queryContext)
        => new[]
            {
                new Book { Title = "Book5", Author = new Author { Age = 30, Name = "Author6" } },
                new Book { Title = "Book7", Author = new Author { Age = 34, Name = "Author17" } },
                new Book { Title = "Book1", Author = new Author { Age = 50, Name = "Author5" } }
            }
            .AsQueryable()
            .With(queryContext);
}

public class Foo
{
    [GraphQLType(typeof(NonNullType<DateType>))]
    public DateTime CreatedUtc { get; set; }
}

public class Author
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    [UseSorting]
    public Book[] Books { get; set; } = [];
}

public class Book
{
    public string Title { get; set; } = string.Empty;
    public Author? Author { get; set; }
}

public class QueryWithExpressionSortField
{
    [UseSorting(typeof(ItemSortType))]
    public IEnumerable<Item> GetItems(QueryContext<Item> queryContext, ISortingContext sorting)
    {
        sorting.Handled(true);

        return new[]
            {
                new Item("B", 1, 1),
                new Item("C", 10, 5),
                new Item("A", 2, 10)
            }
            .AsQueryable()
            .With(queryContext)
            .ToList();
    }
}

public class QueryWithNestedSortField
{
    [UseSorting(typeof(ShelfSortType))]
    public IEnumerable<Shelf> GetShelves(QueryContext<Shelf> queryContext, ISortingContext sorting)
    {
        sorting.Handled(true);

        return new[]
            {
                new Shelf("second", new ShelfBook("B", 20, 2)),
                new Shelf("empty", null),
                new Shelf("first", new ShelfBook("A", 10, 3))
            }
            .AsQueryable()
            .With(queryContext)
            .ToList();
    }
}

public record Shelf(string Name, ShelfBook? Book);

public record ShelfBook(string Title, int Pages, int Chapters);

public class ShelfSortType : SortInputType<Shelf>
{
    protected override void Configure(ISortInputTypeDescriptor<Shelf> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Book).Type<ShelfBookSortType>();
    }
}

public class ShelfBookSortType : SortInputType<ShelfBook>
{
    protected override void Configure(ISortInputTypeDescriptor<ShelfBook> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Title);
        descriptor.Field(t => t.Pages * t.Chapters).Name("total");
        descriptor.Field(t => t.Pages / t.Chapters).Name("ratio");
        descriptor.Field(t => t.Title.ToUpper()).Name("upperTitle");
    }
}

public record Item(string Name, decimal Price, decimal Quantity);

public class ItemSortType : SortInputType<Item>
{
    protected override void Configure(ISortInputTypeDescriptor<Item> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Price * t.Quantity).Name("total");
    }
}

public class QueryWithIgnoredUnsupportedField
{
    [UseSorting]
    public IQueryable<EntityWithIgnoredUnsupportedField> Entities()
        => new[] { new EntityWithIgnoredUnsupportedField { Name = "A" } }.AsQueryable();
}

public class QueryWithIgnoredField
{
    [UseSorting]
    public IQueryable<EntityWithIgnoredField> Entities() =>
        new[]
        {
            new EntityWithIgnoredField { Id = 1, Name = "A", InternalData = "A1" },
            new EntityWithIgnoredField { Id = 2, Name = "B", InternalData = "B1" }
        }.AsQueryable();
}

public class QueryWithExplicitIgnoredFieldSort
{
    [UseSorting(typeof(EntityWithIgnoredFieldSortType))]
    public IQueryable<EntityWithIgnoredField> Entities() =>
        new[]
        {
            new EntityWithIgnoredField { Id = 1, Name = "A", InternalData = "A1" },
            new EntityWithIgnoredField { Id = 2, Name = "B", InternalData = "B1" }
        }.AsQueryable();
}

public class EntityWithIgnoredUnsupportedField
{
    public string Name { get; set; } = string.Empty;
    public UnsupportedSpatialData? SpatialData { get; set; } = new();
}

public class UnsupportedSpatialData;

public class EntityWithIgnoredField
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? InternalData { get; set; }
}

public class EntityWithIgnoredUnsupportedFieldType : ObjectType<EntityWithIgnoredUnsupportedField>
{
    protected override void Configure(IObjectTypeDescriptor<EntityWithIgnoredUnsupportedField> descriptor)
    {
        descriptor.Ignore(t => t.SpatialData);
    }
}

public class EntityWithIgnoredFieldType : ObjectType<EntityWithIgnoredField>
{
    protected override void Configure(IObjectTypeDescriptor<EntityWithIgnoredField> descriptor)
    {
        descriptor.Ignore(t => t.InternalData);
    }
}

public class EntityWithIgnoredFieldSortType : SortInputType<EntityWithIgnoredField>
{
    protected override void Configure(ISortInputTypeDescriptor<EntityWithIgnoredField> descriptor)
    {
        descriptor.Field(t => t.InternalData);
    }
}
