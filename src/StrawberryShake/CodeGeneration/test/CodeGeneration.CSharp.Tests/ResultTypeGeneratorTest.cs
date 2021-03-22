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

        [Fact]
        public void Operation_With_NullableData()
        {
            AssertResult(
                @"
                schema {
                    query: Query
                    subscription: Subscription
                }

                type Query { foo: String }

                type Subscription {
                  onFooUpdated: FooNotification!
                }

                type FooNotification {
                  action: String!
                  data: FooNotificationData!
                }

                type FooNotificationData {
                  barGUID: String!
                  documentID: String
                  documentNAME: String
                  thingGUID: String!
                  thingDATE: String!
                  thingDATA: String
                  thingSTATUS: String
                  fooGUID: String!
                  fooAUTHOR: String
                  fooDATE: String!
                  fooTEXT: String
                }",
                @"
                subscription OnFooUpdated {
                  onFooUpdated {
                    action
                    data {
                      barGUID
                      thingGUID
                      thingDATE
                      thingSTATUS
                      fooGUID
                      fooAUTHOR
                      fooDATE
                      fooTEXT
                    }
                  }
                }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
