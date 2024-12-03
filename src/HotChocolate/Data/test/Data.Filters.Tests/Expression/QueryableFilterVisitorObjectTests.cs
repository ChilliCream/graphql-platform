using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorObjectTests : FilterVisitorTestBase
{
    [Fact]
    public void Create_ObjectShortEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barShort: { eq: 12 }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarShort = 12, }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarShort = 13, }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectShortIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barShort: { in: [13, 14] }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarShort = 13, }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarShort = 12, }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barShort: { eq: 12 }}}");
        var tester = CreateProviderTester(new BarNullableFilterInput());

        // act
        var func = tester.Build<BarNullable>(value);

        // assert
        var a = new BarNullable { Foo = new FooNullable { BarShort = 12, }, };
        Assert.True(func(a));

        var b = new BarNullable { Foo = new FooNullable { BarShort = 13, }, };
        Assert.False(func(b));

        var c = new BarNullable { Foo = new FooNullable { BarShort = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ObjectNullableShortIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barShort: { in: [13, 14] }}}");
        var tester = CreateProviderTester(new BarNullableFilterInput());

        // act
        var func = tester.Build<BarNullable>(value);

        // assert
        var a = new BarNullable { Foo = new FooNullable { BarShort = 13, }, };
        Assert.True(func(a));

        var b = new BarNullable { Foo = new FooNullable { BarShort = 12, }, };
        Assert.False(func(b));

        var c = new BarNullable { Foo = new FooNullable { BarShort = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ObjectBooleanEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barBool: { eq: true }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarBool = true, }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarBool = false, }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectNullableBooleanEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barBool: { eq: true }}}");
        var tester = CreateProviderTester(new BarNullableFilterInput());

        // act
        var func = tester.Build<BarNullable>(value);

        // assert
        var a = new BarNullable { Foo = new FooNullable { BarBool = true, }, };
        Assert.True(func(a));

        var b = new BarNullable { Foo = new FooNullable { BarBool = false, }, };
        Assert.False(func(b));

        var c = new BarNullable { Foo = new FooNullable { BarBool = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ObjectEnumEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barEnum: { eq: BAR }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarEnum = BarEnum.BAR, }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarEnum = BarEnum.BAZ, }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectEnumIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barEnum: { in: [BAZ, QUX] }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarEnum = BarEnum.BAZ, }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarEnum = BarEnum.BAR, }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectNullableEnumEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barEnum: { eq: BAR }}}");
        var tester = CreateProviderTester(new BarNullableFilterInput());

        // act
        var func = tester.Build<BarNullable>(value);

        // assert
        var a = new BarNullable { Foo = new FooNullable { BarEnum = BarEnum.BAR, }, };
        Assert.True(func(a));

        var b = new BarNullable { Foo = new FooNullable { BarEnum = BarEnum.BAZ, }, };
        Assert.False(func(b));

        var c = new BarNullable { Foo = new FooNullable { BarEnum = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ObjectNullableEnumIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barEnum: { in: [BAZ, QUX] }}}");
        var tester = CreateProviderTester(new BarNullableFilterInput());

        // act
        var func = tester.Build<BarNullable>(value);

        // assert
        var a = new BarNullable { Foo = new FooNullable { BarEnum = BarEnum.BAZ, }, };
        Assert.True(func(a));

        var b = new BarNullable { Foo = new FooNullable { BarEnum = BarEnum.BAR, }, };
        Assert.False(func(b));

        var c = new BarNullable { Foo = new FooNullable { BarEnum = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ObjectStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barString: { eq:\"a\" }}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarString = "a", }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarString = "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ObjectStringIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { barString: { in:[\"a\", \"c\"]}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { BarString = "a", }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { BarString = "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ScalarArraySomeStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { scalarArray: {some: { eq: \"a\" }}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { ScalarArray = ["c", "d", "a",], }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { ScalarArray = ["c", "d", "b",], }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ScalarArrayAnyStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { scalarArray: {any: true}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar { Foo = new Foo { ScalarArray = ["c", "d", "a",], }, };
        Assert.True(func(a));

        var b = new Bar { Foo = new Foo { ScalarArray = [], }, };
        Assert.False(func(b));

        var c = new Bar { Foo = new Foo { ScalarArray = null, }, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ArrayObjectNestedArraySomeStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { objectArray: {some: { foo: {scalarArray: {some: { eq: \"a\" }}}}}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                ScalarArray = ["c", "d", "a",],
                            },
                        },
                ],
            },
        };
        Assert.True(func(a));

        var b = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                ScalarArray = ["c", "d", "b",],
                            },
                        },
                ],
            },
        };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { objectArray: {some: { foo: {scalarArray: {any: true}}}}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                ScalarArray = ["c", "d", "a",],
                            },
                        },
                ],
            },
        };
        Assert.True(func(a));

        var b = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                ScalarArray = [],
                            },
                        },
                ],
            },
        };
        Assert.False(func(b));

        var c = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                ScalarArray = null,
                            },
                        },
                ],
            },
        };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ArrayObjectStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ foo: { objectArray: {some: {foo: { barString: { eq: \"a\"}}}}}}");
        var tester = CreateProviderTester(new BarFilterInput());

        // act
        var func = tester.Build<Bar>(value);

        // assert
        var a = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                BarString = "a",
                            },
                        },
                ],
            },
        };
        Assert.True(func(a));

        var b = new Bar
        {
            Foo = new Foo
            {
                ObjectArray =
                [
                    new Bar {
                            Foo = new Foo {
                                BarString = "b",
                            },
                        },
                ],
            },
        };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_Interface_FilterExpression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ test: {prop: { eq: \"a\"}}}");
        var tester = CreateProviderTester(new FilterInputType<BarInterface>());

        // act
        var func = tester.Build<BarInterface>(value);

        // assert
        var a = new BarInterface
        {
            Test = new InterfaceImpl1
            {
                Prop = "a",
            },
        };

        Assert.True(func(a));

        var b = new BarInterface
        {
            Test = new InterfaceImpl1
            {
                Prop = "b",
            },
        };
        Assert.False(func(b));
    }

    public class Foo
    {
        public short BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        public string[]? ScalarArray { get; set; }

        public Bar[]? ObjectArray { get; set; }
    }

    public class FooNullable
    {
        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        public string?[]? ScalarArray { get; set; }

        public Foo?[]? NestedArray { get; set; }
    }

    public interface ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class InterfaceImpl1 : ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class InterfaceImpl2 : ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class BarInterface
    {
        public ITest? Test { get; set; }
    }

    public class Bar
    {
        public Foo? Foo { get; set; }
    }

    public class BarNullable
    {
        public FooNullable? Foo { get; set; }
    }

    public class BarFilterInput
        : FilterInputType<Bar>
    {
    }

    public class BarNullableFilterInput
        : FilterInputType<BarNullable>
    {
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }
}
