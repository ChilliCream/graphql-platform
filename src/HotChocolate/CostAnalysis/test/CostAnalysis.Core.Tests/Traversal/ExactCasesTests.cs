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

        // assert: name priced at 9 (max of 3/9), hero itself contributes its own weight
        Assert.Equal((1.0, 10.0), decision.Resolve(_ => false));
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

        // assert: the Droid region (700) is not dropped in favor of the Human region (5)
        Assert.Equal((2.0, 702.0), decision.Resolve(_ => false));
    }

    [Fact]
    public void Evaluate_Should_Skip_A_Meta_Field_No_Possible_Type_Defines_When_Selecting_Typename()
    {
        // arrange: a Types.Mutable schema carries no __typename field definition
        const string sdl = "type Query { a: Int }";
        const string operation = "{ __typename }";

        // act
        var decision = TraversalTestHelpers.EvaluateOperation(sdl, operation);

        // assert: no possible type resolves __typename, so the group contributes nothing
        Assert.Equal((0.0, 0.0), decision.Resolve(_ => false));
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

        // assert: hero's own weight is still charged, __typename itself contributes nothing
        Assert.Equal((1.0, 1.0), decision.Resolve(_ => false));
    }
}
