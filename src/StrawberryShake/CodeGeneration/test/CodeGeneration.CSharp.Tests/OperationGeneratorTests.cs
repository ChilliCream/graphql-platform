using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationGeneratorTests
    {
        [Fact]
        public void Response_Name_Is_Correctly_Cased()
        {
            AssertResult(
                @"query GetSomething{ bar_baz_foo : foo_bar_baz }",
                @"type Query {
                    foo_bar_baz: String
                }",
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Operation_With_MultipleOperations()
        {
            AssertResult(
                @"query TestOperation($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"query TestOperation2($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"query TestOperation3($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
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
        public void Generate_ChatClient_AllOperations()
        {
            // arrange
            AssertResult(
                FileResource.Open("ChatOperations.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }

        [Fact]
        public void Nullable_List_Input()
        {
            AssertResult(
                @"query GetSomething($bar: Bar){ foo(bar: $bar)}",
                "type Query { foo(bar: Bar ): String }",
                "input Bar { baz: [Baz] }",
                "input Baz { qux: String }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
