using System.ComponentModel.DataAnnotations;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextComparableTests
        : QueryableFilterVisitorTestBase, IClassFixture<SqlServerProvider>
    {

        public QueryableFilterVisitorContextComparableTests(SqlServerProvider provider)
            : base(provider)
        {
        }

        [Fact]
        public void Create_ShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 12 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var b = new Foo { BarShort = 13 };
            Expect("b", value.ToString(), Items(b), "barShort");
        }

        [Fact]
        public void Create_ShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 13 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");
        }


        [Fact]
        public void Create_ShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new Foo { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));
        }

        [Fact]
        public void Create_ShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new Foo { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");
        }


        [Fact]
        public void Create_ShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new Foo { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new Foo { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));
        }

        [Fact]
        public void Create_ShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new Foo { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");
        }



        [Fact]
        public void Create_ShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new Foo { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");
        }

        [Fact]
        public void Create_ShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new Foo { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new Foo { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));
        }


        [Fact]
        public void Create_ShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new Foo { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");
        }

        [Fact]
        public void Create_ShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

            // assert
            var a = new Foo { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new Foo { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));
        }

        [Fact]
        public void Create_ShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

            // assert
            var a = new Foo { BarShort = 13 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var b = new Foo { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");
        }

        [Fact]
        public void Create_ShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            // assert
            var a = new Foo { BarShort = 12 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var b = new Foo { BarShort = 13 };
            Expect("b", value.ToString(), Items(b), "barShort");
        }

        [Fact]
        public void Create_NullableShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 12 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 13 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = null };
            Expect("c", value.ToString(), Items(c), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 13 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = null };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Null(x["barShort"]));
        }


        [Fact]
        public void Create_NullableShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var d = new FooNullable { BarShort = null };
            Expect("d", value.ToString(), Items(d), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new FooNullable { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");
        }


        [Fact]
        public void Create_NullableShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new FooNullable { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new FooNullable { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var d = new FooNullable { BarShort = null };
            Expect("d", value.ToString(), Items(d), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");

        }



        [Fact]
        public void Create_NullableShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");

            var d = new FooNullable { BarShort = null };
            Expect("d", value.ToString(), Items(d), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new FooNullable { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new FooNullable { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

        }


        [Fact]
        public void Create_NullableShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(11, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b",
                value.ToString(),
                Items(b),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var c = new FooNullable { BarShort = 13 };
            Expect("c", value.ToString(), Items(c), "barShort");

            var d = new FooNullable { BarShort = null };
            Expect("d", value.ToString(), Items(d), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));


            // assert
            var a = new FooNullable { BarShort = 11 };
            Expect("a", value.ToString(), Items(a), "barShort");

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = 13 };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

        }

        [Fact]
        public void Create_NullableShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );


            // assert
            var a = new FooNullable { BarShort = 13 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(13, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 12 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = null };
            Expect("c", value.ToString(), Items(c), "barShort");
        }

        [Fact]
        public void Create_NullableShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            // assert
            var a = new FooNullable { BarShort = 12 };
            Expect("a",
                value.ToString(),
                Items(a),
                "barShort",
                x => Assert.Equal(12, (short)x["barShort"]));

            var b = new FooNullable { BarShort = 13 };
            Expect("b", value.ToString(), Items(b), "barShort");

            var c = new FooNullable { BarShort = null };
            Expect("c",
                value.ToString(),
                Items(c),
                "barShort",
                x => Assert.Null(x["barShort"]));
        }


        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public short BarShort { get; set; }
        }

        public class FooNullable
        {
            [Key]
            public int Id { get; set; }

            public short? BarShort { get; set; }
        }
    }
}
