namespace HotChocolate.CostAnalysis;

public class ExactCasesTests
{
    [Fact]
    public void Evaluate_Should_Take_The_Max_Not_The_Sum_When_Type_Branches_Are_Mutually_Exclusive()
    {
        // arrange: c1-exclusive-types shape, `a` and `b` can never both fire at runtime
        const string sdl =
            """
            union Result = A | B
            type A { a: Int @cost(weight: "10") }
            type B { b: Int @cost(weight: "20") }
            type Query { result: Result }
            """;
        const string operation = "{ result { ... on A { a } ... on B { b } } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: 20 (the max of 10/20), not 30 (their sum)
        Assert.Equal((1.0, 21.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Fire_Exactly_One_Branch_When_Include_And_Skip_Are_Complementary()
    {
        // arrange: c3-complementary shape, `costly` is charged once regardless of $x
        const string sdl =
            """
            type Side { costly: Int @cost(weight: "10") }
            type Query { left: Side right: Side }
            """;
        const string operation =
            "query($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: the same total either way, never both branches' cost at once
        Assert.Equal((2.0, 12.0), decision.Resolve(_ => true));
        Assert.Equal((2.0, 12.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Merge_Same_Response_Name_Fields_Before_Weighing_Under_Signed_Weights()
    {
        // arrange: c5-signed-weights shape, `book` selected twice under different conditions
        // must be merged into one field call, and the negative Book weight must not be
        // clamped away before the whole call's sum is taken (collect-then-weigh)
        const string sdl =
            """
            scalar Text @cost(weight: "5")
            type Query @cost(weight: "0") { book: Book }
            type Book @cost(weight: "-7") { title: Text author: Author }
            type Author { name: Text }
            """;
        const string operation =
            """
            query($a: Boolean!, $b: Boolean!) {
              book @include(if: $a) { title }
              book @include(if: $b) { author { name } }
            }
            """;

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: -7 + 5 (title) + 1 + 5 (author.name) = 4, one field call = 2
        Assert.Equal((4.0, 2.0), decision.Resolve(name => true));
    }

    [Fact]
    public void Evaluate_Should_Match_The_Merged_Control_When_Book_Is_Selected_Unconditionally()
    {
        // arrange: the c5 control query, both subtrees selected in one place
        const string sdl =
            """
            scalar Text @cost(weight: "5")
            type Query @cost(weight: "0") { book: Book }
            type Book @cost(weight: "-7") { title: Text author: Author }
            type Author { name: Text }
            """;
        const string operation = "{ book { title author { name } } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((4.0, 2.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Correlate_A_Variable_Used_Twice_Under_The_Same_Field()
    {
        // arrange: `$x` gates two response names on the same type; resolving it once
        // must decide both, never a mix of one field's true-branch with the other's
        // false-branch
        const string sdl =
            """
            type Side { costly: Int @cost(weight: "10") other: Int @cost(weight: "3") }
            type Query { left: Side }
            """;
        const string operation =
            "query($x: Boolean!) { left { costly @include(if: $x) other @include(if: $x) } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: both fields fire together (1 + 10 + 3 = 14) or neither does (1 + 0 = 1)
        Assert.Equal((1.0, 14.0), decision.Resolve(_ => true));
        Assert.Equal((1.0, 1.0), decision.Resolve(_ => false));
    }
}
