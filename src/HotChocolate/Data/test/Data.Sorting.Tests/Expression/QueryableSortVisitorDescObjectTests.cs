using HotChocolate.Language;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableSortVisitorDescObjectTests
    : SortVisitorTestBase
{
    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, true)]
    public void Sort_BooleanDesc(params bool[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(1, 2, 3, 4)]
    [InlineData(4, 3, 2, 1)]
    public void Sort_IntDesc(params int[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData("a", "b", "c", "d")]
    [InlineData("d", "c", "b", "a")]
    public void Sort_StringDesc(params string[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(double.MaxValue, double.MinValue)]
    [InlineData(double.MinValue, double.MaxValue)]
    public void Sort_DoubleDesc(params double[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(TestEnum.Bar, TestEnum.Baz, TestEnum.Foo)]
    [InlineData(TestEnum.Foo, TestEnum.Bar, TestEnum.Baz)]
    public void Sort_EnumDesc(params TestEnum[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData("2018-01-01", "2019-01-01", "2020-01-01")]
    [InlineData("2020-01-01", "2019-01-01", "2018-01-01")]
    public void Sort_DateTimeDesc(params string[] dataObject)
    {
        Test_Desc(dataObject.Select(DateTime.Parse).ToArray());
    }

    [Theory]
    [InlineData(null, null, true, true, false, false)]
    [InlineData(false, false, true, true, null, null)]
    public void Sort_NullableBooleanDesc(params bool?[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(null, 2, 3, 4)]
    [InlineData(4, 3, 2, null)]
    public void Sort_NullableIntDesc(params int?[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(null, double.MaxValue, double.MinValue)]
    [InlineData(double.MinValue, double.MaxValue, null)]
    public void Sort_NullableDoubleDesc(params double?[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(null, TestEnum.Bar, TestEnum.Baz, TestEnum.Foo)]
    [InlineData(TestEnum.Foo, TestEnum.Bar, TestEnum.Baz, null)]
    public void Sort_NullableEnumDesc(params TestEnum?[] dataObject)
    {
        Test_Desc(dataObject);
    }

    [Theory]
    [InlineData(null, "2018-01-01", "2019-01-01", "2020-01-01")]
    [InlineData("2020-01-01", "2019-01-01", "2018-01-01", null)]
    public void Sort_NullableDateTimeDesc(params string?[] dataObject)
    {
        Test_Desc(
            dataObject.Select(x => x is null ? default : (DateTime?)DateTime.Parse(x))
                .ToArray());
    }

    [Theory]
    [InlineData("a", "b", "c", "d")]
    [InlineData("d", "c", "b", "a")]
    public void Sort_NullableStringDesc(params string[] data)
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { baz: DESC}}");
        var tester = CreateProviderTester(new FooNullableSortType<string>());
        var expected = data.OrderByDescending(x => x).ToArray();

        // act
        var func =
            tester.Build<FooNullable<string>>(value);

        // assert
        var inputs =
            data.Select(x => new FooNullable<string> { Bar = new BarNullable<string> { Baz = x, }, })
                .ToArray();
        var sorted = func(inputs);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], sorted[i].Bar?.Baz);
        }
    }

    [Theory]
    [InlineData("a", "b", "c", "d")]
    [InlineData("d", "c", "b", "a")]
    public void Sort_NullableStringDescWithNull(params string[] data)
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { baz: DESC}}");
        var tester = CreateProviderTester(new FooNullableSortType<string>());
        var expected = data.OrderByDescending(x => x).Append(null).ToArray();

        // act
        var func =
            tester.Build<FooNullable<string>>(value);

        // assert
        var inputs =
            data.Select(x => new FooNullable<string> { Bar = new BarNullable<string> { Baz = x, }, })
                .Append(new FooNullable<string> { Bar = null, })
                .ToArray();
        var sorted = func(inputs);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], sorted[i].Bar?.Baz);
        }
    }

    protected void Test_Desc<T>(params T[] data)
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { baz: DESC}}");
        var tester = CreateProviderTester(new FooSortType<T>());
        var expected = data.OrderByDescending(x => x).ToArray();

        // act
        var func = tester.Build<Foo<T>>(value);

        // assert
        var inputs = data.Select(x => new Foo<T> { Bar = new Bar<T> { Baz = x, }, }).ToArray();
        var sorted = func(inputs);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], sorted[i].Bar.Baz);
        }
    }

    public class Bar<T>
    {
        public T Baz { get; set; } = default!;
    }

    public class Foo<T>
    {
        public Bar<T> Bar { get; set; } = default!;
    }

    public class FooNullable<T>
        where T : class
    {
        public BarNullable<T>? Bar { get; set; }
    }

    public class BarNullable<T>
        where T : class
    {
        public T? Baz { get; set; }
    }

    public class FooSortType<T>
        : SortInputType<Foo<T>>
    {
    }

    public enum TestEnum
    {
        Foo = 0,
        Bar = 1,
        Baz = 2,
    }

    public class FooNullableSortType<T>
        : SortInputType<FooNullable<T>>
        where T : class
    {
    }
}
