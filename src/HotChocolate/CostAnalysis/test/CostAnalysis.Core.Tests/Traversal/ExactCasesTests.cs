namespace HotChocolate.CostAnalysis;

public class ExactCasesTests
{
    [Fact]
    public void Evaluate_Should_Take_The_Max_Not_The_Sum_When_Type_Branches_Are_Mutually_Exclusive()
    {
        // arrange
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

        // assert
        // The larger alternative costs 20, plus 1 for the result field; the two object types cost 2.
        Assert.Equal((2.0, 21.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Fire_Exactly_One_Branch_When_Include_And_Skip_Are_Complementary()
    {
        // arrange
        const string sdl =
            """
            type Side { costly: Int @cost(weight: "10") }
            type Query { left: Side right: Side }
            """;
        const string operation =
            "query($x: Boolean!) { left { costly @include(if: $x) } right { costly @skip(if: $x) } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((3.0, 12.0), decision.Resolve(_ => true));
        Assert.Equal((3.0, 12.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Merge_Same_Response_Name_Fields_Before_Weighing_Under_Signed_Weights()
    {
        // arrange
        // Merge both book selections before combining the negative type weight with child costs.
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

        // assert
        // The type cost is -7 + 5 + 1 + 5 = 4; the two field calls cost 2.
        Assert.Equal((4.0, 2.0), decision.Resolve(name => true));
    }

    [Fact]
    public void Evaluate_Should_Match_The_Merged_Control_When_Book_Is_Selected_Unconditionally()
    {
        // arrange
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
        // arrange
        const string sdl =
            """
            type Side { costly: Int @cost(weight: "10") other: Int @cost(weight: "3") }
            type Query { left: Side }
            """;
        const string operation =
            "query($x: Boolean!) { left { costly @include(if: $x) other @include(if: $x) } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((2.0, 14.0), decision.Resolve(_ => true));
        Assert.Equal((2.0, 1.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Correlate_A_Variable_Used_In_Two_Differently_Nested_Boundaries()
    {
        // arrange
        // The same variable appears at different depths and must have one value across both branches.
        const string sdl =
            """
            type Query { p: P q: Q }
            type P { m: M }
            type M { n: Int @cost(weight: "100") }
            type Q { r: R }
            type R { s: Int @cost(weight: "100") }
            """;
        const string operation =
            """
            query($x: Boolean!, $y: Boolean!) {
              p { m @include(if: $x) { n @skip(if: $y) } }
              q { r @include(if: $y) { s @skip(if: $x) } }
            }
            """;

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);
        var folded = decision.FoldWithJoin(
            (a, b) => (Math.Max(a.TypeCost, b.TypeCost), Math.Max(a.FieldCost, b.FieldCost)));

        // assert
        // Allowing different values for $x in each branch would incorrectly raise the bound to 104.
        Assert.Equal((5.0, 103.0), folded);
    }

    [Fact]
    public void Evaluate_Should_Take_The_Max_Over_Members_When_A_Field_Is_Selected_Through_An_Interface()
    {
        // arrange
        const string sdl =
            """
            interface Character { name: String }
            type Human implements Character { name: String @cost(weight: "3") }
            type Droid implements Character { name: String @cost(weight: "9") }
            type Query { hero: Character }
            """;
        const string operation = "{ hero { name } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((2.0, 10.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Join_Every_Covariant_Return_Type_When_A_Field_Narrows_Per_Implementer()
    {
        // arrange
        // Covariant return types give each parent type a different child selection to evaluate.
        const string sdl =
            """
            interface Character { friend: Character }
            type Human implements Character { friend: Human h: Int @cost(weight: "5") }
            type Droid implements Character { friend: Droid d: Int @cost(weight: "700") }
            type Query { hero: Character }
            """;
        const string operation = "{ hero { friend { ... on Droid { d } ... on Human { h } } } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((3.0, 702.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Skip_A_Meta_Field_No_Possible_Type_Defines_When_Selecting_Typename()
    {
        // arrange
        // The mutable schema has no __typename field definition.
        const string sdl = "type Query { a: Int }";
        const string operation = "{ __typename }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        // The missing field contributes no cost; the Query type still contributes 1.
        Assert.Equal((1.0, 0.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_ApplyParentSizedFields_When_CostAlgebraResolvesAChildList()
    {
        // arrange
        // The parent's slicing argument sets the child list's size through sizedFields.
        const string sdl =
            """
            type Edge { node: String }
            type Connection { edges: [Edge] }
            type Query {
              items(first: Int): Connection @listSize(slicingArguments: ["first"], sizedFields: ["edges"])
            }
            """;
        const string operation = "{ items(first: 3) { edges { node } } }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSchemaIndex(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        // Query, the connection, and three edges cost 5 in total; the two field calls cost 2.
        Assert.Equal(new CostEstimate(2.0, 5.0, null), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Not_Crash_When_Selecting_Typename_Inside_An_Interface_Typed_Boundary()
    {
        // arrange
        const string sdl =
            """
            interface Character { name: String }
            type Human implements Character { name: String }
            type Droid implements Character { name: String }
            type Query { hero: Character }
            """;
        const string operation = "{ hero { __typename } }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert
        Assert.Equal((2.0, 1.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_JoinPairOutputs_When_ImplementersCrossListSizesAndFieldWeights()
    {
        // arrange
        const string sdl =
            """
            interface Node { edges: [Edge] }
            type A implements Node { edges: [Edge] @cost(weight: "100") @listSize(assumedSize: 1) }
            type B implements Node { edges: [Edge] @cost(weight: "1") @listSize(assumedSize: 100) }
            type Edge { value: Int @cost(weight: "1") }
            type Container { node: Node }
            type Query { container: Container }
            """;
        const string operation = "{ container { node { edges { value } } } }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSchemaIndex(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        Assert.Equal(new CostEstimate(103.0, 103.0, null), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_PreserveJoinOrder_When_AllPairOutputsAreLeaves()
    {
        // arrange
        const string sdl =
            """
            interface Node { value: Int }
            type A implements Node { value: Int }
            type B implements Node { value: Int }
            type C implements Node { value: Int }
            type Query { node: Node }
            """;
        var joins = new List<string>();
        var algebra = new TraceAlgebra(joins);

        // act
        _ = TraversalTestHelpers.EvaluateOperation(sdl, "{ node { value } }", algebra);

        // assert
        Assert.Equal(
            [
                "A.value(empty) + B.value(empty)",
                "join(A.value(empty),B.value(empty)) + C.value(empty)"
            ],
            joins);
    }

    [Fact]
    public void Evaluate_Should_PreserveJoinOrder_When_ResponseNameRepeatsAcrossConditions()
    {
        // arrange
        const string sdl =
            """
            interface Node { value: Int }
            type A implements Node { value: Int }
            type B implements Node { value: Int }
            type C implements Node { value: Int }
            type Query { node: Node }
            """;
        const string operation =
            "query($include: Boolean!) { node { value value @include(if: $include) } }";
        var joins = new List<string>();
        var algebra = new TraceAlgebra(joins);

        // act
        _ = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        Assert.Equal(
            [
                "A.value(empty) + B.value(empty)",
                "join(A.value(empty),B.value(empty)) + C.value(empty)",
                "A.value(empty) + B.value(empty)",
                "join(A.value(empty),B.value(empty)) + C.value(empty)",
                "join(join(A.value(empty),B.value(empty)),C.value(empty)) + A.value(empty)",
                "join(join(join(A.value(empty),B.value(empty)),C.value(empty)),A.value(empty)) + B.value(empty)",
                "join(join(join(join(A.value(empty),B.value(empty)),C.value(empty))"
                    + ",A.value(empty)),B.value(empty)) + C.value(empty)"
            ],
            joins);
    }

    [Fact]
    public void Evaluate_Should_ClampOpposingInfiniteArgumentAndDirectiveSums_When_WeightsAreFinite()
    {
        // arrange
        const string sdl =
            """
            directive @negative(
              x: Int @cost(weight: "-1.7976931348623157E+308")
              y: Int @cost(weight: "-1.7976931348623157E+308")
            ) on FIELD
            type Query {
              value(
                a: Int @cost(weight: "1.7976931348623157E+308")
                b: Int @cost(weight: "1.7976931348623157E+308")
              ): Int
            }
            """;
        const string operation = "{ value(a: 1, b: 1) @negative(x: 1, y: 1) }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSchemaIndex(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        Assert.Equal(new CostEstimate(0.0, 1.0, null), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_JoinZeroWithOuterListResult_When_ZeroMultiplierMeetsInfiniteChildCost()
    {
        // arrange
        const string sdl =
            """
            type Child { value: Int @cost(weight: "1") }
            type Outer { children: [Child] }
            type Query { outer: [Outer] @listSize(assumedSize: 0) }
            """;
        const string operation = "{ outer { children { value } } }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSchemaIndex(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        Assert.Equal(new CostEstimate(0.0, 1.0, null), decision.Resolve(_ => false));
    }

    private sealed class TraceAlgebra(List<string> joins) : IAnalysisAlgebra<string>
    {
        public string Empty => "empty";

        public string Field(in CollectedFieldGroup group, string child)
            => $"{group.Member.ParentType.Name}.{group.Member.Field.Name}({child})";

        public string Combine(string left, string right) => $"combine({left},{right})";

        public string Join(string left, string right)
        {
            joins.Add($"{left} + {right}");
            return $"join({left},{right})";
        }

        public string Root(double rootTypeWeight, string selection) => $"root({selection})";
    }
}
