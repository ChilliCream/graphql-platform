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
            .BuildRequestExecutorAsync();

        const string query = @"
        {
            foos(order: { createdUtc: DESC }) {
                createdUtc
            }
        }
        ";

        // act
        var result = await executor.ExecuteAsync(query);

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
            .BuildRequestExecutorAsync();

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
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchSnapshot();
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
            .BuildSchemaAsync();

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
            .BuildSchemaAsync();

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
            .BuildSchemaAsync();

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
