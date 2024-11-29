using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests;

public class ExtensionTests
{
    [Fact]
    public void Convention_DefaultScope_Extensions_Enum()
    {
        // arrange
        // act
        var convention = new SortConvention(
            x => x.UseMock()
                .ConfigureEnum<DefaultSortEnumType>(y => y.Operation(123))
                .Operation(123).Name("test"));

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar")
                    .Argument("test", x => x.Type<TestSort>()));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Convention_DefaultScope_Extensions_Enum_Merge()
    {
        // arrange
        // act
        var convention = new SortConvention(
            x => x.UseMock()
                .ConfigureEnum<DefaultSortEnumType>(
                    y => y.Operation(DefaultSortOperations.Ascending).Description("asc")));

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar")
                    .Argument("test", x => x.Type<TestSort>()));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Convention_DefaultScope_Extensions()
    {
        // arrange
        // act
        var convention = new SortConvention(
            x => x.UseMock()
                .Configure<TestSort>(
                    y => y.Field("foo").Type<DefaultSortEnumType>())
                .Operation(123).Name("test"));

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar")
                    .Argument("test", x => x.Type<TestSort>()));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Convention_DefaultScope_Extensions_Merge()
    {
        // arrange
        // act
        var convention = new SortConvention(
            x => x.UseMock()
                .Configure<TestSort>(
                    y => y.Field("test").Description("test"))
                .Operation(123).Name("test"));

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar")
                    .Argument("test", x => x.Type<TestSort>()));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseSorting()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseSorting());

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseSorting_Generic_RuntimeType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseSorting<Bar>());

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseSorting_Generic_SchemaType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseSorting<BarSortType>());

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseSorting_Type_RuntimeType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseSorting(typeof(Bar)));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void ObjectField_UseSorting_Type_SchemaType()
    {
        // arrange
        // act
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>(
                c =>
                    c.Field(x => x.GetFoos()).UseSorting(typeof(BarSortType)));

        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    private sealed class TestSort : SortInputType
    {
        protected override void Configure(ISortInputTypeDescriptor descriptor)
        {
            descriptor.Field("test").Type<DefaultSortEnumType>();
        }
    }

    public class BarSortType : SortInputType<Bar>
    {
        protected override void Configure(ISortInputTypeDescriptor<Bar> descriptor)
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
