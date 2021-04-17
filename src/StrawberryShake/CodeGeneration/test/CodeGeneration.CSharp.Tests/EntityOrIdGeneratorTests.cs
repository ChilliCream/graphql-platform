using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityOrIdGeneratorTests
    {
        [Fact]
        public void UnionList()
        {
            AssertResult(
                @"
                query GetFoo {
                    foo {
                        ... on Baz {
                            id
                        }
                        ... on Quox {
                            foo
                        }
                        ... on Baz2 {
                            id
                        }
                        ... on Quox2 {
                            foo
                        }
                    }
                }
                ",
                @"
                type Query {
                    foo: [Bar]
                }

                type Baz {
                    id: String
                }

                type Baz2 {
                    id: String
                }

                type Quox {
                    foo: String
                }

                type Quox2 {
                    foo: String
                }

                union Bar = Baz | Quox | Baz2 | Quox2
                ",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void UnionField()
        {
            AssertResult(
                @"
                query GetFoo {
                    foo {
                        ... on Baz {
                            id
                        }
                        ... on Quox {
                            foo
                        }
                        ... on Baz2 {
                            id
                        }
                        ... on Quox2 {
                            foo
                        }
                    }
                }
                ",
                @"
                type Query {
                    foo: Bar
                }

                type Baz {
                    id: String
                }

                type Baz2 {
                    id: String
                }

                type Quox {
                    foo: String
                }

                type Quox2 {
                    foo: String
                }

                union Bar = Baz | Quox | Baz2 | Quox2
                ",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void UnionListInEntity()
        {
            AssertResult(
                @"
                query GetFoo {
                    test {
                        foo {
                            ... on Baz {
                                id
                            }
                            ... on Quox {
                                foo
                            }
                            ... on Baz2 {
                                id
                            }
                            ... on Quox2 {
                                foo
                            }
                        }
                    }
                }
                ",
                @"
                type Query {
                    test: Test
                }
                type Test {
                    id: String
                    foo: [Bar]
                }

                type Baz {
                    id: String
                }

                type Baz2 {
                    id: String
                }

                type Quox {
                    foo: String
                }

                type Quox2 {
                    foo: String
                }

                union Bar = Baz | Quox | Baz2 | Quox2
                ",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void InterfaceList()
        {
            AssertResult(
                @"
                query GetFoo {
                    foo {
                        foo
                        ... on Baz {
                            id
                        }
                        ... on Quox {
                            baz
                        }
                        ... on Baz2 {
                            id
                        }
                        ... on Quox2 {
                            bar
                        }
                    }
                }
                ",
                @"
                type Query {
                    foo: [Bar]
                }

                type Baz implements Bar {
                    id: String
                    foo: String
                }

                type Baz2 implements Bar {
                    id: String
                    foo: String
                }

                type Quox implements Bar {
                    foo: String
                    baz: String
                }

                type Quox2 implements Bar {
                    foo: String
                    bar: String
                }

                interface Bar {
                    foo: String
                }
                ",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void InterfaceField()
        {
            AssertResult(
                @"
                query GetFoo {
                    foo {
                        foo
                        ... on Baz {
                            id
                        }
                        ... on Quox {
                            baz
                        }
                        ... on Baz2 {
                            id
                        }
                        ... on Quox2 {
                            bar
                        }
                    }
                }
                ",
                @"
                type Query {
                    foo: Bar
                }

                type Baz implements Bar {
                    id: String
                    foo: String
                }

                type Baz2 implements Bar {
                    id: String
                    foo: String
                }

                type Quox implements Bar {
                    foo: String
                    baz: String
                }

                type Quox2 implements Bar {
                    foo: String
                    bar: String
                }

                interface Bar {
                    foo: String
                }
                ",
                "extend schema @key(fields: \"id\")");
        }
    }
}
