using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class RequirementBindingTests
{
    private static readonly IValueSelectionNode s_map = new FieldSelectionMapParser("foo.x").Parse();

    [Fact]
    public void ReadsSameValue_Should_ReturnTrue_When_RequirementsDifferOnlyByKey()
    {
        // arrange
        var left = CreateRequirement("__fusion_1_seed");
        var right = CreateRequirement("__fusion_5_seed");

        // act
        var readsSameValue = OperationPlanner.ReadsSameValue(left, right);

        // assert
        Assert.True(readsSameValue);
    }

    [Fact]
    public void ReadsSameValue_Should_ReturnFalse_When_PathDiffers()
    {
        // arrange
        var left = CreateRequirement("__fusion_1_seed");
        var right = CreateRequirement("__fusion_5_seed", path: SelectionPath.Parse("$.other"));

        // act
        var readsSameValue = OperationPlanner.ReadsSameValue(left, right);

        // assert
        Assert.False(readsSameValue);
    }

    [Fact]
    public void ReadsSameValue_Should_ReturnFalse_When_MapDiffers()
    {
        // arrange
        var left = CreateRequirement("__fusion_1_seed");
        var right = CreateRequirement(
            "__fusion_5_seed",
            map: new FieldSelectionMapParser("foo.y").Parse());

        // act
        var readsSameValue = OperationPlanner.ReadsSameValue(left, right);

        // assert
        Assert.False(readsSameValue);
    }

    [Fact]
    public void ReadsSameValue_Should_ReturnFalse_When_TypeDiffers()
    {
        // arrange
        var left = CreateRequirement("__fusion_1_seed");
        var right = CreateRequirement("__fusion_5_seed", type: new NamedTypeNode("String"));

        // act
        var readsSameValue = OperationPlanner.ReadsSameValue(left, right);

        // assert
        Assert.False(readsSameValue);
    }

    [Fact]
    public void ReadsSameValue_Should_ReturnFalse_When_InternalAliasDiffers()
    {
        // arrange
        var left = CreateRequirement("__fusion_1_seed");
        var right = CreateRequirement("__fusion_5_seed", internalAlias: "fusion__requirement_foo_0");

        // act
        var readsSameValue = OperationPlanner.ReadsSameValue(left, right);

        // assert
        Assert.False(readsSameValue);
    }

    [Fact]
    public void TryBindToExistingSelection_Should_BindToExistingVariable_When_RequirementsReadSameValue()
    {
        // arrange
        var selection = Utf8GraphQLParser.Syntax.ParseField("foo(seed: $__fusion_5_seed)");
        var existing = Utf8GraphQLParser.Syntax.ParseField("foo(seed: $__fusion_1_seed)");
        var requirements = CreateRequirements(
            CreateRequirement("__fusion_1_seed"),
            CreateRequirement("__fusion_5_seed"));

        // act
        var bound = OperationPlanner.TryBindToExistingSelection(
            selection,
            [existing],
            requirements,
            "__fusion_5_",
            out var boundSelection,
            out var boundRequirementKeys);

        // assert
        Assert.True(bound);
        Assert.Equal("foo(seed: $__fusion_1_seed)", boundSelection!.ToString(indented: false));
        Assert.Equal(["__fusion_5_seed"], boundRequirementKeys);
    }

    [Fact]
    public void TryBindToExistingSelection_Should_Throw_When_RequirementsReadDifferentValues()
    {
        // arrange
        var selection = Utf8GraphQLParser.Syntax.ParseField("foo(seed: $__fusion_5_seed)");
        var existing = Utf8GraphQLParser.Syntax.ParseField("foo(seed: $__fusion_1_seed)");
        var requirements = CreateRequirements(
            CreateRequirement("__fusion_1_seed"),
            CreateRequirement("__fusion_5_seed", internalAlias: "fusion__requirement_foo_0"));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => OperationPlanner.TryBindToExistingSelection(
                selection,
                [existing],
                requirements,
                "__fusion_5_",
                out _,
                out _));

        // assert
        Assert.Equal(
            "The requirement variable '__fusion_5_seed' cannot be bound to '__fusion_1_seed' "
            + "because the two requirements do not read the same value.",
            exception.Message);
    }

    private static OperationRequirement CreateRequirement(
        string key,
        ITypeNode? type = null,
        SelectionPath? path = null,
        IValueSelectionNode? map = null,
        string? internalAlias = null)
        => new(
            key,
            type ?? new NamedTypeNode("Int"),
            path ?? SelectionPath.Parse("$.product"),
            map ?? s_map,
            internalAlias);

    private static ImmutableDictionary<string, OperationRequirement> CreateRequirements(
        params OperationRequirement[] requirements)
        => requirements.ToImmutableDictionary(t => t.Key);
}
