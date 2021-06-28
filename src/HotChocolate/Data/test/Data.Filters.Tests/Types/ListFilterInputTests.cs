using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class ListFilterInputTests
    {
        [Fact]
        public void Create_OperationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument(
                            "test",
                            a => a.Type<ListFilterInputType<StringOperationFilterInputType>>()))
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Operation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<FilterInputType<Foo>>()))
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Operation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<FooFilterInput>()))
                .TryAddConvention<IFilterConvention>(
                    (sp) => new FilterConvention(x => x.UseMock()))
                .AddFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

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
            public string StringProp { get; set; }
        }

        public enum FooBar
        {
            Foo,
            Bar
        }
    }
}
