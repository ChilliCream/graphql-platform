using System.ComponentModel.DataAnnotations;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterEnumTests
        : IntegrationTestbase
    {
        [Fact]
        public void Create_EnumEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Bar };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Qux };
            Expect("b", value.ToString(), Items(b), "barEnum");
        }

        [Fact]
        public void Create_EnumNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not",
                    new EnumValueNode(TestEnum.Bar)));
            // assert
            var a = new Foo { BarEnum = TestEnum.Qux };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");
        }

        [Fact]
        public void Create_EnumGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_gt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_EnumNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_gt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_EnumGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_gte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_EnumNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_gte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_EnumLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_lt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_EnumNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_lt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_EnumLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_lte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_EnumNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_lte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new Foo { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new Foo { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_EnumIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_in",
                new ListValueNode(new[]
                {
                    new EnumValueNode(TestEnum.Qux),
                    new EnumValueNode(TestEnum.Baz)
                }))
            );

            // assert
            var a = new Foo { BarEnum = TestEnum.Qux };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");
        }

        [Fact]
        public void Create_EnumNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_in",
                new ListValueNode(new[] {
                    new EnumValueNode(TestEnum.Qux),
                    new EnumValueNode(TestEnum.Baz) }
                ))
            );

            // assert
            var a = new Foo { BarEnum = TestEnum.Bar };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var b = new Foo { BarEnum = TestEnum.Qux };
            Expect("b", value.ToString(), Items(b), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = null };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = null };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Null(x["barEnum"]));
        }

        [Fact]
        public void Create_NullableEnumGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_gt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var d = new FooNullable { BarEnum = null };
            Expect("d", value.ToString(), Items(d), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_gt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_gte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var d = new FooNullable { BarEnum = null };
            Expect("d", value.ToString(), Items(d), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_gte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_lt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");

            var d = new FooNullable { BarEnum = null };
            Expect("d", value.ToString(), Items(d), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_lt",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_NullableEnumLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_lte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("FOO", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b",
                value.ToString(),
                Items(b),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c", value.ToString(), Items(c), "barEnum");

            var d = new FooNullable { BarEnum = null };
            Expect("d", value.ToString(), Items(d), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_lte",
                    new EnumValueNode(TestEnum.Bar)));

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Foo };
            Expect("a", value.ToString(), Items(a), "barEnum");

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));
        }

        [Fact]
        public void Create_NullableEnumIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_in",
                new ListValueNode(new[]
                {
                    new EnumValueNode(TestEnum.Qux),
                    new EnumValueNode(TestEnum.Baz)
                }))
            );

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("QUX", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = null };
            Expect("c", value.ToString(), Items(c), "barEnum");
        }

        [Fact]
        public void Create_NullableEnumNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barEnum_not_in",
                new ListValueNode(new[] {
                    new EnumValueNode(TestEnum.Qux),
                    new EnumValueNode(TestEnum.Baz) }
                ))
            );

            // assert
            var a = new FooNullable { BarEnum = TestEnum.Bar };
            Expect("a",
                value.ToString(),
                Items(a),
                "barEnum",
                x => Assert.Equal("BAR", x["barEnum"]));

            var b = new FooNullable { BarEnum = TestEnum.Qux };
            Expect("b", value.ToString(), Items(b), "barEnum");

            var c = new FooNullable { BarEnum = null };
            Expect("c",
                value.ToString(),
                Items(c),
                "barEnum",
                x => Assert.Null(x["barEnum"]));
        }

        [Fact]
        public void Create_EnumIn_WithPaging()
        {
            // arrange 

            Foo[] item = Items(new Foo { BarEnum = TestEnum.Qux });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    d => d.Field("foos")
                        .Resolver(item)
                        .UsePaging<ObjectType<Foo>>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { barEnum_in: [QUX, BAZ]}) {  totalCount } }");

            // assert
            var queryResult = (IReadOnlyQueryResult)result;

            Assert.Equal(0, queryResult.Errors?.Count ?? 0);

            result.MatchSnapshot();
        }

        public enum TestEnum
        {
            Foo,
            Bar,
            Qux,
            Baz
        }

        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public TestEnum BarEnum { get; set; }
        }

        public class FooNullable
        {
            [Key]
            public int Id { get; set; }

            public TestEnum? BarEnum { get; set; }
        }
    }
}
