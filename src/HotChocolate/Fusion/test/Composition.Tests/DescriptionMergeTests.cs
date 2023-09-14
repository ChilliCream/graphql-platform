using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DescriptionMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Output_Rewrite_Nullability_For_Output_Types()
        => await Succeed(
            """
            "This is the query type"
            type Query {
              field1(arg1: String): Foo!
              field2(arg1: FooInput): Foo!
              field3: A_B
            }
            
            type A {
              a: String
            }
            
            type B {
              a: String
            }
            
            union A_B = A | B
            
            input FooInput {
              bar: String!
            }
            
            interface Field2Interface {
              field2(arg1: FooInput): Foo!
            }
            
            enum Foo {
              BAR
            }
            
            directive @foo(arg2: Int) on FIELD
            """,
            """
            "This is the query type revision"
            type Query {
              "field 1"
              field1(
                "arg 1"
                arg1: String): Foo!
              field2(arg1: FooInput): Foo!
              field3: A_B
            }
            
            type A {
              a: String
            }
            
            type B {
              a: String
            }
            
            "union A_B1"
            union A_B = A | B
            
            "input FooInput"
            input FooInput {
              "bar input field"
              bar: String!
            }
            
            "This is the interface"
            interface Field2Interface {
              "field 2 description"
              field2(
                "arg 1 field 2"
                arg1: FooInput): Foo!
            }
            
            "Foo enum type"
            enum Foo {
              "Bar enum value"
              BAR
            }

            directive @foo(arg2: Int) on FIELD
            """);
}