using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies <see cref="CostFieldRule"/>, the pure arithmetic of the IBM
/// cost algebra, against the locked field rule and known oracle numbers
/// (graphql-static-analysis-rs estimator.rs).
/// </summary>
public class CostAlgebraTests
{
    // -- Empty, Combine, Join ----------------------------------------------------------------

    [Fact]
    public void Empty_Should_Be_ZeroZero_When_Read()
    {
        // act
        var empty = CostFieldRule.Empty;

        // assert
        Assert.Equal(new CostEstimate(0.0, 0.0, null), empty);
    }

    [Fact]
    public void Combine_Should_SumComponentwise_When_Invoked()
    {
        // arrange
        var left = new CostEstimate(2.0, 3.0, null);
        var right = new CostEstimate(5.0, -1.0, null);

        // act
        var combined = CostFieldRule.Combine(left, right);

        // assert
        Assert.Equal(new CostEstimate(7.0, 2.0, null), combined);
    }

    [Fact]
    public void Join_Should_MaxComponentwise_When_Invoked()
    {
        // arrange
        var left = new CostEstimate(2.0, 9.0, null);
        var right = new CostEstimate(5.0, -1.0, null);

        // act
        var joined = CostFieldRule.Join(left, right);

        // assert
        Assert.Equal(new CostEstimate(5.0, 9.0, null), joined);
    }

    // -- Scale: the 0 * Infinity guard -------------------------------------------------------

    [Fact]
    public void Scale_Should_ReturnZero_When_CostIsZero_And_MultiplierIsInfinite()
    {
        // act
        var scaled = CostFieldRule.Scale(double.PositiveInfinity, 0.0);

        // assert
        Assert.Equal(0.0, scaled);
    }

    [Fact]
    public void Scale_Should_Multiply_When_CostIsNonZero()
    {
        // act
        var scaled = CostFieldRule.Scale(3.0, 2.0);

        // assert
        Assert.Equal(6.0, scaled);
    }

    [Fact]
    public void Scale_Should_Saturate_When_MultiplierIsInfiniteAndCostIsPositive()
    {
        // act
        var scaled = CostFieldRule.Scale(double.PositiveInfinity, 1.0);

        // assert
        Assert.Equal(double.PositiveInfinity, scaled);
    }

    // -- Clamp0 --------------------------------------------------------------------------------

    [Fact]
    public void Clamp0_Should_ClampToZero_When_ValueIsNegative()
    {
        // act
        var clamped = CostFieldRule.Clamp0(-4.0);

        // assert
        Assert.Equal(0.0, clamped);
    }

    [Fact]
    public void Clamp0_Should_PassThrough_When_ValueIsNonNegative()
    {
        // act
        var clamped = CostFieldRule.Clamp0(4.0);

        // assert
        Assert.Equal(4.0, clamped);
    }

    // -- Field: whole-call clamping and signed weights ------------------------------------------

    [Fact]
    public void Field_Should_PayArgumentsCostOnce_When_NegativeWeightOffsetsFieldWeight()
    {
        // arrange
        // graphql-static-analysis-rs estimator.rs: negative_argument_reduces_field_cost_before_clamping,
        // fieldWithCost @cost(weight: "5") with a present approx: Boolean @cost(weight: "-3") argument.
        var child = CostFieldRule.Empty;

        // act
        var estimate = CostFieldRule.Field(
            n: 1.0,
            fieldWeight: 5.0,
            argumentsCost: -3.0,
            directiveArgumentsCost: 0.0,
            returnTypeWeight: 0.0,
            child);

        // assert
        Assert.Equal(new CostEstimate(2.0, 0.0, null), estimate);
    }

    [Fact]
    public void Field_Should_NotPayArgumentsCost_When_ArgumentsAreOmitted()
    {
        // arrange
        // estimator.rs: field_call_cost_is_not_multiplied, fieldWithCost called with no arguments.
        var child = CostFieldRule.Empty;

        // act
        var estimate = CostFieldRule.Field(
            n: 1.0,
            fieldWeight: 5.0,
            argumentsCost: 0.0,
            directiveArgumentsCost: 0.0,
            returnTypeWeight: 0.0,
            child);

        // assert
        Assert.Equal(new CostEstimate(5.0, 0.0, null), estimate);
    }

