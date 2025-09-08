namespace HotChocolate.Data.Filters;

public class MethodOperationInputTests
{
    [Fact]
    public void Create_Explicit_Operation()
    {
        // arrange
        var convention = new FilterConvention(
            x =>
            {
                x.UseMock();
                x.Operation(155).Name("SimpleMethod");
                x.Operation(156).Name("ComplexMethod");
            });

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryExplicit>()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Implicit_Operation()
    {
        // arrange
        var convention = new FilterConvention(x => x.UseMock().Operation(155).Name("Method155"));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class QueryExplicit
    {
        [UseFiltering(Type = typeof(FooOperationType))]
        public IQueryable<Foo> Foos() => new Foo[0].AsQueryable();
    }

    public class Query
    {
        [UseFiltering]
        public IQueryable<Foo> Foos() => new Foo[0].AsQueryable();
    }

    public class FooOperationType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Name("Test");
            descriptor
                .Operation(155)
                .Name("TestSimpleMethod")
                .Type<BooleanOperationFilterInputType>();
            descriptor
                .Operation(156)
                .Name("TestComplexMethod")
                .Type<FilterInputType<Bar>>();
        }
    }

    public class Foo
    {
        public bool SimpleMethod() => true;

        public Bar ComplexMethod() => new Bar();
    }

    public class Bar
    {
        public string StringOperation { get; set; } = "";
    }
}
