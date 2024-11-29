using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class EnumOperationInputTests
{
    [Fact]
    public void Create_OperationType()
        => SchemaBuilder.New()
            .AddQueryType(t => t
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo")
                .Argument("test", a => a.Type<EnumOperationFilterInputType<FooBar>>()))
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Implicit_Operation()
        => SchemaBuilder.New()
            .AddQueryType(t => t
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo")
                .Argument("test", a => a.Type<FilterInputType<Foo>>()))
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
                .Argument("test", a => a.Type<FooFilterInput>()))
            .TryAddConvention<IFilterConvention>(_ => new FilterConvention(x => x.UseMock()))
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    public class FooFilterInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("comparable").Type<EnumOperationFilterInputType<FooBar>>();
        }
    }

    public class Foo
    {
        public FooBar FooBar { get; set; }

        public FooBar? FooBarNullable { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar,
    }
}
