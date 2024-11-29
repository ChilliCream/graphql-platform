using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests;

public class ExtensionTests
{
    [Fact]
    public void Convention_DefaultScope_Extensions()
    {
        // arrange
        // act
        var convention = new FilterConvention(
            x => x.UseMock()
                .Configure<StringOperationFilterInputType>(y => y
                    .Operation(DefaultFilterOperations.Like)
                    .Type<StringType>())
                .Operation(DefaultFilterOperations.Like)
                .Name("like"));

        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .TryAddTypeInterceptor<FilterTypeInterceptor>()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar")
                .Argument("test", x => x.Type<TestFilter>()));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseFiltering());

        // act
        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering_Generic_RuntimeType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(c => c
                .Field(x => x.GetFoos())
                .UseFiltering<Bar>());

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering_Generic_SchemaType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(c => c.Field(x => x.GetFoos()).UseFiltering<BarFilterInput>());

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering_Type_RuntimeType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(c => c.Field(x => x.GetFoos()).UseFiltering(typeof(Bar)));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering_Type_SchemaType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseFiltering(typeof(BarFilterInput)));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseFiltering_Descriptor()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<Query>(c => c
                .Field(a => a.GetFoos())
                .UseFiltering<Bar>(b => b
                    .Name("foo").Field(d => d.Foo)));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    public class TestFilter : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Field("test").Type<StringOperationFilterInputType>();
        }
    }

    public class BarFilterInput : FilterInputType<Bar>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Bar> descriptor)
        {
            descriptor.BindFieldsExplicitly().Field(m => m.Foo);
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class Bar
    {
        public string Foo { get; set; } = default!;
    }

    public class Query
    {
        public IQueryable<Foo> GetFoos() => throw new InvalidOperationException();
    }
}
