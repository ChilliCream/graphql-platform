using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class ListFilterInputTests
{
    [Fact]
    public void Create_OperationType()
        => SchemaBuilder.New()
            .AddQueryType(t => t
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo")
                .Argument(
                    "test",
                    a => a.Type<ListFilterInputType<StringOperationFilterInputType>>()))
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
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FooFilterInput>()))
            .TryAddConvention<IFilterConvention>(
                _ => new FilterConvention(x => x.UseMock()))
            .AddFiltering()
            .Create()
            .MatchSnapshot();

    public class FooFilterInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor
                .Field("string")
                .Type<ListFilterInputType<StringOperationFilterInputType>>();
        }
    }

    public class Foo
    {
        public Baz[] Baz { get; set; } = [];

        public string[] StringArray { get; set; } = [];

        public string?[] StringNullableArray { get; set; } = [];

        public bool[] BooleanArray { get; set; } = [];

        public bool?[] BooleanNullableArray { get; set; } = [];

        public short[] BarShortArray { get; set; } = [];

        public int[] BarIntArray { get; set; } = [];

        public long[] BarLongArray { get; set; } = [];

        public float[] BarFloatArray { get; set; } = [];

        public double[] BarDoubleArray { get; set; } = [];

        public decimal[] BarDecimalArray { get; set; } = [];

        public short?[] BarShortNullableArray { get; set; } = [];

        public int?[] BarIntNullableArray { get; set; } = [];

        public long?[] BarLongNullableArray { get; set; } = [];

        public float?[] BarFloatNullableArray { get; set; } = [];

        public double?[] BarDoubleNullableArray { get; set; } = [];

        public decimal?[] BarDecimalNullableArray { get; set; } = [];

        public FooBar[] FooBarArray { get; set; } = [];
    }

    public class Baz
    {
        public string? StringProp { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar,
    }
}
