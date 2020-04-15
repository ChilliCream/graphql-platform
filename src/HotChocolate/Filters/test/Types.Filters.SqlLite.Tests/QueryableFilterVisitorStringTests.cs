using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HotChocolate.Types.Filters
{

    public class QueryableFilterVisitorStringTests
        : QueryableFilterVisitorTestBase, IClassFixture<SqlServerProvider>
    {

        public QueryableFilterVisitorStringTests(SqlServerProvider provider)
            : base(provider)
        {
        }

        [Fact]
        public void Create_StringEqual_Expression_Valid()
        {
            Expect(
                "{ bar: \"a\" }",
                Items(new Foo { Bar = "a" }),
                x => Assert.Equal("a", x["bar"]));
        }

        [Fact]
        public void Create_StringEqual_Expression_Invalid()
        {
            Expect("{ bar: \"a\" }", Items(new Foo { Bar = "b" }));
        }

        [Fact]
        public void Create_StringNotEqual_Expression_Invalid()
        {
            Expect(
                "{ bar_not: \"a\" }",
                Items(new Foo { Bar = "b" }),
                x => Assert.Equal("b", x["bar"]));
        }

        [Fact]
        public void Create_StringNotEqual_Expression_Valid()
        {
            Expect(
                "{ bar_not: \"a\" }",
                Items(new Foo { Bar = "a" }));
        }

        [Fact]
        public void Create_StringIn_Expression_Invalid()
        {
            Expect(
                "{ bar_in: [\"a\", \"c\"]}",
                Items(new Foo { Bar = "b" }));
        }

        [Fact]
        public void Create_StringIn_Expression_Valid()
        {
            Expect(
                "{ bar_in: [\"a\", \"c\"]}",
                Items(new Foo { Bar = "a" }),
                x => Assert.Equal("a", x["bar"]));
        }


        [Fact]
        public void Create_StringNotIn_Expression_Invalid()
        {
            Expect(
                "{ bar_not_in: [\"a\", \"c\"]}",
                Items(new Foo { Bar = "a" }));
        }

        [Fact]
        public void Create_StringNotIn_Expression_Valid()
        {
            Expect(
                "{ bar_not_in: [\"a\", \"c\"]}",
                Items(new Foo { Bar = "b" }),
                x => Assert.Equal("b", x["bar"]));
        }


        [Fact]
        public void Create_StringIn_SignleExpression_Invalid()
        {
            Expect(
                "{ bar_in: \"a\"}",
                Items(new Foo { Bar = "b" }));
        }

        [Fact]
        public void Create_StringIn_SignleExpression_Valid()
        {
            Expect(
                "{ bar_in: \"a\"}",
                Items(new Foo { Bar = "a" }),
                x => Assert.Equal("a", x["bar"]));
        }


        [Fact]
        public void Create_StringNotIn_SignleExpression_Invalid()
        {
            Expect(
                "{ bar_not_in: \"a\"}",
                Items(new Foo { Bar = "a" }));
        }

        [Fact]
        public void Create_StringNotIn_SignleExpression_Valid()
        {
            Expect(
                "{ bar_not_in: \"a\"}",
                Items(new Foo { Bar = "b" }),
                x => Assert.Equal("b", x["bar"]));
        }

        [Fact]
        public void Create_StringContains_Expression_Invalid()
        {
            Expect(
                "{ bar_contains: \"a\"}",
                Items(new Foo { Bar = "testbtest" }));
        }

        [Fact]
        public void Create_StringContains_SignleExpression_Valid()
        {
            Expect(
                "{ bar_contains: \"a\"}",
                Items(new Foo { Bar = "testatest" }),
                x => Assert.Equal("testatest", x["bar"]));
        }


        [Fact]
        public void Create_StringNotContains_Expression_Invalid()
        {
            Expect(
                "{ bar_not_contains: \"b\"}",
                Items(new Foo { Bar = "testbtest" }));
        }

        [Fact]
        public void Create_StringNotContains_SignleExpression_Valid()
        {
            Expect(
                "{ bar_not_contains: \"c\"}",
                Items(new Foo { Bar = "testatest" }),
                x => Assert.Equal("testatest", x["bar"]));
        }


        [Fact]
        public void Create_StringStartsWith_Expression_Invalid()
        {
            Expect(
                "{ bar_starts_with: \"a\"}",
                Items(new Foo { Bar = "ba" }));
        }

        [Fact]
        public void Create_StringStartsWith_SignleExpression_Valid()
        {
            Expect(
                "{ bar_starts_with: \"a\"}",
                Items(new Foo { Bar = "ab" }),
                x => Assert.Equal("ab", x["bar"]));
        }


        [Fact]
        public void Create_StringNotStartsWith_Expression_Invalid()
        {
            Expect(
                "{ bar_not_starts_with: \"a\"}",
                Items(new Foo { Bar = "ab" }));
        }

        [Fact]
        public void Create_StringNotStartsWith_SignleExpression_Valid()
        {
            Expect(
                "{ bar_not_starts_with: \"a\"}",
                Items(new Foo { Bar = "ba" }),
                x => Assert.Equal("ba", x["bar"]));
        }

        [Fact]
        public void Create_StringEndsWith_Expression_Invalid()
        {
            Expect(
                "{ bar_ends_with: \"a\"}",
                Items(new Foo { Bar = "ab" }));
        }

        [Fact]
        public void Create_StringEndsWith_SignleExpression_Valid()
        {
            Expect(
                "{ bar_ends_with: \"a\"}",
                Items(new Foo { Bar = "ba" }),
                x => Assert.Equal("ba", x["bar"]));
        }


        [Fact]
        public void Create_StringNotEndsWith_Expression_Invalid()
        {
            Expect(
                "{ bar_not_ends_with: \"a\"}",
                Items(new Foo { Bar = "ba" }));
        }

        [Fact]
        public void Create_StringNotEndsWith_SignleExpression_Valid()
        {
            Expect(
                "{ bar_not_ends_with: \"a\"}",
                Items(new Foo { Bar = "ab" }),
                x => Assert.Equal("ab", x["bar"]));
        }

        public class Foo
        {
            [Key]
            public int Id { get; set; }
            public string Bar { get; set; }
        }

    }
}