    [Fact]
    public void Field_Should_PriceDirectiveArguments_When_QueryDirectiveCarriesOwnWeight()
    {
        // arrange
        // estimator.rs: custom_directive_argument_weights_affect_field_cost,
        // value @cost(weight: "5.0") called with @approx(tolerance: 0.5)
        // where @approx's tolerance argument carries @cost(weight: "-1.0") (R-DIRECTIVE-ARG-COST).
        var child = CostFieldRule.Empty;

        // act
        var estimate = CostFieldRule.Field(
            n: 1.0,
            fieldWeight: 5.0,
            argumentsCost: 0.0,
            directiveArgumentsCost: -1.0,
            returnTypeWeight: 0.0,
            child);

        // assert
        Assert.Equal(4.0, estimate.FieldCost);
    }

    [Fact]
    public void Field_Should_KeepZeroMultiplier_When_ListIsEmpty_And_StillPayFieldCallOnce()
    {
        // arrange
        // article fixture c6-zero-length-list: items(limit: 0) { value }, Item.value @cost(weight: "3"),
        // Item and the items field both default to composite weight 1.0. A supplied slicing value of
        // 0 zeroes the multiplier but the field's own call cost is still paid once.
        var valueField = CostFieldRule.Field(
            n: 1.0,
            fieldWeight: 3.0,
            argumentsCost: 0.0,
            directiveArgumentsCost: 0.0,
            returnTypeWeight: 0.0,
            CostFieldRule.Empty);

        // act
        var itemsField = CostFieldRule.Field(
            n: 0.0,
            fieldWeight: 1.0,
            argumentsCost: 0.0,
            directiveArgumentsCost: 0.0,
            returnTypeWeight: 1.0,
            valueField);
        var root = CostFieldRule.Root(rootTypeWeight: 1.0, itemsField);

        // assert
        Assert.Equal(new CostEstimate(1.0, 1.0, null), root);
    }

    [Fact]
    public void Field_Should_MatchOracle_When_AbstractReturnTypeWeightIsTheSignedMaxOverMemberTypes()
    {
        // arrange
        // estimator.rs: abstract_output_uses_the_maximum_possible_object_weight, { publication { title } }
        // over interface Publication with members Book (default weight 1.0) and Magazine
        // (@cost(weight: "7")). returnTypeWeight is the schema-wide signed max (7.0, hc-3-mmh.7 edge (d));
        // title is a leaf on both members and joins to (0, 0).
        var titleOnBook = CostFieldRule.Field(1.0, 0.0, 0.0, 0.0, 0.0, CostFieldRule.Empty);
        var titleOnMagazine = CostFieldRule.Field(1.0, 0.0, 0.0, 0.0, 0.0, CostFieldRule.Empty);
        var title = CostFieldRule.Join(titleOnBook, titleOnMagazine);

        // act
        var publication = CostFieldRule.Field(
            n: 1.0,
            fieldWeight: 1.0,
            argumentsCost: 0.0,
            directiveArgumentsCost: 0.0,
            returnTypeWeight: 7.0,
            title);
        var root = CostFieldRule.Root(rootTypeWeight: 1.0, publication);

        // assert
        Assert.Equal(new CostEstimate(1.0, 8.0, null), root);
    }

    // -- Root: root type weight added once, never to the field cost ----------------------------

    [Fact]
    public void Root_Should_ClampAtZero_When_RootTypeWeightIsNegativeAndUnoffset()
    {
        // arrange
        var selection = new CostEstimate(4.0, 2.0, null);

        // act
        var root = CostFieldRule.Root(rootTypeWeight: -10.0, selection);

        // assert
        Assert.Equal(new CostEstimate(4.0, 0.0, null), root);
    }

    // -- CostAlgebra: CostFieldRule and ListSizeResolver wired against a real snapshot ---------

