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
            "A deferred incremental plan fetch references a requirement that was not imported.",
            exception.Message);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_CopyCompositeArrayValues_When_ImportedRequirementIsArray()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field(
                "__fusion_1_ids",
                new ListValueNode(
                    new ObjectValueNode(
                        Field("id", new StringValueNode("a")),
                        Field("kind", new StringValueNode("X"))),
                    new ObjectValueNode(
                        Field("id", new StringValueNode("b")),
                        Field("kind", new StringValueNode("Y"))))));

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_ids"),
            [],
            [Requirement("__fusion_1_ids")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_ids":[{"id":"a","kind":"X"},{"id":"b","kind":"Y"}]}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_CopyValueAcrossChunks_When_ValueSpansChunkBoundary()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        // The value alone exceeds one 128KB ChunkedArrayWriter chunk so the
        // imported entry's JsonSegment spans more than one chunk.
        var largeValue = new string('a', 200_000);
        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_blob", new StringValueNode(largeValue)));

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_blob"),
            [],
            [Requirement("__fusion_1_blob")]);

        // assert
        var entry = Assert.Single(result);
        using var document = JsonDocument.Parse(entry.Values.AsSequence());
        Assert.Equal(largeValue, document.RootElement.GetProperty("__fusion_1_blob").GetString());
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_MatchPropertyName_When_PropertyNameSpansChunkBoundary()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        // Pad the source writer so the next entry's property name straddles
        // a 128KB ChunkedArrayWriter chunk boundary.
        PadSourceWriterTo(source, position: 131_065);

        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_EmitInRequestedOrder_When_ImportedSnapshotOrderDiffers()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        // Imported snapshot has properties in order a, b, c.
        var imported = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_a", new StringValueNode("aa")),
            Field("__fusion_2_b", new StringValueNode("bb")),
            Field("__fusion_3_c", new StringValueNode("cc")));

        // act
        // Requirements are passed in c, a, b order; output must follow this order.
        var result = target.CreateVariableValueSetsFromSnapshot(
            [imported],
            ImportedKeys("__fusion_1_a", "__fusion_2_b", "__fusion_3_c"),
            [],
            [
                Requirement("__fusion_3_c"),
                Requirement("__fusion_1_a"),
                Requirement("__fusion_2_b")
            ]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_3_c":"cc","__fusion_1_a":"aa","__fusion_2_b":"bb"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_ReturnEmpty_When_ImportedEntriesIsEmpty()
    {
        // arrange
        using var target = new FetchResultStore();

        // act
        var result = target.CreateVariableValueSetsFromSnapshot(
            ImmutableArray<VariableValues>.Empty,
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        Assert.True(result.IsDefaultOrEmpty);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_SkipEmptyEntries_When_EntryValuesIsEmpty()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var realEntry = CreateVariableValues(
            source,
            CompactPath.Root,
            Field("__fusion_1_id", new StringValueNode("1")));

        // act
        // VariableValues.Empty has IsEmpty == true (default JsonSegment) and is skipped.
        var result = target.CreateVariableValueSetsFromSnapshot(
            [VariableValues.Empty, realEntry],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_id":"1"}
            """);
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

    private static void PadSourceWriterTo(FetchResultStore store, int position)
    {
        // A padding entry has shape {"k":"<filler>"} which is 8 + filler.Length bytes.
        var fillerLength = position - 8;

        if (fillerLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Position must leave room for the {\"k\":\"\"} padding shell.");
        }

        _ = CreateVariableValues(
            store,
            CompactPath.Root,
            Field("k", new StringValueNode(new string('p', fillerLength))));
    }
}
