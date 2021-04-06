using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputGeneratorTests
    {
        [Fact]
        public void Operation_With_Complex_Arguments()
        {
            AssertResult(
                @"query test($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"type Query {
                    foo(single: Bar!, list: [Bar!]!, nestedList: [[Bar]]): String
                }

                input Bar {
                    str: String
                    strNonNullable: String!
                    nested: Bar
                    nestedList: [Bar!]!
                    nestedMatrix: [[Bar]]
                }",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Operation_With_Comments()
        {
            AssertResult(
                @"query test($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"type Query {
                    foo(single: Bar!, list: [Bar!]!, nestedList: [[Bar]]): String
                }

                ""Bar InputType""
                input Bar {
                    ""Field str""
                    str: String
                    ""Field strNonNullable""
                    strNonNullable: String!
                    ""Field nested""
                    nested: Bar
                    ""Field nestedList""
                    nestedList: [Bar!]!
                    ""Field nestedMatrix""
                    nestedMatrix: [[Bar]]
                }",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Operation_With_Comments_With_Input_Records()
        {
            AssertResult(
                new AssertSettings { InputRecords = true },
                @"query test($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"type Query {
                    foo(single: Bar!, list: [Bar!]!, nestedList: [[Bar]]): String
                }

                ""Bar InputType""
                input Bar {
                    ""Field str""
                    str: String
                    ""Field strNonNullable""
                    strNonNullable: String!
                    ""Field nested""
                    nested: Bar
                    ""Field nestedList""
                    nestedList: [Bar!]!
                    ""Field nestedMatrix""
                    nestedMatrix: [[Bar]]
                }",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Input_Type_Fields_Are_Inspected_For_LeafTypes()
        {
            AssertResult(
                @"mutation ChangeHomePlanet($input: ChangeHomePlanetInput!) {
                    changeHomePlanet(input: $input) {
                        human {
                            homePlanet
                        }
                    }
                }",
                FileResource.Open("StarWarsSchema_ChangeHomePlanet.graphql"),
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void KeywordCollisions()
        {
            AssertResult(
                @"query readonly($input: abstract!) {
                    readonly(readonly: $input) {
                        abstract
                    }
                    readonlyEntity {
                        id
                        abstract
                    }
                }",
                @"
                type Query {
                    readonly(readonly: abstract): readonly
                    readonlyEntity: readonlyEntity
                }
                input abstract {
                    class: String
                }
                type readonly {
                    abstract: String
                }
                type readonlyEntity {
                    id: ID
                    abstract: String
                }
                ",
                "extend schema @key(fields: \"id\")");
        }
    }
}
