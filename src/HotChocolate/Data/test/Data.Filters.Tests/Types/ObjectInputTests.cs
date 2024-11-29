using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class ObjectInputTests
{
    [Fact]
    public void Create_Implicit_Operation()
        => SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<Bar>>()))
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Explicit_Operation()
        => SchemaBuilder.New()
            .AddQueryType(t => t
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo")
                .Argument("test", a => a.Type<ExplicitFilterInput>()))
            .TryAddConvention<IFilterConvention>(_ => new FilterConvention(x => x.UseMock()))
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    public class ExplicitFilterInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("comparable").Type<FilterInputType<Bar>>();
        }
    }

    public class Bar
    {
        public Foo? Foo { get; set; }

        public Foo? FooNullable { get; set; }
    }

    public class Foo
    {
        public short BarShort { get; set; }
    }
}
