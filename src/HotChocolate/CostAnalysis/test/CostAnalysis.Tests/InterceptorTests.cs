using HotChocolate.CostAnalysis.Types;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class InterceptorTests
{
    [Fact]
    public async Task FilterArgument_Gets_FilterArgumentCost_And_SortArgument_Gets_SortArgumentCost()
    {
        // arrange
        var schema = await CreateSchemaAsync(
            o =>
            {
                o.Filtering.DefaultFilterArgumentCost = 7.0;
                o.Sorting.DefaultSortArgumentCost = 13.0;
            });

        var field = QueryField(schema);

        // act
        var whereCost = field.Arguments["where"]
            .Directives.Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();
        var orderCost = field.Arguments["order"]
            .Directives.Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(7.0, whereCost.Weight);
        Assert.Equal(13.0, orderCost.Weight);
    }

    [Fact]
    public async Task FilterArgument_With_Own_CostDirective_Keeps_It()
    {
        // arrange
        // The @cost directive is attached to the filter argument's own configuration right
        // after UseFiltering() creates it, simulating a user-supplied directive on the
        // argument itself, before CostTypeInterceptor ever looks at it.
        var schema = await CreateSchemaAsync(
            o => o.Filtering.DefaultFilterArgumentCost = 7.0,
            configureField: f => f.Extend().OnBeforeCreate((ctx, def) =>
            {
                var filterArgument = def.Arguments.Single();
                filterArgument.AddDirective(new CostDirective(99.0), ctx.TypeInspector);
            }),
            configureFieldBeforeSorting: true);

        var field = QueryField(schema);

        // act
        var whereCost = field.Arguments["where"]
            .Directives.Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(99.0, whereCost.Weight);
    }

    [Fact]
    public async Task Field_CostDirective_Does_Not_Suppress_ArgumentDefaults()
    {
        // arrange
        var schema = await CreateSchemaAsync(
            o => o.Sorting.DefaultSortArgumentCost = 13.0,
            configureField: f => f.Cost(50.0));

        var field = QueryField(schema);

        // act
        var orderCost = field.Arguments["order"]
            .Directives.Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(13.0, orderCost.Weight);
    }

    private static ObjectField QueryField(Schema schema)
        => schema.Types.GetType<ObjectType>(OperationTypeNames.Query).Fields["books"];

    private static async Task<Schema> CreateSchemaAsync(
        Action<CostOptions> configureOptions,
        Action<IObjectFieldDescriptor>? configureField = null,
        bool configureFieldBeforeSorting = false)
        => await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d =>
            {
                var field = d.Field("books")
                    .Type<ListType<ObjectType<Book>>>()
                    .UseFiltering<BookFilterInputType>();

                if (configureFieldBeforeSorting)
                {
                    configureField?.Invoke(field);
                }

                field = field.UseSorting<BookSortInputType>().Resolve(_ => new List<Book>());

                if (!configureFieldBeforeSorting)
                {
                    configureField?.Invoke(field);
                }
            })
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(configureOptions)
            .BuildSchemaAsync();

    public sealed class Book
    {
        public required string Title { get; set; }
    }

    public sealed class BookFilterInputType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Title);
        }
    }

    public sealed class BookSortInputType : SortInputType<Book>
    {
        protected override void Configure(ISortInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Title);
        }
    }
}
