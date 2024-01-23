using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DescriptionMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Merge_Single_Line_Descriptions()
        => await Succeed(
            """
            "This is the query type"
            type Query {
              "field1"
              field1("This is an arg" arg1: String, arg2: String): String!
              field2: String
            }
            """,
            """
            "This is the query type revision"
            type Query {
              "field1 from subgraph 2"
              field1("This is the 1st arg" arg1: String, "This is the 2nd arg" arg2: String): String!
              "field2 from subgraph 2"
              field2: String
            }
            """);

    [Fact]
    public async Task Merge_Multi_Line_Descriptions()
        => await Succeed(""""
            """
            This is the
            query type
            """
            type Query {
              """
              field
              one
              """
              field1(
                """
                This is an
                arg
                """
                arg1: String, arg2: String): String!
              field2: String
            }
            """",
            """"
            """
            This is the query type revision
            """
            type Query {
              """
              field1
              from subgraph 2
              """
              field1(
                """
                This is
                the 1st arg
                """
                arg1: String,
                """
                This is
                the 2nd arg
                """
                arg2: String): String!
              """
              field2
              from subgraph 2
              """
              field2: String
            }
            """");
}
