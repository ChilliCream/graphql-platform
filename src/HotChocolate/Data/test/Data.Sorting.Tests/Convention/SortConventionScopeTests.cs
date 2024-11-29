using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public class SortConventionScopeTests
{
    [Fact]
    public void SortConvention_Should_Work_When_ConfiguredWithAttributes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddConvention<ISortConvention, BarSortConvention>("Bar")
            .AddQueryType<Query1>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Work_When_ConfiguredWithType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddConvention<ISortConvention, BarSortConvention>("Bar")
            .AddQueryType<QueryType>()
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("foos").Resolve(new Foo[0].AsQueryable()).UseSorting();
            descriptor.Field("foosBar").Resolve(new Foo[0].AsQueryable()).UseSorting("Bar");
        }
    }

    public class Query1
    {
        [UseSorting]
        public IQueryable<Foo> Foos() => new Foo[0].AsQueryable();

        [UseSorting(Scope = "Bar")]
        public IQueryable<Foo> FoosBar() => new Foo[0].AsQueryable();
    }

    public class BarSortConvention : SortConvention
    {
        protected override void Configure(ISortConventionDescriptor descriptor)
        {
            descriptor.AddDefaults();
            descriptor.Operation(DefaultSortOperations.Ascending).Name("Different");
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class FooSortType : SortInputType<Foo>
    {
        protected override void Configure(
            ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
