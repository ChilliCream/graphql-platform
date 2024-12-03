using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorEnumTests : FilterVisitorTestBase
{
    [Fact]
    public void Create_EnumEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { eq: BAR }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarEnum = FooEnum.BAR, };
        Assert.True(func(a));

        var b = new Foo { BarEnum = FooEnum.BAZ, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_EnumNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { neq: BAR }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarEnum = FooEnum.BAZ, };
        Assert.True(func(a));

        var b = new Foo { BarEnum = FooEnum.BAR, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_EnumIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { in: [BAZ, QUX] }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarEnum = FooEnum.BAZ, };
        Assert.True(func(a));

        var b = new Foo { BarEnum = FooEnum.BAR, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_EnumNotIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { nin: [BAZ, QUX] }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarEnum = FooEnum.BAR, };
        Assert.True(func(a));

        var b = new Foo { BarEnum = FooEnum.BAZ, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_NullableEnumEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { eq: BAR }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarEnum = FooEnum.BAR, };
        Assert.True(func(a));

        var b = new FooNullable { BarEnum = FooEnum.BAZ, };
        Assert.False(func(b));

        var c = new FooNullable { BarEnum = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NullableEnumNotEqual_Expression()
    {
        // arrange
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { neq: BAR }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarEnum = FooEnum.BAZ, };
        Assert.True(func(a));

        var b = new FooNullable { BarEnum = FooEnum.BAR, };
        Assert.False(func(b));

        var c = new FooNullable { BarEnum = null, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_NullableEnumIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { in: [BAZ, QUX] }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarEnum = FooEnum.BAZ, };
        Assert.True(func(a));

        var b = new FooNullable { BarEnum = FooEnum.BAR, };
        Assert.False(func(b));

        var c = new FooNullable { BarEnum = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NullableEnumNotIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barEnum: { nin: [BAZ, QUX] }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarEnum = FooEnum.BAR, };
        Assert.True(func(a));

        var b = new FooNullable { BarEnum = FooEnum.BAZ, };
        Assert.False(func(b));

        var c = new FooNullable { BarEnum = null, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_NonNullOnNullableTypes_Attributes()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ nonNullOnNullableTypes: { eq: THIRD }}");
        var tester = CreateProviderTester(new FilterInputType<AttributeTest>());

        // act
        var func = tester.Build<AttributeTest>(value);

        // assert
        var a = new AttributeTest { NonNullOnNullableTypes = TestEnum.Third, };
        Assert.True(func(a));

        var b = new AttributeTest { NonNullOnNullableTypes = TestEnum.First, };
        Assert.False(func(b));

        var c = new AttributeTest { NonNullOnNullableTypes = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NonNullOnNullable_Attributes()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ nonNullOnNullable: { eq: THIRD }}");
        var tester = CreateProviderTester(new FilterInputType<AttributeTest>());

        // act
        var func = tester.Build<AttributeTest>(value);

        // assert
        var a = new AttributeTest { NonNullOnNullable = TestEnum.Third, };
        Assert.True(func(a));

        var b = new AttributeTest { NonNullOnNullable = TestEnum.First, };
        Assert.False(func(b));

        var c = new AttributeTest { NonNullOnNullable = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Overwrite_Enum_Filter_Type_With_Attribute()
    {
        // arrange
        // act
        var schema = CreateSchema(new FilterInputType<EntityWithTypeAttribute>());

        // assert
        schema.MatchSnapshot();
    }

    public class Foo
    {
        public FooEnum BarEnum { get; set; }
    }

    public class FooNullable
    {
        public FooEnum? BarEnum { get; set; }
    }

    public enum FooEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
    }

    public class EntityWithTypeAttribute
    {
        [GraphQLType(typeof(IntType))]
        public short? BarEnum { get; set; }
    }

    public class Entity
    {
        public short? BarEnum { get; set; }
    }

    public enum TestEnum
    {
        None,
        First,
        Second,
        Third,
    }

    public class AttributeTest
    {
        [GraphQLType(typeof(NonNullType<EnumType<TestEnum>>))]
        public TestEnum? NonNullOnNullableTypes { get; set; }

        [GraphQLNonNullType]
        public TestEnum? NonNullOnNullable { get; set; }

        [GraphQLType(typeof(EnumType<TestEnum>))]
        public TestEnum NullableOnNullableType { get; set; }

        [GraphQLNonNullType]
        public TestEnum NullableOnNNonullableType { get; set; }
    }
}
