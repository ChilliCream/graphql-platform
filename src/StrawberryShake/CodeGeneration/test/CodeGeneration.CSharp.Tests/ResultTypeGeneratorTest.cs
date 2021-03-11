using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultTypeGeneratorTests
    {
        [Fact]
        public void Operation_With_Complex_Types()
        {
            AssertResult(
                @"query GetFoo {
                    foo {
                        str
                        strNonNullable
                        nested { str }
                        nestedList { str }
                        nestedMatrix { str }
                    }
                }",
                @"type Query {
                    foo: Baz
                }

                type Baz {
                    str: String
                    strNonNullable: String!
                    nested: Baz
                    nestedList: [Baz!]!
                    nestedMatrix: [[Baz]]
                }",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Operation_With_Comments()
        {
            AssertResult(
                @"query GetFoo {
                    foo {
                        str
                        strNonNullable
                        nested { str }
                        nestedList { str }
                        nestedMatrix { str }
                    }
                }",
                @"type Query {
                    foo: Baz
                }

                ""Baz Type""
                type Baz {
                    ""Field str""
                    str: String
                    ""Field strNonNullable""
                    strNonNullable: String!
                    ""Field nested""
                    nested: Baz
                    ""Field nestedList""
                    nestedList: [Baz!]!
                    ""Field nestedMatrix""
                    nestedMatrix: [[Baz]]
                }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
