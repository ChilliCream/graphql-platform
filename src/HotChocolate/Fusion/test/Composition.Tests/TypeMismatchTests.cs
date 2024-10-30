using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class TypeMismatchTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Output_Rewrite_Nullability_For_Output_Types()
        => await Succeed(
            """
            type Query {
              someData1: String!
              someData2: [String!]
              someData3: [String!]
              someData4: [String!]!
              someData5: [String!]!
              someData6: [[String!]!]
              someData7: [[String!]!]
              someData8: [[String!]!]!
              someData9: [[String!]!]!
            }
            """,
            """
            type Query {
              someData1: String
              someData2: [String]
              someData3: [String]!
              someData4: [String!]
              someData5: [String]!
              someData6: [[String!]]
              someData7: [[String]!]
              someData8: [[String!]!]
              someData9: [[String]!]!
            }
            """);

    [Fact]
    public async Task Output_Fail_On_Named_Type_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: String!
            }
            """,
            """
            type Query {
              someData1: Int!
            }
            """);

    [Fact]
    public async Task Output_Fail_On_Structure_1_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: String!
            }
            """,
            """
            type Query {
              someData1: [String]!
            }
            """);

    [Fact]
    public async Task Output_Fail_On_Structure_2_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: [String]!
            }
            """,
            """
            type Query {
              someData1: [[String]]!
            }
            """);

    [Fact]
    public async Task Input_Rewrite_Nullability_For_Argument_Types()
        => await Succeed(
            """
            type Query {
              someData1(a: Int!): String!
              someData2(a: [Int!]): String!
              someData3(a: [Int]!): String!
              someData4(a: [Int!]!): String!
            }
            """,
            """
            type Query {
              someData1(a: Int): String!
              someData2(a: [Int]): String!
              someData3(a: [Int]): String!
              someData4(a: [Int]!): String!
            }
            """);

    [Fact]
    public async Task Input_Rewrite_Nullability_For_Input_Types()
        => await Succeed(
            """
            type Query {
              someData1(a: Abc): String!
            }

            input Abc {
              a: Int!
              b: [Int!]
              c: [Int]!
              d: [Int!]!
            }
            """,
            """
            type Query {
              someData1(a: Abc!): String!
            }

            input Abc {
              a: Int
              b: [Int]
              c: [Int]
              d: [Int]!
            }
            """);

    [Fact]
    public async Task Input_Fail_On_Named_Type_Mismatch()
        => await Fail(
            """
            type Query {
              someData1(a: Abc): String!
            }

            input Abc {
              a: Int!
            }
            """,
            """
            type Query {
              someData1(a: Abc!): String!
            }

            input Abc {
              a: String
            }
            """);
}
