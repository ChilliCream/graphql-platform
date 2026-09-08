using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies <see cref="ListSizeResolver"/> against the locked list-size
/// priority chain: inherited sizedFields, slicing arguments present after
/// coercion, slicingArgumentDefaultValue, assumedSize, then
/// CostEngineOptions.DefaultListSize.
/// </summary>
public class ListSizeResolverTests
{
    private static readonly IReadOnlyDictionary<string, SlicingArgumentValue> NoSlicingArguments =
        new Dictionary<string, SlicingArgumentValue>();

    // -- Non-list fields never enter the chain --------------------------------------------------

    [Fact]
    public void Resolve_Should_ReturnOne_When_FieldIsNotAList()
    {
        // act
        var n = ListSizeResolver.Resolve(
            isListField: false,
            metadata: null,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(1.0, n);
    }

    // -- Rank 1: inherited sizedFields -----------------------------------------------------------

    [Fact]
    public void Resolve_Should_UseInheritedSize_When_ParentSizedFieldsNamesThisField()
    {
        // act
        var n = ListSizeResolver.Resolve(
            isListField: true,
            metadata: null,
            inheritedSizes: [5.0],
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(5.0, n);
    }

    [Fact]
    public void Resolve_Should_UseMaxInheritedSize_When_SeveralPossibleParentTypesNameThisField()
    {
        // act
        var n = ListSizeResolver.Resolve(
            isListField: true,
            metadata: null,
            inheritedSizes: [5.0, 9.0, 2.0],
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(9.0, n);
    }

    [Fact]
    public void Resolve_Should_PreferInheritedSize_When_FieldAlsoCarriesItsOwnSlicingArgument()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(3), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            isListField: true,
            metadata,
            inheritedSizes: [20.0],
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(20.0, n);
    }

    // -- Rank 2: slicing arguments present after coercion --------------------------------------

    [Fact]
    public void Resolve_Should_UseSuppliedSlicingArgument_When_Present()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(3), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(3.0, n);
    }

    [Fact]
    public void Resolve_Should_UseSchemaDefault_When_SlicingArgumentIsOmitted()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(SuppliedValue: null, new IntValueNode(4))
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(4.0, n);
    }