    [Fact]
    public void CostAlgebra_Should_DelegateToCostFieldRule_When_EmptyCombineJoinRootAreInvoked()
    {
        // arrange
        var snapshot = CostSchemaSnapshot.Create(SchemaParser.Parse("type Query { a: Int }"), new CostEngineOptions());
        var algebra = new CostAlgebra(snapshot);
        var left = new CostEstimate(2.0, 3.0, null);
        var right = new CostEstimate(5.0, -1.0, null);

        // act
        var empty = algebra.Empty;
        var combined = algebra.Combine(left, right);
        var joined = algebra.Join(left, right);
        var root = algebra.Root(rootTypeWeight: 1.0, left);

        // assert
        Assert.Equal(CostFieldRule.Empty, empty);
        Assert.Equal(CostFieldRule.Combine(left, right), combined);
        Assert.Equal(CostFieldRule.Join(left, right), joined);
        Assert.Equal(CostFieldRule.Root(1.0, left), root);
    }

    [Fact]
    public void CostAlgebra_Field_Should_PriceDirectiveArguments_When_QueryDirectiveCarriesOwnWeight()
    {
        // arrange
        // estimator.rs: custom_directive_argument_weights_affect_field_cost (R-DIRECTIVE-ARG-COST)
        const string sdl =
            """
            directive @approx(tolerance: Float @cost(weight: "-1.0")) on FIELD
            type Query { value: Int @cost(weight: "5") }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ value @approx(tolerance: 0) }", "Query", "value");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("value", members, listMultiplier: 1.0, field.Arguments, field.Directives),
            algebra.Empty);

        // assert: 5 (field weight) - 1 (tolerance's own weight, charged once) = 4
        Assert.Equal(4.0, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_UseDefaultListSize_When_FieldCarriesNoListSizeAnnotation()
    {
        // arrange: an unannotated list of composites costs +Infinity through the default list size
        const string sdl =
            """
            type Item { value: Int @cost(weight: "3") }
            type Query { items: [Item] }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ items { value } }", "Query", "items");
        var valueField = CostFieldRule.Field(1.0, 3.0, 0.0, 0.0, 0.0, algebra.Empty);

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("items", members, listMultiplier: 1.0, field.Arguments, field.Directives),
            valueField);

        // assert
        Assert.Equal(double.PositiveInfinity, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_ApplyZeroInfinityGuard_When_ListOfLeavesCarriesNoAnnotation()
    {
        // arrange: R-DEFAULT-LIST-SIZE addendum, a list of leaves costs 0 through the 0 * inf guard
        const string sdl = "type Query { names: [String] }";
        var (algebra, members, field) = ParseRootField(sdl, "{ names }", "Query", "names");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("names", members, listMultiplier: 1.0, field.Arguments, field.Directives),
            algebra.Empty);

        // assert
        Assert.Equal(new CostEstimate(0.0, 0.0, null), estimate);
    }

    [Fact]
    public void CostAlgebra_Field_Should_ResolveListMultiplier_When_SlicingArgumentIsSuppliedAsLiteral()
    {
        // arrange: rank 2 of the locked list-size priority chain, a literal Int slicing argument
        const string sdl =
            """
            type Item { value: Int @cost(weight: "3") }
            type Query { items(first: Int): [Item] @listSize(slicingArguments: ["first"]) }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ items(first: 4) { value } }", "Query", "items");
        var valueField = CostFieldRule.Field(1.0, 3.0, 0.0, 0.0, 0.0, algebra.Empty);

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("items", members, listMultiplier: 1.0, field.Arguments, field.Directives),
            valueField);

        // assert: n = 4, items' own weight 1 (default) + 4 * value's fieldCost 3
        Assert.Equal(13.0, estimate.FieldCost);
    }

    /// <summary>
    /// Builds a snapshot from <paramref name="sdl"/>, parses
    /// <paramref name="operationText"/>'s single root field, and returns a
    /// <see cref="CostAlgebra"/> over that snapshot together with the root
    /// field's one-member <see cref="CollectedFieldGroupMember"/> array.
    /// </summary>
    private static (CostAlgebra Algebra, CollectedFieldGroupMember[] Members, FieldNode Field) ParseRootField(
        string sdl,
        string operationText,
        string typeName,
        string fieldName)
    {
        var schema = SchemaParser.Parse(sdl);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var field = (FieldNode)operation.SelectionSet.Selections[0];
        var parentType = snapshot.GetObjectTypeDefinition(snapshot.GetObjectTypeIndex(typeName));
        var members = new CollectedFieldGroupMember[] { new(parentType, parentType.Fields[fieldName]) };
        return (new CostAlgebra(snapshot), members, field);
    }
}
