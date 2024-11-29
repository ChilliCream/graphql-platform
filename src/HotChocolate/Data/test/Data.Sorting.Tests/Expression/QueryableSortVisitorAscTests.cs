using HotChocolate.Language;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableSortVisitorAscTests
    : SortVisitorTestBase
{
    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, true)]
    public void Sort_BooleanAsc(params bool[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(1, 2, 3, 4)]
    [InlineData(4, 3, 2, 1)]
    public void Sort_IntAsc(params int[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData("a", "b", "c", "d")]
    [InlineData("d", "c", "b", "a")]
    public void Sort_StringAsc(params string[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(double.MaxValue, double.MinValue)]
    [InlineData(double.MinValue, double.MaxValue)]
    public void Sort_DoubleAsc(params double[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(TestEnum.Bar, TestEnum.Baz, TestEnum.Foo)]
    [InlineData(TestEnum.Foo, TestEnum.Bar, TestEnum.Baz)]
    public void Sort_EnumAsc(params TestEnum[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData("2018-01-01", "2019-01-01", "2020-01-01")]
    [InlineData("2020-01-01", "2019-01-01", "2018-01-01")]
    public void Sort_DateTimeAsc(params string[] dataObject)
    {
        Test_Asc(dataObject.Select(DateTime.Parse).ToArray());
    }

    [Theory]
    [InlineData(null, null, true, true, false, false)]
    [InlineData(false, false, true, true, null, null)]
    public void Sort_NullableBooleanAsc(params bool?[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(null, 2, 3, 4)]
    [InlineData(4, 3, 2, null)]
    public void Sort_NullableIntAsc(params int?[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(null, double.MaxValue, double.MinValue)]
    [InlineData(double.MinValue, double.MaxValue, null)]
    public void Sort_NullableDoubleAsc(params double?[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(null, TestEnum.Bar, TestEnum.Baz, TestEnum.Foo)]
    [InlineData(TestEnum.Foo, TestEnum.Bar, TestEnum.Baz, null)]
    public void Sort_NullableEnumAsc(params TestEnum?[] dataObject)
    {
        Test_Asc(dataObject);
    }

    [Theory]
    [InlineData(null, "2018-01-01", "2019-01-01", "2020-01-01")]
    [InlineData("2020-01-01", "2019-01-01", "2018-01-01", null)]
    public void Sort_NullableDateTimeAsc(params string?[] dataObject)
    {
        Test_Asc(
            dataObject.Select(x => x is null ? default : (DateTime?)DateTime.Parse(x))
                .ToArray());
    }

    [Theory]
    [InlineData("a", "b", "c", "d")]
    [InlineData("d", "c", "b", "a")]
    public void Sort_NullableStringAsc(params string[] data)
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: ASC}");
        var tester = CreateProviderTester(new FooNullableSortType<string>());
        var expected = data.OrderBy(x => x).ToArray();

        // act
        var func =
            tester.Build<FooNullable<string>>(value);

        // assert
        var inputs =
            data.Select(x => new FooNullable<string> { Bar = x, }).ToArray();
        var sorted = func(inputs);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], sorted[i].Bar);
        }
    }

    protected void Test_Asc<T>(params T[] data)
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: ASC}");
        var tester = CreateProviderTester(new FooSortType<T>());
        var expected = data.OrderBy(x => x).ToArray();

        // act
        var func = tester.Build<Foo<T>>(value);

        // assert
        var inputs = data.Select(x => new Foo<T> { Bar = x, }).ToArray();
        var sorted = func(inputs);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], sorted[i].Bar);
        }
    }

    public class Foo<T>
    {
        public T? Bar { get; set; }
    }

    public class FooNullable<T>
        where T : class
    {
        public T? Bar { get; set; }
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
