using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class FetchResultStoreTests
{
    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_MergeForwardedVariables_When_RequirementsAreImported()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")),
            Field("__fusion_2_sku", new StringValueNode("sku-1")));

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_id", "__fusion_2_sku"),
            [Field("limit", new IntValueNode(10))],
            [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Assert.Equal(CompactPath.Root, entry.Path);
        Assert.True(entry.AdditionalPaths.IsDefaultOrEmpty);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"limit":10,"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_PreserveAdditionalPaths_When_FilteredRequirementsDeduplicate()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var primaryPath = Path(1);
        var primaryAdditionalPath = Path(2);
        var duplicatePath = Path(3);
        var duplicateAdditionalPath = Path(4);

        var first = WithAdditionalPaths(
            CreateVariableValues(
                source,
                primaryPath,
                Field("__fusion_1_id", new StringValueNode("1")),
                Field("__fusion_2_sku", new StringValueNode("sku-1"))),
            primaryAdditionalPath);

        var second = WithAdditionalPaths(
            CreateVariableValues(
                source,
                duplicatePath,
                Field("__fusion_1_id", new StringValueNode("1")),
                Field("__fusion_2_sku", new StringValueNode("sku-2"))),
            duplicateAdditionalPath);

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [first, second],
            ImportedKeys("__fusion_1_id", "__fusion_2_sku"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Assert.Equal(primaryPath, entry.Path);
        Assert.Equal(
            [primaryAdditionalPath, duplicatePath, duplicateAdditionalPath],
            entry.AdditionalPaths.AsSpan().ToArray());
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_CopyCompositeValues_When_ImportedRequirementIsNested()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field(
                "__fusion_1_filter",
                new ObjectValueNode(
                    Field("tags", new ListValueNode(new StringValueNode("a"), new StringValueNode("b"))),
                    Field("size", new IntValueNode(2)))));

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_filter"),
            [],
            [Requirement("__fusion_1_filter")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_filter":{"tags":["a","b"],"size":2}}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_Throw_When_RequirementWasNotImported()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => target.CreateVariableValueSetsFromSnapshot(
                [imported],
                ImportedKeys("__fusion_1_id"),
                [],
                [Requirement("__fusion_2_sku")]));

        // assert
        Assert.Equal(
            "A deferred sub-plan fetch references a requirement that was not imported.",
            exception.Message);
    }

    private static VariableValues CreateVariableValues(
        FetchResultStore store,
        CompactPath path,
        params ObjectFieldNode[] fields)
        => store.CreateVariableValueSets(path, fields);

    private static VariableValues WithAdditionalPaths(
        VariableValues values,
        params CompactPath[] additionalPaths)
        => values with
        {
            AdditionalPaths = new CompactPathSegment(additionalPaths, 0, additionalPaths.Length)
        };

    private static ObjectFieldNode Field(string name, IValueNode value)
        => new(name, value);

    private static OperationRequirement Requirement(string key)
        => new(
            key,
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new PathNode(new PathSegmentNode(new FusionNameNode(key))));

    private static HashSet<string> ImportedKeys(params string[] keys)
        => new(keys, StringComparer.Ordinal);

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }

    private static string Normalize(JsonSegment segment)
    {
        using var document = JsonDocument.Parse(segment.AsSequence());
        return JsonSerializer.Serialize(document.RootElement);
    }
}
