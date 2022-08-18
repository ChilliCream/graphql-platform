using HotChocolate.Types;
using CookieCrumble;

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
        public Baz[] Baz { get; set; } = new Baz[0];

        public string[] StringArray { get; set; } = new string[0];

        public string?[] StringNullableArray { get; set; } = new string?[0];

        public bool[] BooleanArray { get; set; } = new bool[0];

        public bool?[] BooleanNullableArray { get; set; } = new bool?[0];

        public short[] BarShortArray { get; set; } = new short[0];

        public int[] BarIntArray { get; set; } = new int[0];

        public long[] BarLongArray { get; set; } = new long[0];

        public float[] BarFloatArray { get; set; } = new float[0];

        public double[] BarDoubleArray { get; set; } = new double[0];

        public decimal[] BarDecimalArray { get; set; } = new decimal[0];

        public short?[] BarShortNullableArray { get; set; } = new short?[0];

        public int?[] BarIntNullableArray { get; set; } = new int?[0];

        public long?[] BarLongNullableArray { get; set; } = new long?[0];

        public float?[] BarFloatNullableArray { get; set; } = new float?[0];

        public double?[] BarDoubleNullableArray { get; set; } = new double?[0];

        public decimal?[] BarDecimalNullableArray { get; set; } = new decimal?[0];

        public FooBar[] FooBarArray { get; set; } = new FooBar[0];
    }

    public class Baz
    {
        public string? StringProp { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar
    }
}
