using System;
using HotChocolate.Types;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters;

public class ComparableOperationInputTests
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
                    .Resolve("foo")
                    .Argument("test", a => a.Type<ComparableOperationFilterInputType<int>>()))
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
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<Foo>>()))
            .AddFiltering()
            .Create();

        // assert
#if NET6_0_OR_GREATER
        schema.ToString().MatchSnapshot(new SnapshotNameExtension("NET6"));
#else
        schema.ToString().MatchSnapshot();
#endif
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
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FooFilterInput>()))
            .TryAddConvention<IFilterConvention>(
                (sp) => new FilterConvention(x => x.UseMock()))
            .AddFiltering()
            .Create();

        // assert
        schema.ToString().MatchSnapshot(new SnapshotNameExtension("NET6"));
    }

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

        public short? BarShortNullable { get; set; }

        public int? BarIntNullable { get; set; }

        public long? BarLongNullable { get; set; }

        public float? BarFloatNullable { get; set; }

        public double? BarDoubleNullable { get; set; }

        public decimal? BarDecimalNullable { get; set; }

        public FooBar FooBar { get; set; }

#if NET6_0_OR_GREATER
        public DateOnly DateOnly { get; set; }

        public DateOnly? DateOnlyNullable { get; set; }

        public TimeOnly TimeOnly { get; set; }

        public TimeOnly? TimeOnlyNullable { get; set; }
#endif
    }

    public enum FooBar
    {
        Foo,
        Bar
    }
}
