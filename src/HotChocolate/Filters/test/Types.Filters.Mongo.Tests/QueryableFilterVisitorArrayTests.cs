using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HotChocolate.Language;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextArrayTests
        : QueryableFilterVisitorTestBase, IClassFixture<MongoResource>
    {

        public QueryableFilterVisitorContextArrayTests(MongoResource provider)
            : base(new MongoProvider(provider))
        {
        }

        [Fact]
        public void Create_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("a",
                value.ToString(),
                Items(a),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var b = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("b", value.ToString(), Items(b), "fooNested {id}");
        }

        [Fact]
        public void Create_ArraySomeObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("a",
                value.ToString(),
                Items(a),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var b = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("b", value.ToString(), Items(b), "fooNested {id}");

            var c = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=10, Bar = null },
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("c",
                value.ToString(),
                Items(c),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));
        }

        [Fact]
        public void Create_ArrayNoneObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_none",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };
            Expect("a", value.ToString(), Items(a), "fooNested {id}");

            var b = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("b",
                value.ToString(),
                Items(b),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var c = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=10, Bar = null },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("c",
                value.ToString(),
                Items(c),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));
        }

        [Fact(Skip = "Currently not supported")]
        public void Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_all",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=1, Bar = "a" },
                    new FooNested { Id=2, Bar = "a" },
                    new FooNested { Id=3, Bar = "a" }
                }
            };

            Expect("a",
                value.ToString(),
                Items(a),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var b = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=2, Bar = "a" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("b", value.ToString(), Items(b), "fooNested {id}");

            var c = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=1, Bar = "a" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("c", value.ToString(), Items(c), "fooNested {id}");

            var d = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("d", value.ToString(), Items(d), "fooNested {id}");

            var e = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=10, Bar = null },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=2, Bar = "b" }
                }
            };

            Expect("e", value.ToString(), Items(e), "fooNested {id}");
        }

        [Fact]
        public void Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(true)
                )
            );

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("a",
                value.ToString(),
                Items(a),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var b = new Foo { FooNested = new List<FooNested>() { } };

            Expect("b", value.ToString(), Items(b), "fooNested {id}");
            var c = new Foo { FooNested = null };

            Expect("c", value.ToString(), Items(c), "fooNested {id}");

            var d = new Foo { FooNested = new List<FooNested>() { } };

            Expect("d",
                value.ToString(),
                Items(d),
                "fooNested {id}");
        }

        [Fact]
        public void Create_ArrayNotAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(false)));

            // assert
            var a = new Foo
            {
                FooNested = new List<FooNested>()
                {
                    new FooNested { Id=3, Bar = "c" },
                    new FooNested { Id=4, Bar = "d" },
                    new FooNested { Id=1, Bar = "a" }
                }
            };

            Expect("a", value.ToString(), Items(a), "fooNested {id}");

            var b = new Foo { FooNested = new List<FooNested>() { } };

            Expect(
                "b",
                value.ToString(),
                Items(b),
                "fooNested {id}",
                x => Assert.NotNull(x["fooNested"]));

            var c = new Foo { FooNested = null };

            Expect(
                "c",
                value.ToString(),
                Items(c),
                "fooNested {id}",
                x => Assert.Null(x["fooNested"]));
        }
        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public List<FooNested> FooNested { get; set; }
        }

        public class FooNested
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }
        }

    }
}
