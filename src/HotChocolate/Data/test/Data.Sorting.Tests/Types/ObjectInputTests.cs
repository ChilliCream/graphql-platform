using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public class ObjectInputTests
{
    [Fact]
    public void Create_Implicit_Operation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<SortInputType<Bar>>()))
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Explicit_Operation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<ExplicitSortType>()))
            .TryAddConvention<ISortConvention>(
                (sp) => new SortConvention(x => x.UseMock()))
            .AddSorting()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class ExplicitSortType : SortInputType
    {
        protected override void Configure(ISortInputTypeDescriptor descriptor)
        {
            descriptor.Field("comparable").Type<SortInputType<Bar>>();
        }
    }

    public class Bar
    {
        public Foo Foo { get; set; } = default!;

        public Foo? FooNullable { get; set; }

        public List<Foo> FooList { get; set; } = default!;
    }

    public class Foo
    {
        public short BarShort { get; set; }
    }
}
