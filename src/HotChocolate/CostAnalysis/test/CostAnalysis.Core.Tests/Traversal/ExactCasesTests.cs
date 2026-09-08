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

        // assert: 20 (the max of 10/20), not 30 (their sum); typeCost 2 = Query's own root weight (1,
        // applied once by the Root hook) + the selection's 1
        Assert.Equal((2.0, 21.0), decision.Resolve(_ => false));
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

        // assert: the same total either way, never both branches' cost at once; typeCost 3 = Query's
        // own root weight (1, applied once by the Root hook) + the selection's 2
        Assert.Equal((3.0, 12.0), decision.Resolve(_ => true));
        Assert.Equal((3.0, 12.0), decision.Resolve(_ => false));
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

        // assert: both fields fire together (1 + 10 + 3 = 14) or neither does (1 + 0 = 1); typeCost
        // gains Query's own root weight (1, applied once by the Root hook) on top of the selection's
        Assert.Equal((2.0, 14.0), decision.Resolve(_ => true));
        Assert.Equal((2.0, 1.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Correlate_A_Variable_Used_In_Two_Differently_Nested_Boundaries()
    {
        // arrange: `$x` gates `m` one level under `p`, and gates `s` two levels under `q`
        // behind `$y`; the two boundaries discover `$x` at different depths, so a fold
        // that decorrelates its two occurrences overestimates the static bound to 104
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
        var folded = decision.FoldWithJoin((a, b) => (Math.Max(a.TypeCost, b.TypeCost), Math.Max(a.FieldCost, b.FieldCost)));

        // assert: the true static bound is the max over the 4 real assignments (103),
        // never the decorrelated 104 that lets $x read false for `p` and true for `q`; typeCost
        // gains Query's own root weight (1, applied once by the Root hook) on top of the selection's 4
        Assert.Equal((5.0, 103.0), folded);
    }

    [Fact]
    public void Evaluate_Should_Take_The_Max_Over_Members_When_A_Field_Is_Selected_Through_An_Interface()
    {
        // arrange: an interface field priced through each possible object type's own definition
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

        // assert: name priced at 9 (max of 3/9), hero itself contributes its own weight; typeCost
        // gains Query's own root weight (1, applied once by the Root hook) on top of the selection's 1
        Assert.Equal((2.0, 10.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Join_Every_Covariant_Return_Type_When_A_Field_Narrows_Per_Implementer()
    {
        // arrange: Character.friend is covariant, Human.friend: Human and Droid.friend: Droid;
        // both regions' child boundaries must be evaluated and joined, not just the first
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

        // assert: the Droid region (700) is not dropped in favor of the Human region (5); typeCost
        // gains Query's own root weight (1, applied once by the Root hook) on top of the selection's 2
        Assert.Equal((3.0, 702.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Skip_A_Meta_Field_No_Possible_Type_Defines_When_Selecting_Typename()
    {
        // arrange: a Types.Mutable schema carries no __typename field definition
        const string sdl = "type Query { a: Int }";
        const string operation = "{ __typename }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: no possible type resolves __typename, so the group contributes nothing; the root
        // rule still charges Query's own weight (1, applied once by the Root hook) for an empty selection
        Assert.Equal((1.0, 0.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_ApplyParentSizedFields_When_CostAlgebraResolvesAChildList()
    {
        // arrange: oracle sized_fields_apply_a_parent_slice_to_a_child_list, a literal first: 3
        // slices items' own edges list through the parent's sizedFields, not edges' own (absent)
        // @listSize, and is threaded through the real CostAlgebra rather than the test double
        const string sdl =
            """
            type Edge { node: String }
            type Connection { edges: [Edge] }
            type Query {
              items(first: Int): Connection @listSize(slicingArguments: ["first"], sizedFields: ["edges"])
            }
            """;
        const string operation = "{ items(first: 3) { edges { node } } }";
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSnapshot(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert: typeCost 5 = Query's own root weight (1, applied once by the Root hook) + items'
        // contribution (4, edges sized to 3 by items' sizedFields); fieldCost 2 = items' own weight
        // (1) + edges' field call cost (1), paid once regardless of the multiplier
        Assert.Equal(new CostEstimate(2.0, 5.0, null), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Not_Crash_When_Selecting_Typename_Inside_An_Interface_Typed_Boundary()
    {
        // arrange: __typename selected under an interface-typed field, still absent everywhere
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

        // assert: hero's own weight is still charged, __typename itself contributes nothing; typeCost
        // gains Query's own root weight (1, applied once by the Root hook) on top of the selection's 1
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
        var algebra = new CostAlgebra(ConditionTreeTestHelpers.BuildSnapshot(sdl));

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation, algebra);

        // assert
        Assert.Equal(new CostEstimate(103.0, 103.0, null), decision.Resolve(_ => false));
    }
}
