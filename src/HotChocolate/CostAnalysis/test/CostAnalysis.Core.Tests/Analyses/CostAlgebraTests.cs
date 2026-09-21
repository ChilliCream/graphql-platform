using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests field-cost and type-cost arithmetic.
/// </summary>
public class CostAlgebraTests
{

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

    [Fact]
    public void Join_Should_UseMaximumNumberSemantics_When_AnOperandIsNaN()
    {
        // arrange
        var left = new CostEstimate(double.NaN, 2.0, null);
        var right = new CostEstimate(3.0, double.NaN, null);

        // act
        var joined = CostFieldRule.Join(left, right);

        // assert
        Assert.Equal(new CostEstimate(3.0, 2.0, null), joined);
    }

    [Fact]
    public void Combine_Should_PropagateNaN_When_AnOperandIsNaN()
    {
        // arrange
        var left = new CostEstimate(double.NaN, 1.0, null);
        var right = new CostEstimate(2.0, 3.0, null);

        // act
        var combined = CostFieldRule.Combine(left, right);

        // assert
        Assert.True(double.IsNaN(combined.FieldCost));
        Assert.Equal(4.0, combined.TypeCost);
    }

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

    [Fact]
    public void Scale_Should_PropagateNaN_When_ZeroMultiplierAndInfiniteCost()
    {
        // act
        var scaled = CostFieldRule.Scale(0.0, double.PositiveInfinity);

        // assert
        Assert.True(double.IsNaN(scaled));
    }

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

