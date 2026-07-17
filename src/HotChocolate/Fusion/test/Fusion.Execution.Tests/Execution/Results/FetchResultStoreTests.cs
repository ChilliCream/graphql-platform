using System.Buffers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;
using IntValueNode = HotChocolate.Language.IntValueNode;
using NullValueNode = HotChocolate.Language.NullValueNode;
using StringValueNode = HotChocolate.Language.StringValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;
using ObjectValueNode = HotChocolate.Language.ObjectValueNode;
using ObjectFieldNode = HotChocolate.Language.ObjectFieldNode;
using IValueNode = HotChocolate.Language.IValueNode;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class FetchResultStoreTests : FusionTestBase
{
    private static readonly FieldInfo s_dataElementStagingField =
        typeof(FetchResultStore).GetField(
            "_dataElementStaging",
            BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException(
            "FetchResultStore no longer contains a non-public instance field named '_dataElementStaging'. Update the tests accordingly.");
    private static readonly byte[] s_fieldPayload = """{"data":{"field":"value"}}"""u8.ToArray();
    private static readonly FusionSchemaDefinition s_schema = CreateCompositeSchema();

    [Fact]
    public void GetResultPaths_Should_ThrowInvalidOperationException_When_TargetTraversesScalar()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);
        var plan = PlanOperation(schema, "{ field }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var document = SourceResultDocument.Parse(
            sourceArena,
            s_fieldPayload,
            s_fieldPayload.Length);
        store.AddPartialResult(
            SelectionPath.Root,
            new SourceSchemaResult(CompactPath.Root, document),
            node.ResultSelectionSet,
            containsErrors: false);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => store.GetResultPaths(SelectionPath.Root.AppendField("field")));

        // assert
        Assert.Equal(
            "Expected the value at result path 'field' for selection path '$.field' to be an object or list, "
                + "but found 'String'.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddPartialResults_Should_RegisterAllResults_When_MergeThrows(bool containsErrors)
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);
        var plan = PlanOperation(schema, "{ field }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var results = new[]
        {
            CreateSourceSchemaResult(sourceArena, CompactPath.Root),
            CreateSourceSchemaResult(sourceArena, Path(~0)),
            CreateSourceSchemaResult(sourceArena, CompactPath.Root)
        };

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => store.AddPartialResults(
                SelectionPath.Root,
                results,
                node.ResultSelectionSet,
                containsErrors));

        // assert
        Assert.Contains("Expected StartArray but found StartObject.", exception.Message);
        Assert.Contains(results[0], store.MemoryOwners);
        Assert.Contains(results[1], store.MemoryOwners);
        Assert.Contains(results[2], store.MemoryOwners);

        if (!containsErrors)
        {
            AssertDataElementStagingCleared(store, results.Length);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(64)]
    public void AddPartialResults_Should_ReuseRetainedStaging_When_NoErrorBatchIsWithinLimit(int count)
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateEmptyStore(schema, "{ field }", resultArena, out var resultSelectionSet);
        var first = CreateSourceSchemaResults(sourceArena, count, "warm");
        var second = CreateSourceSchemaResults(sourceArena, count, "final");

        // act
        Assert.True(
            store.AddPartialResults(
                SelectionPath.Root,
                first,
                resultSelectionSet,
                containsErrors: false));

        var staging = GetDataElementStaging(store);
        Assert.Equal(256, staging.Length);
        AssertDataElementStagingCleared(store, count);
        AssertResultsRegisteredInOrder(store, first);

        using (var dirt = CreateSourceSchemaResult(
                   sourceArena,
                   CompactPath.Root,
                   """{"data":{"field":"dirt"}}"""))
        {
            staging.AsSpan(0, count).Fill(dirt.Data);

            Assert.True(
                store.AddPartialResults(
                    SelectionPath.Root,
                    second,
                    resultSelectionSet,
                    containsErrors: false));
        }

        // assert
        Assert.Same(staging, GetDataElementStaging(store));
        AssertDataElementStagingCleared(store, count);
        AssertResultsRegisteredInOrder(store, second);
        Assert.Equal($"{{\"field\":\"final-{count - 1}\"}}", RenderData(store));
    }

    [Fact]
    public void AddPartialResults_Should_ClearRetainedStaging_When_DataReadThrows()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateEmptyStore(schema, "{ field }", resultArena, out var resultSelectionSet);
        var results = CreateSourceSchemaResults(sourceArena, 3, "value");
        results[1].Dispose();

        try
        {
            // act
            Assert.Throws<ObjectDisposedException>(
                () => store.AddPartialResults(
                    SelectionPath.Root,
                    results,
                    resultSelectionSet,
                    containsErrors: false));

            // assert
            AssertDataElementStagingCleared(store, results.Length);
            Assert.Collection(store.MemoryOwners, memory => Assert.Same(store.Result, memory));
        }
        finally
        {
            results[0].Dispose();
            results[2].Dispose();
        }
    }

    [Fact]
    public void AddPartialResults_Should_ClearRetainedStaging_When_MergeStopsEarly()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateEmptyStore(schema, "{ field }", resultArena, out var resultSelectionSet);
        var results = CreateSourceSchemaResults(sourceArena, 3, "value");
        store.Result.Data.Invalidate();

        // act
        var added = store.AddPartialResults(
            SelectionPath.Root,
            results,
            resultSelectionSet,
            containsErrors: false);

        // assert
        Assert.False(added);
        AssertDataElementStagingCleared(store, results.Length);
        AssertResultsRegisteredInOrder(store, results);
        Assert.Equal("{\"field\":null}", RenderData(store));
    }

    [Fact]
    public void AddPartialResults_Should_GrowRetainedStagingGeometrically_When_NoErrorBatchCrossesBoundaries()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              field: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateEmptyStore(schema, "{ field }", resultArena, out var resultSelectionSet);

        // act
        var retained256 = AddBatch(256, "retained256");
        var retained512 = AddBatch(257, "retained512Start");
        var reused512 = AddBatch(512, "retained512End");
        var retained1024 = AddBatch(513, "retained1024Start");
        var reused1024 = AddBatch(1024, "retained1024End");
        var afterFallback = AddBatch(1025, "pooled");

        // assert
        Assert.Equal(256, retained256.Length);
        Assert.Equal(512, retained512.Length);
        Assert.NotSame(retained256, retained512);
        Assert.Same(retained512, reused512);
        Assert.Equal(1024, retained1024.Length);
        Assert.NotSame(retained512, retained1024);
        Assert.Same(retained1024, reused1024);
        Assert.Same(retained1024, afterFallback);
        AssertDataElementStagingCleared(store, retained1024.Length);

        SourceResultElement[] AddBatch(int count, string valuePrefix)
        {
            var results = CreateSourceSchemaResults(sourceArena, count, valuePrefix);
            Assert.True(
                store.AddPartialResults(
                    SelectionPath.Root,
                    results,
                    resultSelectionSet,
                    containsErrors: false));

            var staging = GetDataElementStaging(store);
            AssertDataElementStagingCleared(store, Math.Min(count, staging.Length));
            AssertResultsRegisteredInOrder(store, results);
            Assert.Equal(
                $"{{\"field\":\"{valuePrefix}-{count - 1}\"}}",
                RenderData(store));
            return staging;
        }
    }

    [Fact]
    public void AddErrors_Should_UseAliasesAndPreserveSiblings_When_NonNullFieldNullsParent()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              name: String
              sku: String!
            }
            """);
        var plan = PlanOperation(
            schema,
            "{ aliasedFoos: foos { aliasedId: id name aliasedSku: sku } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var elementSelectionSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Parse("{ aliasedId: id name aliasedSku: sku }")
                .Definitions
                .OfType<OperationDefinitionNode>()
                .Single()
                .SelectionSet,
            schema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload =
            """{"data":{"aliasedFoos":[{"name":"one","aliasedSku":"sku-1"},{"aliasedId":"2","name":"two","aliasedSku":"sku-2"}]}}"""u8
                .ToArray();
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);

        var error = ErrorBuilder.New()
            .SetMessage("boom")
            .Build();

        // act
        var completed = store.AddErrors(
            error,
            elementSelectionSet,
            global::HotChocolate.Path.Root.Append("aliasedFoos").Append(0));

        // assert
        Assert.True(added);
        Assert.True(completed);
        Assert.NotNull(store.Errors);
        Assert.Collection(
            store.Errors,
            error =>
            {
                Assert.Equal("boom", error.Message);
                Assert.Equal("aliasedFoos[0].aliasedId", error.Path?.Print());
            });
        RenderData(store).MatchInlineSnapshot(
            """
            {"aliasedFoos":[null,{"aliasedId":"2","name":"two","aliasedSku":"sku-2"}]}
            """);
    }

    [Fact]
    public void AddErrors_Should_NullFieldAndPreserveSiblings_When_FieldIsNullable()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foo: Foo
              sibling: String
            }

            type Foo {
              id: ID!
              value: String
            }
            """);
        var plan = PlanOperation(
            schema,
            "{ foo { id value } sibling }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var fieldSelectionSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Parse("{ value }")
                .Definitions
                .OfType<OperationDefinitionNode>()
                .Single()
                .SelectionSet,
            schema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload =
            """{"data":{"foo":{"id":"1"},"sibling":"sibling"}}"""u8
                .ToArray();
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);

        var error = ErrorBuilder.New()
            .SetMessage("boom")
            .Build();

        // act
        var completed = store.AddErrors(
            error,
            fieldSelectionSet,
            global::HotChocolate.Path.Root.Append("foo"));

        // assert
        Assert.True(added);
        Assert.True(completed);
        Assert.Collection(
            store.Errors!,
            error =>
            {
                Assert.Equal("boom", error.Message);
                Assert.Equal("foo.value", error.Path?.Print());
            });
        RenderData(store).MatchInlineSnapshot(
            """
            {"foo":{"id":"1","value":null},"sibling":"sibling"}
            """);
    }

    [Fact]
    public void FinalizeInaccessibleRuntimeTypes_Should_MaskNullableAbstractValue_AndPreserveStoredValue()
    {
        // arrange
        var schema = ComposeInaccessibleAbstractSchema("Foo");
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ value { __typename name } sibling }",
            """{"data":{"value":{"__typename":"Baz","name":"secret"},"sibling":"visible"}}""",
            resultArena,
            sourceArena);

        // act
        store.FinalizePocketedErrors();

        // assert
        RenderData(store).MatchInlineSnapshot(
            """
            {"value":null,"sibling":"visible"}
            """);
        var storedValue = store.Result.Data.GetProperty("value");
        Assert.Equal(JsonValueKind.Object, storedValue.ValueKind);
        Assert.Equal("Baz", storedValue.GetProperty("__typename").AssertString());
        Assert.Equal("secret", storedValue.GetProperty("name").AssertString());
        Assert.Null(store.Errors);
    }

    [Fact]
    public void FinalizeInaccessibleRuntimeTypes_Should_PropagateThroughNonNullAbstractField()
    {
        // arrange
        var schema = ComposeInaccessibleAbstractSchema("Foo!");
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ value { __typename name } sibling }",
            """{"data":{"value":{"__typename":"Baz","name":"secret"},"sibling":"visible"}}""",
            resultArena,
            sourceArena);

        // act
        store.FinalizePocketedErrors();

        // assert
        Assert.Equal("null", RenderData(store));
        Assert.Null(store.Errors);
    }

    [Theory]
    [InlineData("[Foo]", "{\"value\":[null,{\"__typename\":\"Qux\",\"name\":\"visible\"}],\"sibling\":\"visible\"}")]
    [InlineData("[Foo!]", "{\"value\":null,\"sibling\":\"visible\"}")]
    [InlineData("[Foo!]!", "null")]
    public void FinalizeInaccessibleRuntimeTypes_Should_PropagateAccordingToListNullability(
        string fieldType,
        string expected)
    {
        // arrange
        var schema = ComposeInaccessibleAbstractSchema(fieldType);
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ value { __typename name } sibling }",
            """
            {"data":{"value":[
              {"__typename":"Baz","name":"secret"},
              {"__typename":"Qux","name":"visible"}],
              "sibling":"visible"}}
            """,
            resultArena,
            sourceArena);

        // act
        store.FinalizePocketedErrors();

        // assert
        Assert.Equal(expected, RenderData(store));
        Assert.Null(store.Errors);
    }

    [Fact]
    public void AddErrors_Should_UseAliasesInErrorPath()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foo: Foo
            }

            type Foo {
              id: ID!
              value: String
            }
            """);
        var plan = PlanOperation(
            schema,
            "{ aliasedFoo: foo { id aliasedValue: value } }");
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var fieldSelectionSet = ResultSelectionSet.Create(
            Utf8GraphQLParser.Parse("{ aliasedValue: value }")
                .Definitions
                .OfType<OperationDefinitionNode>()
                .Single()
                .SelectionSet,
            schema);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload =
            """{"data":{"aliasedFoo":{"id":"1","aliasedValue":"value"}}}"""u8.ToArray();
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);

        var error = ErrorBuilder.New()
            .SetMessage("boom")
            .Build();

        // act
        var completed = store.AddErrors(
            error,
            fieldSelectionSet,
            global::HotChocolate.Path.Root.Append("aliasedFoo"));

        // assert
        Assert.True(added);
        Assert.True(completed);
        Assert.Collection(
            store.Errors!,
            error => Assert.Equal("aliasedFoo.aliasedValue", error.Path?.Print()));
    }

    [Fact]
    public void Reset_Should_ClearAccumulatedErrors_When_ReusedForNextEvent()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    id
                }
            }
            """);

        using var initialArena = new MemoryArena();
        using var store = new FetchResultStore();
        store.Initialize(
            initialArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        store.AddError(ErrorBuilder.New().SetMessage("event 1").Build());
        Assert.Collection(
            store.Errors!,
            static error => Assert.Equal("event 1", error.Message));

        // act
        store.Reset(new MemoryArena());

        // assert
        Assert.Empty(store.Errors!);

        store.AddError(ErrorBuilder.New().SetMessage("event 2").Build());
        Assert.Collection(
            store.Errors!,
            static error => Assert.Equal("event 2", error.Message));
    }

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
            [],
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

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_ReinitializeDeduplicationTable_When_Reused()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var firstEntries = new[]
        {
            CreateVariableValues(
                source,
                Path(1),
                Field("__fusion_1_id", new StringValueNode("shared"))),
            CreateVariableValues(
                source,
                Path(2),
                Field("__fusion_1_id", new StringValueNode("first-only")))
        };
        var secondEntries = new[]
        {
            CreateVariableValues(
                source,
                Path(3),
                Field("__fusion_1_id", new StringValueNode("shared"))),
            CreateVariableValues(
                source,
                Path(4),
                Field("__fusion_1_id", new StringValueNode("second-only")))
        };

        // act
        var first = target.CreateVariableValueSetsFromSnapshot(
            [.. firstEntries],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);
        var second = target.CreateVariableValueSetsFromSnapshot(
            [.. secondEntries],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        ("first:\n" + Normalize(first) + "\nsecond:\n" + Normalize(second)).MatchInlineSnapshot(
            """
            first:
            path=[1]; additional=[]; values={"__fusion_1_id":"shared"}
            path=[2]; additional=[]; values={"__fusion_1_id":"first-only"}
            second:
            path=[3]; additional=[]; values={"__fusion_1_id":"shared"}
            path=[4]; additional=[]; values={"__fusion_1_id":"second-only"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_ReinitializeDeduplicationTable_When_SparseTrackingOverflows()
    {
        // arrange
        const int entryCount = 17;
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        var entries = new VariableValues[entryCount];

        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = CreateVariableValues(
                source,
                Path(i + 1),
                Field("__fusion_1_id", new StringValueNode($"value-{i}")));
        }

        var reusedEntry = CreateVariableValues(
            source,
            Path(101),
            Field("__fusion_1_id", new StringValueNode("value-16")));

        // act
        var overflowed = target.CreateVariableValueSetsFromSnapshot(
            [.. entries],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);
        var reused = target.CreateVariableValueSetsFromSnapshot(
            [reusedEntry],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        var expected = Enumerable
            .Range(0, entryCount)
            .Select(static i =>
                $"path=[{i + 1}]; additional=[]; values={{\"__fusion_1_id\":\"value-{i}\"}}")
            .Append(
                "path=[101]; additional=[]; values={\"__fusion_1_id\":\"value-16\"}")
            .ToArray();
        var actual = overflowed
            .Concat(reused)
            .Select(Describe)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_ReinitializeDeduplicationTable_When_GrowthIsFollowedByReuse()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();

        // The serialized values have distinct full hashes whose low eight bits are all E3.
        // This fills one four-entry bucket and forces growth before the hashes redistribute.
        string[] values = ["value-1", "value-43", "value-100", "value-188", "value-221"];
        var entries = new VariableValues[values.Length];
        var reusedEntries = new VariableValues[values.Length];

        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = CreateVariableValues(
                source,
                Path(i + 1),
                Field("__fusion_1_id", new StringValueNode(values[i])));
            reusedEntries[i] = CreateVariableValues(
                source,
                Path(i + 101),
                Field("__fusion_1_id", new StringValueNode(values[i])));
        }

        // act
        var grown = target.CreateVariableValueSetsFromSnapshot(
            [.. entries],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);
        var reused = target.CreateVariableValueSetsFromSnapshot(
            [.. reusedEntries],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        var expected = values
            .Select(static value => $"{{\"__fusion_1_id\":\"{value}\"}}")
            .Concat(values.Select(static value => $"{{\"__fusion_1_id\":\"{value}\"}}"))
            .ToArray();
        var actual = grown
            .Select(static entry => Normalize(entry.Values))
            .Concat(reused.Select(static entry => Normalize(entry.Values)))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateVariableValueSetsFromSnapshot_Should_ReinitializeDeduplicationTable_When_PreviousCallThrows()
    {
        // arrange
        using var source = new FetchResultStore();
        using var target = new FetchResultStore();
        using var malformedWriter = new ChunkedArrayWriter();

        var valid = CreateVariableValues(
            source,
            Path(1),
            Field("__fusion_1_id", new StringValueNode("shared")));
        var malformedJson = """{"__fusion_1_id":"""u8;
        malformedJson.CopyTo(malformedWriter.GetSpan(malformedJson.Length));
        malformedWriter.Advance(malformedJson.Length);
        var malformed = new VariableValues(
            Path(2),
            JsonSegment.Create(malformedWriter, 0, malformedJson.Length));

        Assert.ThrowsAny<JsonException>(
            () => target.CreateVariableValueSetsFromSnapshot(
                [valid, malformed],
                ImportedKeys("__fusion_1_id"),
                [],
                [Requirement("__fusion_1_id")]));

        // act
        var healed = target.CreateVariableValueSetsFromSnapshot(
            [valid],
            ImportedKeys("__fusion_1_id"),
            [],
            [Requirement("__fusion_1_id")]);

        // assert
        Normalize(healed).MatchInlineSnapshot(
            """
            path=[1]; additional=[]; values={"__fusion_1_id":"shared"}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_ShipNestedNull_When_NonNullInputFieldValueIsNull()
    {
        // arrange
        // The requirement map projects 'name' into the non-null input field
        // 'FooKeyInput.name', but nested input-object field nullability is not
        // enforced during projection, so the null value ships and the downstream
        // subgraph owns it. No element is skipped.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
              fooByKey(key: FooKeyInput): Foo
            }

            type Foo {
              id: ID!
              name: String
            }

            input FooKeyInput {
              name: String!
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id name } }",
            """{"data":{"foos":[{"id":"1","name":null},{"id":"2","name":"n"}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [Requirement(schema, "__fusion_1_key", "{ name }", new NamedTypeNode("FooKeyInput"))]);

        // assert
        Assert.Collection(
            result,
            entry => Normalize(entry.Values).MatchInlineSnapshot(
                """
                {"__fusion_1_key":{"name":null}}
                """),
            entry => Normalize(entry.Values).MatchInlineSnapshot(
                """
                {"__fusion_1_key":{"name":"n"}}
                """));
    }

    [Fact]
    public void CreateVariableValueSets_Should_ResolveInvariantNames_When_SelectionSetOrdinalsDiffer()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              nodes: [Node]
            }

            interface Node {
              target: Target
            }

            type A implements Node {
              a: String
              target: Target
            }

            type B implements Node {
              target: Target
              b: String
            }

            type Target {
              pad: String
              common: String
              aliasSource: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            """
            {
              nodes {
                __typename
                ... on A {
                  a
                  target {
                    pad
                    common
                    __fusion_internal_alias: aliasSource
                  }
                }
                ... on B {
                  target {
                    __fusion_internal_alias: aliasSource
                    pad
                    common
                  }
                  b
                }
              }
            }
            """,
            """
            {"data":{"nodes":[
              {"__typename":"A","a":"a-0","target":{"pad":"pad-a-0","common":"common-a-0","__fusion_internal_alias":"alias-a-0"}},
              {"__typename":"A","a":"a-1","target":{"pad":"pad-a-1","common":"common-a-1","__fusion_internal_alias":"alias-a-1"}},
              {"__typename":"B","target":{"__fusion_internal_alias":"alias-b-2","pad":"pad-b-2","common":"common-b-2"},"b":"b-2"}
            ]}}
            """,
            resultArena,
            sourceArena);

        var aliasRequirement = new OperationRequirement(
            "__fusion_2_alias",
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new FieldSelectionMapParser("aliasSource").Parse(),
            "__fusion_internal_alias");

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("nodes").AppendField("target"),
            [],
            [
                Requirement(schema, "__fusion_1_common", "common", new NamedTypeNode("String")),
                aliasRequirement
            ]);

        // assert
        RenderVariableValueSets(store, result).MatchInlineSnapshot(
            """
            Path: nodes[0].target
            Additional paths: []
            Variables: {"__fusion_1_common":"common-a-0","__fusion_2_alias":"alias-a-0"}
            Path: nodes[1].target
            Additional paths: []
            Variables: {"__fusion_1_common":"common-a-1","__fusion_2_alias":"alias-a-1"}
            Path: nodes[2].target
            Additional paths: []
            Variables: {"__fusion_1_common":"common-b-2","__fusion_2_alias":"alias-b-2"}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_ResolveHit_When_PreviousSelectionSetMemoizedMiss()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              nodes: [Node]
            }

            interface Node {
              common: String
            }

            type A implements Node {
              common: String
            }

            type B implements Node {
              common: String
              onlyB: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            """
            {
              nodes {
                __typename
                common
                ... on B {
                  onlyB
                }
              }
            }
            """,
            """
            {"data":{"nodes":[
              {"__typename":"A","common":"a-0"},
              {"__typename":"A","common":"a-1"},
              {"__typename":"B","common":"b-2","onlyB":"hit"}
            ]}}
            """,
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("nodes"),
            [],
            [Requirement(schema, "__fusion_1_only_b", "onlyB", new NamedTypeNode("String"))]);

        // assert
        RenderVariableValueSets(store, result).MatchInlineSnapshot(
            """
            Path: nodes[2]
            Additional paths: []
            Variables: {"__fusion_1_only_b":"hit"}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_ReadInternalAlias_When_RequirementHasInternalAlias()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { __fusion_internal_id: id } }",
            """{"data":{"foos":[{"__fusion_internal_id":"1"}]}}""",
            resultArena,
            sourceArena);

        var requirement = new OperationRequirement(
            "__fusion_1_id",
            new NamedTypeNode("String"),
            SelectionPath.Root,
            new FieldSelectionMapParser("id").Parse(),
            "__fusion_internal_id");

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [requirement]);

        // assert
        Assert.Collection(
            result,
            entry => Normalize(entry.Values).MatchInlineSnapshot(
                """
                {"__fusion_1_id":"1"}
                """));
    }

    [Fact]
    public void CreateVariableValueSets_Should_WriteNullInputField_When_NullableInputFieldValueIsNull()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
              fooByKey(key: FooKeyInput): Foo
            }

            type Foo {
              id: ID!
              name: String
            }

            input FooKeyInput {
              name: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id name } }",
            """{"data":{"foos":[{"id":"1","name":null}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [Requirement(schema, "__fusion_1_key", "{ name }", new NamedTypeNode("FooKeyInput"))]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_key":{"name":null}}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_SkipElement_When_NonNullRequirementPathResolvesNull()
    {
        // arrange
        // A null intermediate resolves 'brand.name' to null, which cannot
        // satisfy the non-null requirement type.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              brand: Brand
            }

            type Brand {
              name: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id brand { name } } }",
            """{"data":{"foos":[{"id":"1","brand":null}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [
                Requirement(
                    schema,
                    "__fusion_1_bn",
                    "brand.name",
                    new NonNullTypeNode(new NamedTypeNode("String")))
            ]);

        // assert
        Assert.True(result.IsDefaultOrEmpty);
    }

    [Fact]
    public void CreateVariableValueSets_Should_SkipElement_When_NonNullListElementValueIsNull()
    {
        // arrange
        // The requirement type '[String!]' does not allow null elements, so the
        // element with a null tag is skipped.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              tags: [String]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id tags } }",
            """{"data":{"foos":[{"id":"1","tags":["a",null]},{"id":"2","tags":["b"]}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [
                Requirement(
                    schema,
                    "__fusion_1_tags",
                    "tags",
                    new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("String"))))
            ]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_tags":["b"]}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_WriteNullListElement_When_NullableListElementValueIsNull()
    {
        // arrange
        // The requirement type '[String]' allows null elements, so the null
        // tag ships as part of the list value.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              tags: [String]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id tags } }",
            """{"data":{"foos":[{"id":"1","tags":["a",null]}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [
                Requirement(
                    schema,
                    "__fusion_1_tags",
                    "tags",
                    new ListTypeNode(new NamedTypeNode("String")))
            ]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_tags":["a",null]}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_CopyObjectScalarElements_When_RequirementIsObjectValuedScalarList()
    {
        // arrange
        // Elements of a 'JSON' scalar list are JSON objects, which the
        // list-typed requirement routes through the type-aware mapper.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              dimensions: [JSON!]
            }

            scalar JSON
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id dimensions } }",
            """{"data":{"foos":[{"id":"1","dimensions":[{"width":1},{"height":2}]}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [
                Requirement(
                    schema,
                    "__fusion_1_dims",
                    "dimensions",
                    new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("JSON"))))
            ]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_dims":[{"width":1},{"height":2}]}
            """);
    }

    [Fact]
    public void CreateVariableValueSets_Should_CopyObjectScalarValue_When_RequirementPathResolvesObjectValuedScalar()
    {
        // arrange
        // The multi-segment path routes through the type-aware mapper, whose
        // leaf value is a JSON object of the 'JSON' scalar.
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID!
              meta: Meta
            }

            type Meta {
              size: JSON
            }

            scalar JSON
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id meta { size } } }",
            """{"data":{"foos":[{"id":"1","meta":{"size":{"width":1,"height":2}}}]}}""",
            resultArena,
            sourceArena);

        // act
        var result = store.CreateVariableValueSets(
            SelectionPath.Root.AppendField("foos"),
            [],
            [Requirement(schema, "__fusion_1_size", "meta.size", new NamedTypeNode("JSON"))]);

        // assert
        var entry = Assert.Single(result);
        Normalize(entry.Values).MatchInlineSnapshot(
            """
            {"__fusion_1_size":{"width":1,"height":2}}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromResolvedVariables_Should_WriteForwardedValuesBeforeFourRequirements_When_FastPathIsUsed()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID
              sku: String
              optional: String
              required: String
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { __fusion_internal_id: id sku optional required } }",
            """
            {"data":{"foos":[
              {"__fusion_internal_id":"1","sku":"s1","optional":null,"required":"r1"},
              {"__fusion_internal_id":"1","sku":"s1","optional":null,"required":"r1"},
              {"__fusion_internal_id":"2","sku":"s2","optional":"o2","required":null}
            ]}}
            """,
            resultArena,
            sourceArena);

        var requirements = new OperationRequirement[]
        {
            new(
                "__fusion_1_id",
                new NamedTypeNode("String"),
                SelectionPath.Root,
                new FieldSelectionMapParser("id").Parse(),
                "__fusion_internal_id"),
            Requirement(schema, "__fusion_2_sku", "sku", new NamedTypeNode("String")),
            Requirement(schema, "__fusion_3_optional", "optional", new NamedTypeNode("String")),
            Requirement(
                schema,
                "__fusion_4_required",
                "required",
                new NonNullTypeNode(new NamedTypeNode("String")))
        };
        var forwardedVariables = new ForwardedVariableValue[]
        {
            new("limit", new IntValueNode(10)),
            new("nullable", NullValueNode.Default),
            new("limit", new IntValueNode(20))
        };

        // act
        var result = store.CreateVariableValueSetsFromResolvedVariables(
            SelectionPath.Root.AppendField("foos"),
            forwardedVariables,
            requirements);

        // assert
        var entry = Assert.Single(result);
        var operation = store.Result.Data.Operation;
        var additionalPath = Assert.Single(entry.AdditionalPaths.AsSpan().ToArray());
        var rawVariables = Encoding.UTF8.GetString(entry.Values.AsSequence().ToArray());
        $"""
        Path: {entry.Path.ToPath(operation).Print()}
        Additional path: {additionalPath.ToPath(operation).Print()}
        Variables bytes: {rawVariables}
        """.MatchInlineSnapshot(
            """
            Path: foos[0]
            Additional path: foos[1]
            Variables bytes: {"limit":10,"nullable":null,"limit":20,"__fusion_1_id":"1","__fusion_2_sku":"s1","__fusion_3_optional":null,"__fusion_4_required":"r1"}
            """);
    }

    [Fact]
    public void CreateVariableValueSetsFromResolvedVariables_Should_RejectNullListElement_When_FourthRequirementForcesSlowPath()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              foos: [Foo]
            }

            type Foo {
              id: ID
              sku: String
              category: String
              tags: [String]
            }
            """);

        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var store = CreateLiveStore(
            schema,
            "{ foos { id sku category tags } }",
            """
            {"data":{"foos":[
              {"id":"1","sku":"s1","category":"c1","tags":["bad",null]},
              {"id":"2","sku":"s2","category":"c2","tags":["a","b"]}
            ]}}
            """,
            resultArena,
            sourceArena);

        var requirements = new OperationRequirement[]
        {
            Requirement(schema, "__fusion_1_id", "id", new NamedTypeNode("String")),
            Requirement(schema, "__fusion_2_sku", "sku", new NamedTypeNode("String")),
            Requirement(schema, "__fusion_3_category", "category", new NamedTypeNode("String")),
            Requirement(
                schema,
                "__fusion_4_tags",
                "tags",
                new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("String"))))
        };

        // act
        var result = store.CreateVariableValueSetsFromResolvedVariables(
            SelectionPath.Root.AppendField("foos"),
            [new ForwardedVariableValue("tenant", new StringValueNode("acme"))],
            requirements);

        // assert
        RenderVariableValueSets(store, result).MatchInlineSnapshot(
            """
            Path: foos[1]
            Additional paths: []
            Variables: {"tenant":"acme","__fusion_1_id":"2","__fusion_2_sku":"s2","__fusion_3_category":"c2","__fusion_4_tags":["a","b"]}
            """);
    }

    private static FetchResultStore CreateLiveStore(
        FusionSchemaDefinition schema,
        string operationText,
        string payloadJson,
        MemoryArena resultArena,
        MemoryArena sourceArena)
    {
        var plan = PlanOperation(schema, operationText);
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);

        var payload = Encoding.UTF8.GetBytes(payloadJson);
        var document = SourceResultDocument.Parse(sourceArena, payload, payload.Length);
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);
        Assert.True(added);

        return store;
    }

    private static FetchResultStore CreateEmptyStore(
        FusionSchemaDefinition schema,
        string operationText,
        MemoryArena resultArena,
        out ResultSelectionSet resultSelectionSet)
    {
        var plan = PlanOperation(schema, operationText);
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: 0,
            deferFlags: 0,
            pathSegmentLocalPoolCapacity: 16);
        resultSelectionSet = node.ResultSelectionSet;
        return store;
    }

    private static FusionSchemaDefinition ComposeInaccessibleAbstractSchema(string fieldType)
        => ComposeSchema(
            $$"""
            # name: test
            type Query {
              value: {{fieldType}}
              sibling: String
            }

            interface Foo {
              name: String
            }

            type Baz implements Foo @inaccessible {
              name: String
            }

            type Qux implements Foo {
              name: String
            }
            """);

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
            new PathNode(new PathSegmentNode(new FusionNameNode(key))),
            null);

    private static OperationRequirement Requirement(
        FusionSchemaDefinition schema,
        string key,
        string map,
        ITypeNode type)
        => new(
            key,
            type,
            SelectionPath.Root,
            new FieldSelectionMapParser(map).Parse(),
            null);

    private static HashSet<string> ImportedKeys(params string[] keys)
        => new(keys, StringComparer.Ordinal);

    private static CompactPath Path(params int[] segments)
    {
        var buffer = new int[segments.Length + 1];
        buffer[0] = segments.Length;
        segments.CopyTo(buffer.AsSpan(1));
        return new CompactPath(buffer);
    }

    private static SourceSchemaResult CreateSourceSchemaResult(
        IMemoryArena arena,
        CompactPath path)
    {
        var document = SourceResultDocument.Parse(arena, s_fieldPayload, s_fieldPayload.Length);
        return new SourceSchemaResult(path, document);
    }

    private static SourceSchemaResult CreateSourceSchemaResult(
        IMemoryArena arena,
        CompactPath path,
        string payloadJson)
    {
        var payload = Encoding.UTF8.GetBytes(payloadJson);
        var document = SourceResultDocument.Parse(arena, payload, payload.Length);
        return new SourceSchemaResult(path, document);
    }

    private static SourceSchemaResult[] CreateSourceSchemaResults(
        IMemoryArena arena,
        int count,
        string valuePrefix)
    {
        var results = new SourceSchemaResult[count];

        for (var i = 0; i < results.Length; i++)
        {
            results[i] = CreateSourceSchemaResult(
                arena,
                CompactPath.Root,
                $"{{\"data\":{{\"field\":\"{valuePrefix}-{i}\"}}}}");
        }

        return results;
    }

    private static SourceResultElement[] GetDataElementStaging(FetchResultStore store)
        => Assert.IsType<SourceResultElement[]>(s_dataElementStagingField.GetValue(store));

    private static void AssertDataElementStagingCleared(FetchResultStore store, int count)
    {
        var staging = GetDataElementStaging(store);
        Assert.True(staging.Length >= count);

        for (var i = 0; i < count; i++)
        {
            Assert.Null(staging[i]._parent);
        }
    }

    private static void AssertResultsRegisteredInOrder(
        FetchResultStore store,
        ReadOnlySpan<SourceSchemaResult> results)
    {
        var owners = store.MemoryOwners;
        var offset = owners.Count - results.Length;
        Assert.True(offset >= 0);

        for (var i = 0; i < results.Length; i++)
        {
            Assert.Same(results[i], owners[offset + i]);
        }
    }

    private static string Normalize(JsonSegment segment)
    {
        using var document = JsonDocument.Parse(segment.AsSequence());
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static string Normalize(IEnumerable<VariableValues> entries)
        => string.Join("\n", entries.Select(Describe));

    private static string Describe(VariableValues entry)
    {
        var path = string.Join(",", entry.Path.Segments.ToArray());
        var additionalPaths = string.Join(
            "|",
            entry.AdditionalPaths
                .AsSpan()
                .ToArray()
                .Select(static path => string.Join(",", path.Segments.ToArray())));

        return $"path=[{path}]; additional=[{additionalPaths}]; values={Normalize(entry.Values)}";
    }

    private static string RenderVariableValueSets(
        FetchResultStore store,
        IEnumerable<VariableValues> entries)
    {
        var operation = store.Result.Data.Operation;
        var output = new StringBuilder();

        foreach (var entry in entries)
        {
            output.Append("Path: ");
            output.AppendLine(entry.Path.ToPath(operation).Print());
            output.Append("Additional paths: [");

            var additionalPaths = entry.AdditionalPaths.AsSpan();
            for (var i = 0; i < additionalPaths.Length; i++)
            {
                if (i > 0)
                {
                    output.Append(", ");
                }

                output.Append(additionalPaths[i].ToPath(operation).Print());
            }

            output.AppendLine("]");
            output.Append("Variables: ");
            output.AppendLine(Normalize(entry.Values));
        }

        return output.ToString().TrimEnd();
    }

    private static string RenderData(FetchResultStore store)
    {
        using var buffer = new PooledArrayWriter();
        store.Result.Data.WriteTo(buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
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
