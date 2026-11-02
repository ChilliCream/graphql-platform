using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis.Fuzz;

internal static class GeneratedSchema
{
    public static async Task<string> CreateAsync()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddFiltering()
            .AddSorting()
            .AddCostAnalyzer()
            .BuildSchemaAsync();

        return schema.ToString();
    }

    public sealed class Query
    {
        [UsePaging(DefaultPageSize = 7)]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooks() => Array.Empty<Book>().AsQueryable();

        [UseOffsetPaging(DefaultPageSize = 9)]
        [UseFiltering<BookFilterInputType>]
        [UseSorting<BookSortInputType>]
        public IQueryable<Book> GetBooksOffset() => Array.Empty<Book>().AsQueryable();

        public IReadOnlyList<string> GetLabels() => [];
    }

    public sealed class Book
    {
        public required string Title { get; init; }

        public int Pages { get; init; }
    }

    public sealed class BookFilterInputType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(book => book.Title);
            descriptor.Field(book => book.Pages);
        }
    }

    public sealed class BookSortInputType : SortInputType<Book>
    {
        protected override void Configure(ISortInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(book => book.Title);
            descriptor.Field(book => book.Pages);
        }
    }
}