    [Fact]
    public void Clamp0_Should_ReturnPositiveZero_When_ValueIsNaN()
    {
        // act
        var clamped = CostFieldRule.Clamp0(double.NaN);

        // assert
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(clamped));
    }

    [Fact]
    public void Field_Should_PayArgumentsCostOnce_When_NegativeWeightOffsetsFieldWeight()
    {
        // arrange
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
        // An empty list still incurs the field's call cost.
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
        // The abstract return type uses the larger weight of Book and Magazine.
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

    [Fact]
    public void CostAlgebra_Should_DelegateToCostFieldRule_When_EmptyCombineJoinRootAreInvoked()
    {
        // arrange
        var schemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse("type Query { a: Int }"), new CostSchemaIndexOptions());
        var algebra = new CostAlgebra(schemaIndex);
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
        const string sdl =
            """
            directive @approx(tolerance: Float @cost(weight: "-1.0")) on FIELD
            type Query { value: Int @cost(weight: "5") }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ value @approx(tolerance: 0) }", "Query", "value");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("value", field, members[0], inheritedSize: null),
            algebra.Empty);

        // assert
        // Field weight 5 plus argument weight -1 gives a total of 4.
        Assert.Equal(4.0, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_ReturnEmpty_When_GroupIsDefault()
    {
        // arrange
        var schemaIndex = CostSchemaIndex.Create(
            SchemaParser.Parse("type Query { value: Int }"),
            new CostSchemaIndexOptions());
        var algebra = new CostAlgebra(schemaIndex);

        // act
        var estimate = algebra.Field(default, algebra.Empty);

        // assert
        Assert.Equal(algebra.Empty, estimate);
    }

    [Fact]
    public void CostAlgebra_Field_Should_UseDefaultListSize_When_FieldCarriesNoListSizeAnnotation()
    {
        // arrange
        const string sdl =
            """
            type Item { value: Int @cost(weight: "3") }
            type Query { items: [Item] }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ items { value } }", "Query", "items");
        var valueField = CostFieldRule.Field(1.0, 3.0, 0.0, 0.0, 0.0, algebra.Empty);

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("items", field, members[0], inheritedSize: null),
            valueField);

        // assert
        Assert.Equal(double.PositiveInfinity, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_ApplyZeroInfinityGuard_When_ListOfLeavesCarriesNoAnnotation()
    {
        // arrange
        const string sdl = "type Query { names: [String] }";
        var (algebra, members, field) = ParseRootField(sdl, "{ names }", "Query", "names");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("names", field, members[0], inheritedSize: null),
            algebra.Empty);

        // assert
        Assert.Equal(new CostEstimate(0.0, 0.0, null), estimate);
    }

    [Fact]
    public void CostAlgebra_Field_Should_ResolveListMultiplier_When_SlicingArgumentIsSuppliedAsLiteral()
    {
        // arrange
        const string sdl =
            """
            type Item { value: Int @cost(weight: "3") }
            type Query { items(first: Int): [Item] @listSize(slicingArguments: ["first"]) }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ items(first: 4) { value } }", "Query", "items");
        var valueField = CostFieldRule.Field(1.0, 3.0, 0.0, 0.0, 0.0, algebra.Empty);

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("items", field, members[0], inheritedSize: null),
            valueField);

        // assert
        // The field costs 1, plus 3 for each of the four selected values.
        Assert.Equal(13.0, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_UseInheritedSize_When_ParentSizedFieldsNamesTheField()
    {
        // arrange
        const string sdl =
            """
            type Node { id: ID }
            type Connection { nodes: [Node] }
            type Query { conn(first: Int): Connection @listSize(slicingArguments: ["first"], sizedFields: ["nodes"]) }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ nodes { id } }", "Connection", "nodes");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("nodes", field, members[0], inheritedSize: 10.0),
            algebra.Empty);

        // assert
        Assert.Equal(10.0, estimate.TypeCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_PriceOmittedDirectiveArgument_When_DefinitionDeclaresDefault()
    {
        // arrange
        const string sdl =
            """
            directive @approx(tolerance: Float = 1 @cost(weight: "-1.0")) on FIELD
            type Query { value: Int @cost(weight: "5") }
            """;
        var (algebra, members, field) = ParseRootField(sdl, "{ value @approx }", "Query", "value");

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("value", field, members[0], inheritedSize: null),
            algebra.Empty);

        // assert
        Assert.Equal(4.0, estimate.FieldCost);
    }

    [Fact]
    public void CostAlgebra_Field_Should_JoinOutputs_When_ParentTypesHaveDifferentArgumentWeights()
    {
        // arrange
        const string sdl =
            """
            type A { value(a: Int @cost(weight: "-10")): Int @cost(weight: "5") }
            type B { value(a: Int): Int @cost(weight: "1") }
            type Query { placeholder: Int }
            """;
        var schemaIndex = CostSchemaIndex.Create(SchemaParser.Parse(sdl), new CostSchemaIndexOptions());
        var document = Utf8GraphQLParser.Parse("{ value(a: 1) }");
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var field = (FieldNode)operation.SelectionSet.Selections[0];
        var members = new CollectedFieldGroupMember[]
        {
            new(
                schemaIndex.GetObjectTypeDefinition(schemaIndex.GetObjectTypeIndex("A")),
                schemaIndex.GetObjectTypeDefinition(schemaIndex.GetObjectTypeIndex("A")).Fields["value"]),
            new(
                schemaIndex.GetObjectTypeDefinition(schemaIndex.GetObjectTypeIndex("B")),
                schemaIndex.GetObjectTypeDefinition(schemaIndex.GetObjectTypeIndex("B")).Fields["value"])
        };
        var algebra = new CostAlgebra(schemaIndex);

        // act
        var estimate = algebra.Field(
            new CollectedFieldGroup("value", field, members[0], inheritedSize: null),
            algebra.Empty);
        var estimateForB = algebra.Field(
            new CollectedFieldGroup("value", field, members[1], inheritedSize: null),
            algebra.Empty);
        estimate = algebra.Join(estimate, estimateForB);

        // assert
        Assert.Equal(new CostEstimate(1.0, 0.0, null), estimate);
    }

    /// <summary>
    /// Creates a cost algebra and returns the operation's single root field and its schema definition.
    /// </summary>
    private static (CostAlgebra Algebra, CollectedFieldGroupMember[] Members, FieldNode Field) ParseRootField(
        string sdl,
        string operationText,
        string typeName,
        string fieldName)
    {
        var schema = SchemaParser.Parse(sdl);
        var schemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions());
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var field = (FieldNode)operation.SelectionSet.Selections[0];
        var parentType = schemaIndex.GetObjectTypeDefinition(schemaIndex.GetObjectTypeIndex(typeName));
        var members = new CollectedFieldGroupMember[] { new(parentType, parentType.Fields[fieldName]) };
        return (new CostAlgebra(schemaIndex), members, field);
    }
}
