using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationInputTests
    {
        [Fact]
        public void CreateIntOperationType()
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
                        .Argument("test", a => a.Type<ComparableOperationInput<int>>()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferComparableOperationTypeFromFields()
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
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public short BarShort { get; set; }
            public int BarInt { get; set; }
            public long BarLong { get; set; }
            public float BarFloat { get; set; }
            public double BarDouble { get; set; }
            public decimal BarDecimal { get; set; }
            public short? BarShortNullable { get; set; }
            public int? BarIntNullable { get; set; }
            public long? BarLongNullable { get; set; }
            public float? BarFloatNullable { get; set; }
            public double? BarDoubleNullable { get; set; }
            public decimal? BarDecimalNullable { get; set; }
            public FooBar FooBar { get; set; }
        }

        public enum FooBar
        {
            Foo,
            Bar
        }
    }
}