    [Fact]
    public void Resolve_Should_UseSchemaDefault_When_SlicingArgumentVariableIsUndefined()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("missing"), new IntValueNode(4))
        };
        var variableValues = new FakeCostVariableValues();

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(4.0, n);
    }

    [Fact]
    public void Resolve_Should_SuppressSchemaDefault_When_SlicingArgumentIsExplicitNull()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], slicingArgumentDefaultValue: 25.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(NullValueNode.Default, new IntValueNode(4))
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(25.0, n);
    }

    [Fact]
    public void Resolve_Should_UseMaxAcrossSlicingArguments_When_MultipleArePresent()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first", "last"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(3), SchemaDefaultValue: null),
            ["last"] = new SlicingArgumentValue(new IntValueNode(5), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(5.0, n);
    }

    [Fact]
    public void Resolve_Should_ClampNegativeSlicingValue_To_Zero()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(-2), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(0.0, n);
    }

    [Fact]
    public void Resolve_Should_KeepZero_When_SlicingValueIsZero()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(0), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(0.0, n);
    }

    // -- Rank 3: slicingArgumentDefaultValue, only when no slicing argument is present ----------

    [Fact]
    public void Resolve_Should_UseSlicingArgumentDefaultValue_When_NoSlicingArgumentIsPresent()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], slicingArgumentDefaultValue: 10.0);

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(10.0, n);
    }

    // -- Rank 4: assumedSize ----------------------------------------------------------------------

    [Fact]
    public void Resolve_Should_UseAssumedSize_When_NoSlicingArgumentOrDefaultValueApply()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], assumedSize: 15.0);

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(15.0, n);
    }

    [Fact]
    public void Resolve_Should_PreferSlicingArgument_Over_AssumedSize_When_BothApply()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], assumedSize: 15.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(1), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(1.0, n);
    }

    // -- Rank 5: CostEngineOptions.DefaultListSize ------------------------------------------------

    [Fact]
    public void Resolve_Should_UseDefaultListSize_When_FieldCarriesNoListSizeMetadata()
    {
        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata: null,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: double.PositiveInfinity);

        // assert
        Assert.Equal(double.PositiveInfinity, n);
    }

    [Fact]
    public void Resolve_Should_UseDefaultListSize_When_MetadataResolvesNothing()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(10.0, n);
    }

    // -- An annotation never sizes its own field --------------------------------------------------

    [Fact]
    public void Resolve_Should_UseDefaultListSize_When_OwnSizedFieldsIsNonEmpty_And_SlicingArgumentIsSupplied()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["items"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(3), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(10.0, n);
    }

    [Fact]
    public void Resolve_Should_UseDefaultListSize_When_OwnSizedFieldsIsNonEmpty_And_AssumedSizeIsSet()
    {
        // arrange
        var metadata = CreateMetadata(assumedSize: 15.0, sizedFields: ["items"]);

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(10.0, n);
    }

    [Fact]
    public void Resolve_Should_UseInheritedSize_When_OwnSizedFieldsIsNonEmpty()
    {
        // arrange
        var metadata = CreateMetadata(sizedFields: ["items"]);

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: [7.0],
            NoSlicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(7.0, n);
    }

    // -- Variable-bound slicing arguments on the static path (variableValues is null) -------------

    [Fact]
    public void Resolve_Should_UseAssumedSize_When_SlicingArgumentIsVariableBound_And_VariableValuesAreNull()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], assumedSize: 100.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), new IntValueNode(10))
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(100.0, n);
    }

    [Fact]
    public void Resolve_Should_PreferAssumedSize_Over_SlicingArgumentDefaultValue_When_VariableBoundInStaticMode()
    {
        // arrange
        var metadata = CreateMetadata(
            slicingArguments: ["first"],
            slicingArgumentDefaultValue: 25.0,
            assumedSize: 100.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(100.0, n);
    }

    [Fact]
    public void Resolve_Should_UseDefaultListSize_When_VariableBoundInStaticMode_And_NoAssumedSize()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(10.0, n);
    }

    [Fact]
    public void Resolve_Should_UseMaxOfLiteralAndStaticFallback_When_MixedInStaticMode()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first", "last"], assumedSize: 100.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(5), SchemaDefaultValue: null),
            ["last"] = new SlicingArgumentValue(new VariableNode("n"), SchemaDefaultValue: null)
        };

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues: null,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(100.0, n);
    }

    // -- Evaluate mode (variableValues is non-null) resolves the coerced value ---------------------

    [Fact]
    public void Resolve_Should_UseCoercedVariableValue_When_VariableIsDefined()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), new IntValueNode(10))
        };
        var variableValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?> { ["n"] = new IntValueNode(4) });

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(4.0, n);
    }

    [Fact]
    public void Resolve_Should_SuppressSchemaDefault_When_VariableIsExplicitNull()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], slicingArgumentDefaultValue: 25.0);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), new IntValueNode(4))
        };
        var variableValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?> { ["n"] = NullValueNode.Default });

        // act
        var n = ListSizeResolver.Resolve(
            true,
            metadata,
            inheritedSizes: default,
            slicingArguments,
            variableValues,
            defaultListSize: 10.0);

        // assert
        Assert.Equal(25.0, n);
    }

    // -- TryResolveSizedFieldSize: the propagation half of rank 1 -------------------------------

    [Fact]
    public void TryResolveSizedFieldSize_Should_ResolveCoercedVariableSlicingArgument_When_ParentSlicesAChildList()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["edges"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("first"), SchemaDefaultValue: null)
        };
        var variableValues = new FakeCostVariableValues(new Dictionary<string, IValueNode?> { ["first"] = new IntValueNode(3) });

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, slicingArguments, variableValues, out var size);

        // assert
        Assert.True(resolved);
        Assert.Equal(3.0, size);
    }

    [Fact]
    public void TryResolveSizedFieldSize_Should_ResolveLiteralSlicingArgument_When_RedirectingSizeFromTheAnnotatedList()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["page"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(3), SchemaDefaultValue: null)
        };

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, slicingArguments, variableValues: null, out var size);

        // assert
        Assert.True(resolved);
        Assert.Equal(3.0, size);
    }

    [Fact]
    public void TryResolveSizedFieldSize_Should_ReturnFalse_When_NoSlicingArgumentAndNoAssumedSize()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["items"]);

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, NoSlicingArguments, variableValues: null, out var size);

        // assert
        Assert.False(resolved);
    }

    [Fact]
    public void TryResolveSizedFieldSize_Should_UseAssumedSize_When_SlicingArgumentIsExplicitNull()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], assumedSize: 50.0, sizedFields: ["items"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(NullValueNode.Default, new IntValueNode(4))
        };

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, slicingArguments, variableValues: null, out var size);

        // assert
        Assert.True(resolved);
        Assert.Equal(50.0, size);
    }

    [Fact]
    public void TryResolveSizedFieldSize_Should_ClampToZero_When_SlicingValueIsNegative()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["items"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new IntValueNode(-2), SchemaDefaultValue: null)
        };

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, slicingArguments, variableValues: null, out var size);

        // assert
        Assert.True(resolved);
        Assert.Equal(0.0, size);
    }

    [Fact]
    public void TryResolveSizedFieldSize_Should_ReturnFalse_When_SlicingArgumentIsVariableBound_And_StaticPathHasNoAssumedSize()
    {
        // arrange
        var metadata = CreateMetadata(slicingArguments: ["first"], sizedFields: ["items"]);
        var slicingArguments = new Dictionary<string, SlicingArgumentValue>
        {
            ["first"] = new SlicingArgumentValue(new VariableNode("n"), SchemaDefaultValue: null)
        };

        // act
        var resolved = ListSizeResolver.TryResolveSizedFieldSize(metadata, slicingArguments, variableValues: null, out var size);

        // assert
        Assert.False(resolved);
    }

    private static ListSizeMetadata CreateMetadata(
        ImmutableArray<string> slicingArguments = default,
        double? slicingArgumentDefaultValue = null,
        double? assumedSize = null,
        ImmutableArray<string> sizedFields = default)
        => new(
            assumedSize,
            slicingArguments.IsDefault ? [] : slicingArguments,
            slicingArgumentDefaultValue,
            sizedFields.IsDefault ? [] : sizedFields,
            RequireOneSlicingArgument: true);

    private sealed class FakeCostVariableValues(Dictionary<string, IValueNode?>? values = null) : ICostVariableValues
    {
        private readonly Dictionary<string, IValueNode?> _values = values ?? [];

        public bool TryGetValue(string name, out IValueNode? value) => _values.TryGetValue(name, out value);
    }
}
