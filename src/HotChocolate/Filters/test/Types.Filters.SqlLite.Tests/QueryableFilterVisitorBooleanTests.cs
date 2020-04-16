using System.ComponentModel.DataAnnotations;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextBooleanTests
        : QueryableFilterVisitorTestBase, IClassFixture<SqlServerProvider>
    {

        public QueryableFilterVisitorContextBooleanTests(SqlServerProvider provider)
            : base(provider)
        {
        }

        [Fact]
        public void Create_BooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));


            // assert
            var a = new Foo { Bar = true };
            Expect("a",
                value.ToString(),
                Items(a),
                "bar",
                x => Assert.Equal(true, x["bar"]));

            var b = new Foo { Bar = false };
            Expect("b", value.ToString(), Items(b), "bar");
        }

        [Fact]
        public void Create_BooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));


            // assert
            var a = new Foo { Bar = false };
            Expect("a",
                value.ToString(),
                Items(a),
                "bar",
                x => Assert.Equal(false, x["bar"]));

            var b = new Foo { Bar = true };
            Expect("b", value.ToString(), Items(b), "bar");
        }

        [Fact]
        public void Create_NullableBooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

            // assert
            var a = new FooNullable { Bar = true };
            Expect("a",
                value.ToString(),
                Items(a),
                "bar",
                x => Assert.Equal(true, x["bar"]));

            var b = new FooNullable { Bar = false };
            Expect("b", value.ToString(), Items(b), "bar");

        }

        [Fact]
        public void Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));

            // assert
            var a = new FooNullable { Bar = false };
            Expect("a",
                value.ToString(),
                Items(a),
                "bar",
                x => Assert.Equal(false, x["bar"]));

            var b = new FooNullable { Bar = true };
            Expect("b", value.ToString(), Items(b), "bar");

        }

        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            [Key]
            public int Id { get; set; }

            public bool? Bar { get; set; }
        }
    }
}
