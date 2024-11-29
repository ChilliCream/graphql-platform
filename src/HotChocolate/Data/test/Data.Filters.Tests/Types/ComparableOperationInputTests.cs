using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class ComparableOperationInputTests
{
    [Fact]
    public void Create_OperationType()
        => SchemaBuilder.New()
            .AddQueryType(t => t
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo")
                .Argument("test", a => a.Type<ComparableOperationFilterInputType<int>>()))
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
            .AddFiltering(compatibilityMode: true)
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Create_Implicit_Operation_Normalized()
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
            descriptor.Field("comparable").Type<ComparableOperationFilterInputType<int>>();
        }
    }

    public class Foo
    {
        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }

        public Uri BarUri { get; set; } = default!;

        public byte BarByte { get; set; } = default!;

        public Uri? BarUriNullable { get; set; }

        public short? BarShortNullable { get; set; }

        public int? BarIntNullable { get; set; }

        public long? BarLongNullable { get; set; }

        public float? BarFloatNullable { get; set; }

        public double? BarDoubleNullable { get; set; }

        public decimal? BarDecimalNullable { get; set; }

        public byte? BarByteNullable { get; set; } = default!;

        public FooBar FooBar { get; set; }

        public DateOnly DateOnly { get; set; }

        public DateOnly? DateOnlyNullable { get; set; }

        public TimeOnly TimeOnly { get; set; }

        public TimeOnly? TimeOnlyNullable { get; set; }
    }

    public enum FooBar
    {
        Foo,
        Bar,
    }
}
