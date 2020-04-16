using System.ComponentModel.DataAnnotations;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorObjectTests
        : QueryableFilterVisitorTestBase, IClassFixture<SqlServerProvider>
    {

        public QueryableFilterVisitorObjectTests(SqlServerProvider provider)
            : base(provider)
        {
        }

        [Fact]
        public void Create_ObjectStringEqual_Expression_Valid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            // act 
            Expect(
                value.ToString(),
                Items(new Foo { FooNested = new FooNested { Bar = "a" } }),
                "id",
                x => Assert.NotNull(x));
        }

        [Fact]
        public void Create_ObjectStringEqual_Expression_Invalid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            // act 
            Expect(
                value.ToString(),
                Items(new Foo { FooNested = new FooNested { Bar = "b" } }),
                "id");
        }

        [Fact]
        public void Create_ObjectStringEqualWithNull_Expression_Valid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            //act
            Expect(
                value.ToString(),
                Items(new Foo { FooNested = new FooNested { Bar = "a" } }),
                "id",
                x => Assert.NotNull(x));

        }

        [Fact]
        public void Create_ObjectStringEqualWithNull_Expression_Invalid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            // act 
            Expect(
                value.ToString(),
                Items(new Foo { FooNested = null }),
                "id");
        }



        [Fact]
        public void Create_ObjectStringEqualEvenDeeper_Expression_Valid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));
            var item =
                new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id",
                x => Assert.NotNull(x));
        }

        [Fact]
        public void Create_ObjectStringEqualEvenDeeper_Expression_Invalid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));
            var item =
                new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "b" } } };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id");
        }

        [Fact]
        public void Create_ObjectStringEqualRecursive_Expression_Valid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("nested",
                    new ObjectValueNode(
                    new ObjectFieldNode("nested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));
            var item =
                new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "a" } } };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id",
                x => Assert.NotNull(x));
        }

        [Fact]
        public void Create_ObjectStringEqualRecursive_Expression_Invalid()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("nested",
                    new ObjectValueNode(
                    new ObjectFieldNode("nested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            var item =
                new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "b" } } };


            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id");
        }

        [Fact]
        public void Create_ObjectStringEqualRecursiveNull_Expression_Invalid1()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            var item = new EvenDeeper { Foo = null };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id");
        }

        [Fact]
        public void Create_ObjectStringEqualRecursiveNull_Expression_Invalid2()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            var item = new EvenDeeper { Foo = new Foo { FooNested = null } };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id");
        }


        [Fact]
        public void Create_ObjectStringEqualRecursiveNull_Expression_Invalid3()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            var item =
                new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } };

            // act 
            Expect(
                value.ToString(),
                Items(item),
                "id");
        }

        public class EvenDeeper
        {
            [Key]
            public int Id { get; set; }

            public Foo Foo { get; set; }
        }

        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public FooNested FooNested { get; set; }
        }

        public class FooNested
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }
        }

        public class Recursive
        {
            [Key]
            public int Id { get; set; }

            public Recursive Nested { get; set; }

            public string Bar { get; set; }
        }


        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Object(t => t.FooNested).AllowObject(x => x.Filter(y => y.Bar));
            }
        }

        public class EvenDeeperFilterType
            : FilterInputType<EvenDeeper>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<EvenDeeper> descriptor)
            {
                descriptor.Object(t => t.Foo)
                    .AllowObject(x => x.Object(y => y.FooNested)
                                .AllowObject(z => z.Filter(z => z.Bar)));
            }
        }
    }
}
