using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentTests : FusionTestBase
{
    [Fact]
    public void Initialize_Basic_Result()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // act
        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);

        // assert
        Assert.Equal(1, compositeResult.Data.GetPropertyCount());
        Assert.NotNull(compositeResult.Data.SelectionSet);

        var propertyValue = compositeResult.Data.GetProperty("productBySlug");
        Assert.Equal("productBySlug", propertyValue.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, propertyValue.ValueKind);
    }

    [Fact]
    public void Add_Object_From_SelectionSet()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        Assert.Equal("productBySlug", productBySlug.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, productBySlug.ValueKind);
        Assert.False(productBySlugSelection.IsLeaf);

        // act
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        // assert
        Assert.Equal(JsonValueKind.Object, productBySlug.ValueKind);
    }

    [Fact]
    public void Add_SourceResult_Leaf_Value()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
            {
              "id": 1,
              "name": "Abc"
            }
            """u8.ToArray();
        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);

        // act
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // assert
        Assert.Equal(JsonValueKind.Number, productBySlug.GetProperty("id").ValueKind);
        Assert.Equal(JsonValueKind.String, productBySlug.GetProperty("name").ValueKind);
    }

    [Fact]
    public void Invalidate_Object()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        // act
        productBySlug.Invalidate();

        // assert
        Assert.True(productBySlug.IsInvalidated);
    }

    [Fact]
    public void Invalidate_Data()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);

        // act
        compositeResult.Data.Invalidate();

        // assert
        Assert.True(compositeResult.Data.IsInvalidated);
    }

    [Fact]
    public void WriteTo_Should_MaskOnlyMarkedLogicalReference_When_ValueIsShared()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var plan = PlanOperation(
            schema,
            """
            {
              first: productBySlug(slug: "1") { id name }
              second: productBySlug(slug: "1") { id name }
            }
            """);
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var first = document.Data.GetProperty("first");
        var second = document.Data.GetProperty("second");
        var selectionSet = plan.Operation.GetSelectionSet(first.AssertSelection());
        var shared = document.CreateObject(first.Cursor, selectionSet);
        document.AssignCompositeValue(first, shared);
        document.AssignCompositeValue(second, shared);

        var payload = """{"id":1,"name":"shared"}"""u8.ToArray();
        var source = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            payload,
            payload.Length);
        shared.GetProperty("id").SetLeafValue(source.Root.GetProperty("id"));
        shared.GetProperty("name").SetLeafValue(source.Root.GetProperty("name"));

        // act
        first.SetNullMarker();

        // assert
        using var buffer = new PooledArrayWriter();
        document.Data.WriteTo(buffer);
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchInlineSnapshot(
            """
            {"first":null,"second":{"id":1,"name":"shared"}}
            """);
        Assert.Equal("shared", first.GetProperty("name").AssertString());
        Assert.Equal("shared", second.GetProperty("name").AssertString());
    }

    [Fact]
    public void NullMarkerState_Should_PreserveMarkers_AcrossFinalizationTransitions()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var plan = PlanOperation(schema, "{ productBySlug(slug: \"1\") { id } }");
        var document = new CompositeResultDocument(
            CommonTestExtensions.CreateArena(),
            plan.Operation,
            0);
        var states = new List<string>();

        // act
        RecordState("none");

        document.RequireNullMarkerFinalization();
        RecordState("requires");

        document.CompleteNullMarkerFinalization();
        RecordState("complete without marker");

        document.Data.SetNullMarker();
        RecordState("has marker");

        document.RequireNullMarkerFinalization();
        RecordState("both after later require");

        document.CompleteNullMarkerFinalization();
        RecordState("complete preserves marker");

        // assert
        string.Join(Environment.NewLine, states).MatchInlineSnapshot(
            """
            none: requires=False, markers=False
            requires: requires=True, markers=False
            complete without marker: requires=False, markers=False
            has marker: requires=False, markers=True
            both after later require: requires=True, markers=True
            complete preserves marker: requires=False, markers=True
            """);

        void RecordState(string name)
            => states.Add(
                $"{name}: requires={document.RequiresNullMarkerFinalization}, "
                + $"markers={document.HasNullMarkers}");
    }

    [Fact]
    public void Enumerate_Object()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
                {
                  "id": 1,
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var enumerator = productBySlug.EnumerateObject();

        // assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("id", enumerator.Current.Name);
        Assert.Equal(1, enumerator.Current.Value.GetInt32());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("name", enumerator.Current.Name);
        Assert.Equal("Abc", enumerator.Current.Value.AssertString());

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Enumerate_Array()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        var i = 0;

        // act
        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));
        }

        // assert
        using var enumerator = nodes.EnumerateArray().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Abc", enumerator.Current.GetProperty("name").AssertString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Def", enumerator.Current.GetProperty("name").AssertString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Ghi", enumerator.Current.GetProperty("name").AssertString());

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Path_Fields_Only()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
                {
                  "id": 1,
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var path = productBySlug.GetProperty("name").Path;

        // assert
        Assert.Equal("productBySlug.name", path.ToString());
    }

    [Fact]
    public void Path_Array_Index()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(1);

        var element = nodes[0];
        element.SetObjectValue(nodesSelectionSet);

        var name = element.GetProperty("name");

        var result =
            """
                {
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        name.SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var path = name.Path;

        // assert
        Assert.Equal("users.nodes[0].name", path.ToString());
    }

    [Fact]
    public void Write_Document_To_BufferWriter()
    {
        // arrange
        using var buffer = new PooledArrayWriter();

        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        var i = 0;

        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            var propertyRow = compositeResult._metaDb.Get(name.Cursor - 1);
            Assert.Equal(name.AssertSelection().Id, propertyRow.OperationReferenceId);

            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));

            var valueRow = compositeResult._metaDb.Get(name.Cursor);
            Assert.Equal(0, valueRow.OperationReferenceId);
            Assert.Equal(ElementTokenType.String, valueRow.TokenType);
        }

        // act
        var operationResultData = new OperationResultData(
            compositeResult,
            compositeResult.Data.IsNullOrInvalidated,
            compositeResult,
            compositeResult);
        var operationResult = new OperationResult(
            operationResultData);

        new JsonResultFormatter(indented: true).Format(operationResult, buffer);

        // assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        json.MatchSnapshot();
    }

    [Theory]
    [InlineData(false, "{\"productBySlug\":{\"id\":[null,true,false]}}")]
    [InlineData(
        true,
        "{\n  \"productBySlug\": {\n    \"id\": [\n      null,\n      true,\n      false\n    ]\n  }\n}")]
    public void WriteDataTo_Should_PreserveNestedValues_When_RowsCrossChunkBoundaries(
        bool indented,
        string expected)
    {
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
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var rootValue = document.Data.GetProperty("productBySlug");
        var rootSelection = rootValue.AssertSelection();
        var childSelectionSet = plan.Operation.GetSelectionSet(rootSelection);
        Assert.Equal(1, childSelectionSet.Selections.Length);
        var childSelection = childSelectionSet.Selections[0];
        ref var metaDb = ref document._metaDb;

        var firstChunkBoundary = CompositeResultDocument.Cursor.RowsPerChunkFor(0) - 2;

        while (metaDb.NextCursor.Index < firstChunkBoundary)
        {
            metaDb.AppendNull(parentRow: 0);
        }

        var objectStart = metaDb.AppendStartObject(
            rootValue.Cursor.Value,
            childSelectionSet.Id,
            propertyCount: 1,
            flags: CompositeResultDocument.ElementFlags.None);
        var childProperty = metaDb.AppendEmptyPropertyWithNullValue(
            objectStart.Value,
            childSelection.Id,
            flags: CompositeResultDocument.ElementFlags.None);
        var childValue = childProperty + 1;
        metaDb.AppendEndObject();

        var arrayBoundaryChunk = metaDb.NextCursor.Chunk;
        var arrayBoundary = CompositeResultDocument.Cursor.From(
            arrayBoundaryChunk,
            CompositeResultDocument.Cursor.RowsPerChunkFor(arrayBoundaryChunk) - 1);

        while (metaDb.NextCursor.Index < arrayBoundary.Index)
        {
            metaDb.AppendNull(parentRow: 0);
        }

        var arrayStart = metaDb.AppendStartArray(
            childValue.Value,
            length: 3,
            flags: CompositeResultDocument.ElementFlags.None);
        Assert.Equal(arrayBoundary, arrayStart);
        metaDb.AppendNull(arrayStart.Value);
        metaDb.Append(ElementTokenType.True, parentRow: arrayStart.Value);
        metaDb.Append(ElementTokenType.False, parentRow: arrayStart.Value);
        metaDb.AppendEndArray();

        metaDb.Replace(
            childValue,
            ElementTokenType.Reference,
            location: arrayStart.Value,
            parentRow: childProperty.Value);
        metaDb.Replace(
            rootValue.Cursor,
            ElementTokenType.Reference,
            location: objectStart.Value,
            parentRow: (rootValue.Cursor - 1).Value);

        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = indented, SkipValidation = true });

        document.WriteDataTo(writer);

        Assert.Equal(expected, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void WriteDataTo_Should_SkipInternalAndExcludedProperties_When_SkippedPairCrossesChunkBoundary()
    {
        const int propertyCount = 30;
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
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var rootValue = document.Data.GetProperty("productBySlug");
        var childSelectionSet = plan.Operation.GetSelectionSet(rootValue.AssertSelection());
        Assert.Equal(1, childSelectionSet.Selections.Length);
        var childSelection = childSelectionSet.Selections[0];
        ref var metaDb = ref document._metaDb;

        while (metaDb.NextCursor.Index < 5)
        {
            metaDb.AppendNull(parentRow: 0);
        }

        var objectStart = metaDb.AppendStartObject(
            rootValue.Cursor.Value,
            childSelectionSet.Id,
            propertyCount,
            CompositeResultDocument.ElementFlags.None);

        for (var i = 0; i < propertyCount; i++)
        {
            var flags = i switch
            {
                22 => CompositeResultDocument.ElementFlags.IsInternal,
                23 => CompositeResultDocument.ElementFlags.IsExcluded,
                _ => CompositeResultDocument.ElementFlags.None
            };

            metaDb.AppendEmptyPropertyWithNullValue(
                objectStart.Value,
                childSelection.Id,
                flags);
        }

        metaDb.AppendEndObject();
        metaDb.Replace(
            rootValue.Cursor,
            ElementTokenType.Reference,
            location: objectStart.Value,
            parentRow: (rootValue.Cursor - 1).Value);

        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = false, SkipValidation = true });

        document.WriteDataTo(writer);

        var expected = new StringBuilder("{\"productBySlug\":{");

        for (var i = 0; i < propertyCount - 2; i++)
        {
            if (i > 0)
            {
                expected.Append(',');
            }

            expected.Append("\"id\":null");
        }

        expected.Append("}}");
        Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void SetObjectValue_Should_MaterializeIdenticalSiblings_When_ManyObjectsShareSelectionSet()
    {
        // arrange
        // Each sibling object appends a 4-row template block (StartObject, name property row,
        // value row, EndObject) while the early MetaDb chunks hold far fewer rows than the 400
        // rows the siblings need, so some blocks must be applied through the chunk-spanning
        // copy. The act loop records the first block that spans a chunk boundary.
        const int nodeCount = 100;
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = document.Data.Operation;

        var users = document.Data.GetProperty("users");
        users.SetObjectValue(operation.GetSelectionSet(users.AssertSelection()));

        var nodes = users.GetProperty("nodes");
        var nodesSelectionSet = operation.GetSelectionSet(nodes.AssertSelection());
        nodes.SetArrayValue(nodeCount);

        var sourceJson = new StringBuilder("{");

        for (var i = 0; i < nodeCount; i++)
        {
            if (i > 0)
            {
                sourceJson.Append(',');
            }

            sourceJson.Append($"\"name{i}\":\"User {i}\"");
        }

        sourceJson.Append('}');
        var sourceBytes = Encoding.UTF8.GetBytes(sourceJson.ToString());
        var sourceResult = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            sourceBytes,
            sourceBytes.Length);

        // act
        var index = 0;
        var firstSplitIndex = -1;

        foreach (var element in nodes.EnumerateArray())
        {
            var blockStart = document._metaDb.NextCursor;
            element.SetObjectValue(nodesSelectionSet);
            var blockEnd = document._metaDb.NextCursor - 1;

            if (firstSplitIndex < 0 && blockStart.Chunk != blockEnd.Chunk)
            {
                firstSplitIndex = index;
            }

            element.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name" + index));
            index++;
        }

        // assert
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = false, SkipValidation = true });
        document.WriteDataTo(writer);

        var expected = new StringBuilder("{\"users\":{\"nodes\":[");

        for (var i = 0; i < nodeCount; i++)
        {
            if (i > 0)
            {
                expected.Append(',');
            }

            expected.Append($"{{\"name\":\"User {i}\"}}");
        }

        expected.Append("]}}");
        Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(buffer.WrittenSpan));
        Assert.True(
            firstSplitIndex >= 0,
            $"With {CompositeResultDocument.Cursor.RowsPerChunkFor(0)} rows in the first chunk "
            + $"and 4 rows per sibling, at least one of the {nodeCount} sibling blocks must "
            + "span a chunk boundary.");
        Assert.Equal(
            $"users.nodes[{firstSplitIndex}].name",
            nodes[firstSplitIndex].GetProperty("name").Path.ToString());
        Assert.Equal(
            $"users.nodes[{nodeCount - 1}].name",
            nodes[nodeCount - 1].GetProperty("name").Path.ToString());
    }

    [Fact]
    public void CreateObject_Should_NotLeakInclusionIntoSharedTemplate_When_IncludedDocumentBuildsTemplateFirst()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            query($flag: Boolean!) {
                productBySlug(slug: "1") {
                    id
                    name @include(if: $flag)
                }
            }
            """);

        // act
        // The first document builds the operation-shared template while the conditional
        // selection is included; the second document reuses that template with the opposite
        // include outcome.
        var includedJson = MaterializeProductResult(plan.Operation, ulong.MaxValue, 0);
        var excludedJson = MaterializeProductResult(plan.Operation, 0, 0);

        // assert
        Assert.Equal("""{"productBySlug":{"id":1,"name":"Abc"}}""", includedJson);
        Assert.Equal("""{"productBySlug":{"id":1}}""", excludedJson);
    }

    [Fact]
    public void CreateObject_Should_NotLeakExclusionIntoSharedTemplate_When_ExcludedDocumentBuildsTemplateFirst()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            query($flag: Boolean!) {
                productBySlug(slug: "1") {
                    id
                    name @include(if: $flag)
                }
            }
            """);

        // act
        // The first document builds the operation-shared template while the conditional
        // selection is excluded; the second document reuses that template with the opposite
        // include outcome.
        var excludedJson = MaterializeProductResult(plan.Operation, 0, 0);
        var includedJson = MaterializeProductResult(plan.Operation, ulong.MaxValue, 0);

        // assert
        Assert.Equal("""{"productBySlug":{"id":1}}""", excludedJson);
        Assert.Equal("""{"productBySlug":{"id":1,"name":"Abc"}}""", includedJson);
    }

    [Fact]
    public void CreateObject_Should_NotLeakDeferralIntoSharedTemplate_When_DeferredDocumentBuildsTemplateFirst()
    {
        // arrange
        // The planner splits statically deferred fragments into incremental plans, so a
        // deferred selection only shares a selection set (carrying its defer mask) in a
        // directly compiled operation, as is the case for the operation of an incremental
        // plan.
        var schema = CreateCompositeSchema();
        var document = Utf8GraphQLParser.Parse(
            """
            {
                productBySlug(slug: "1") {
                    id
                    ... @defer {
                        name
                    }
                }
            }
            """);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var operation = compiler.Compile("1", "1", operationDefinition);

        // act
        // The first document builds the operation-shared template while the deferred selection
        // is excluded from the initial payload; the second document reuses that template with
        // the deferral inactive.
        var deferredJson = MaterializeProductResult(operation, 0, ulong.MaxValue);
        var inlineJson = MaterializeProductResult(operation, 0, 0);

        // assert
        Assert.Equal("""{"productBySlug":{"id":1}}""", deferredJson);
        Assert.Equal("""{"productBySlug":{"id":1,"name":"Abc"}}""", inlineJson);
    }

    [Fact]
    public void CreateObject_Should_EmitRowsIdenticalToPerRowAppends_When_TemplateIsApplied()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    __typename
                    id
                    name
                }
            }
            """);

        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var productBySlug = document.Data.GetProperty("productBySlug");
        var selectionSet = plan.Operation.GetSelectionSet(productBySlug.AssertSelection());
        ref var metaDb = ref document._metaDb;

        // act
        // Materialize the same object twice with the same parent: once through the template
        // block copy and once through the per-row appenders the template rendering must stay
        // byte-identical to.
        var templateStart = document.CreateObject(productBySlug.Cursor, selectionSet).Cursor;

        var manualStart = metaDb.AppendStartObject(
            productBySlug.Cursor.Value,
            selectionSet.Id,
            selectionSet.Selections.Length,
            CompositeResultDocument.ElementFlags.None);

        foreach (var selection in selectionSet.Selections)
        {
            var flags = CompositeResultDocument.ElementFlags.None;

            if (selection.IsInternal)
            {
                flags |= CompositeResultDocument.ElementFlags.IsInternal;
            }

            if (selection.Type.Kind is not TypeKind.NonNull)
            {
                flags |= CompositeResultDocument.ElementFlags.IsNullable;
            }

            if (selection.IsEnumValue)
            {
                flags |= CompositeResultDocument.ElementFlags.IsEnumValue;
            }

            if (selection.Field.Name == "__typename")
            {
                var propertyCursor = metaDb.AppendEmptyProperty(manualStart.Value, selection.Id, flags);
                metaDb.Append(
                    ElementTokenType.String,
                    location: selectionSet.Id,
                    parentRow: propertyCursor.Value);
            }
            else
            {
                metaDb.AppendEmptyPropertyWithNullValue(manualStart.Value, selection.Id, flags);
            }
        }

        metaDb.AppendEndObject();

        // assert
        var rowCount = (selectionSet.Selections.Length * 2) + 2;
        var templateRows = new List<string>();
        var manualRows = new List<string>();

        for (var i = 0; i < rowCount; i++)
        {
            templateRows.Add(Describe(document._metaDb.Get(templateStart + i), i, templateStart.Value));
            manualRows.Add(Describe(document._metaDb.Get(manualStart + i), i, manualStart.Value));
        }

        Assert.Equal(manualRows, templateRows);

        static string Describe(CompositeResultDocument.DbRow row, int index, int startValue)
        {
            // Parent pointers are per-instance: row 0 points at the shared caller parent and
            // the remaining rows point into their own block, so those compare block-relative.
            var parent = index == 0
                ? row.Parent
                : row.Parent == 0
                    ? 0
                    : row.Parent - startValue;

            return $"{row.TokenType} parent={parent} "
                + $"opRef={row.OperationReferenceType}:{row.OperationReferenceId} "
                + $"flags={row.Flags} size={row.SizeOrLength} locOrRows={row.NumberOfRows} "
                + $"source={row.SourceDocumentId}";
        }
    }

    [Fact]
    public void CreateObject_Should_StampExclusionAcrossChunkSplit_When_SpanningSegmentBeginsWithValueRow()
    {
        // arrange
        // The 4 selections produce a 10-row block that is steered so exactly 4 rows land
        // before the chunk edge. The spanning segment then begins with the value row of the
        // second selection (its PropertyName row closed the previous chunk), and the
        // conditional selection's property row lies past the split, so the per-document
        // exclusion stamp must cross the chunk boundary.
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            query($flag: Boolean!) {
                productBySlug(slug: "1") {
                    id
                    name
                    c: name @include(if: $flag)
                    d: name
                }
            }
            """);

        // act
        var (includedJson, includedGeometry) =
            MaterializeSteeredProductResult(plan.Operation, ulong.MaxValue, 4);
        var (excludedJson, excludedGeometry) =
            MaterializeSteeredProductResult(plan.Operation, 0, 4);

        // assert
        // The document rewriter orders the conditional selection last, so its property row is
        // block row 7 of 10, in the spanning segment past the split.
        Assert.Equal(
            "start=chunk0 end=chunk1 splitRows=4 segment2BeginsWithValueRow=True "
            + "conditionalPropertyRow=7 pastSplit=True namePath=productBySlug.name",
            includedGeometry);
        Assert.Equal(includedGeometry, excludedGeometry);
        Assert.Equal("""{"productBySlug":{"id":1,"name":"Abc","d":"Abc","c":"Abc"}}""", includedJson);
        Assert.Equal("""{"productBySlug":{"id":1,"name":"Abc","d":"Abc"}}""", excludedJson);
    }

    [Fact]
    public void CreateObject_Should_MaterializeBlockAcrossThreeChunks_When_SelectionSetOutgrowsEarlyChunks()
    {
        // arrange
        // 200 selections produce a 402-row block. Started in chunk 0, the block outgrows the
        // capacities of the first chunks, so the spanning apply must roll several chunk edges.
        // The conditional selection sits at the block tail, past the last split.
        const int fieldCount = 199;
        var fields = new StringBuilder();

        for (var i = 0; i < fieldCount; i++)
        {
            fields.AppendLine($"f{i}: name");
        }

        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            $$"""
            query($flag: Boolean!) {
                productBySlug(slug: "1") {
                    {{fields}}
                    cond: name @include(if: $flag)
                }
            }
            """);
        var operation = plan.Operation;

        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var productBySlug = document.Data.GetProperty("productBySlug");
        var selectionSet = operation.GetSelectionSet(productBySlug.AssertSelection());

        var payload = """{"name":"Abc"}"""u8.ToArray();
        var source = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            payload,
            payload.Length);

        // act
        var blockStart = document._metaDb.NextCursor;
        productBySlug.SetObjectValue(selectionSet);
        var blockEnd = document._metaDb.NextCursor - 1;

        for (var i = 0; i < fieldCount; i++)
        {
            productBySlug.GetProperty("f" + i).SetLeafValue(source.Root.GetProperty("name"));
        }

        productBySlug.GetProperty("cond").SetLeafValue(source.Root.GetProperty("name"));

        // assert
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = false, SkipValidation = true });
        document.WriteDataTo(writer);

        var expected = new StringBuilder("{\"productBySlug\":{");

        for (var i = 0; i < fieldCount; i++)
        {
            if (i > 0)
            {
                expected.Append(',');
            }

            expected.Append($"\"f{i}\":\"Abc\"");
        }

        expected.Append("}}");

        var conditionalIndex = selectionSet.Selections.Length - 1;
        var conditionalPropertyCursor = blockStart + ((conditionalIndex * 2) + 1);

        Assert.True(
            blockStart.Chunk == 0 && blockEnd.Chunk >= 2,
            $"A block of {(selectionSet.Selections.Length * 2) + 2} rows starting at row index "
            + $"{blockStart.Index} must reach chunk 2: the first two chunks hold only "
            + $"{CompositeResultDocument.Cursor.RowsPerChunkFor(0)} and "
            + $"{CompositeResultDocument.Cursor.RowsPerChunkFor(1)} rows.");
        Assert.True(
            selectionSet.Selections[conditionalIndex].IsConditional
                && conditionalPropertyCursor.Chunk == blockEnd.Chunk,
            "The conditional selection's property row must sit in the block's last chunk "
            + $"but points at {conditionalPropertyCursor} while the block ends at {blockEnd}.");
        Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(buffer.WrittenSpan));
        Assert.Equal(
            "productBySlug.f198",
            productBySlug.GetProperty("f198").Path.ToString());
    }

    private static (string Json, string Geometry) MaterializeSteeredProductResult(
        Operation operation,
        ulong includeFlags,
        int splitRows)
    {
        var document = new CompositeResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            includeFlags);
        var productBySlug = document.Data.GetProperty("productBySlug");
        var selectionSet = operation.GetSelectionSet(productBySlug.AssertSelection());
        ref var metaDb = ref document._metaDb;

        // Steer the frontier so exactly splitRows rows of the object block land in the first
        // chunk; the spanning segment in the next chunk then begins with block row splitRows.
        while (metaDb.NextCursor.Index < CompositeResultDocument.Cursor.RowsPerChunkFor(0) - splitRows)
        {
            metaDb.AppendNull(parentRow: 0);
        }

        var blockStart = metaDb.NextCursor;
        productBySlug.SetObjectValue(selectionSet);
        var blockEnd = metaDb.NextCursor - 1;

        var conditionalPropertyRow = -1;

        for (var i = 0; i < selectionSet.Selections.Length; i++)
        {
            if (selectionSet.Selections[i].IsConditional)
            {
                conditionalPropertyRow = (i * 2) + 1;
            }
        }

        var payload = """{"id":1,"name":"Abc"}"""u8.ToArray();
        var source = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            payload,
            payload.Length);
        productBySlug.GetProperty("id").SetLeafValue(source.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(source.Root.GetProperty("name"));
        productBySlug.GetProperty("c").SetLeafValue(source.Root.GetProperty("name"));
        productBySlug.GetProperty("d").SetLeafValue(source.Root.GetProperty("name"));

        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = false, SkipValidation = true });
        document.WriteDataTo(writer);

        var actualSplitRows =
            CompositeResultDocument.Cursor.RowsPerChunkFor(0) - blockStart.Index;
        var geometry =
            $"start=chunk{blockStart.Chunk} end=chunk{blockEnd.Chunk} "
            + $"splitRows={actualSplitRows} "
            + $"segment2BeginsWithValueRow={actualSplitRows >= 2 && actualSplitRows % 2 == 0} "
            + $"conditionalPropertyRow={conditionalPropertyRow} "
            + $"pastSplit={conditionalPropertyRow >= actualSplitRows} "
            + $"namePath={productBySlug.GetProperty("name").Path}";

        return (Encoding.UTF8.GetString(buffer.WrittenSpan), geometry);
    }

    private static string MaterializeProductResult(
        Operation operation,
        ulong includeFlags,
        ulong deferFlags)
    {
        var document = new CompositeResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            includeFlags,
            deferFlags);

        var productBySlug = document.Data.GetProperty("productBySlug");
        productBySlug.SetObjectValue(operation.GetSelectionSet(productBySlug.AssertSelection()));

        var payload = """{"id":1,"name":"Abc"}"""u8.ToArray();
        var source = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            payload,
            payload.Length);
        productBySlug.GetProperty("id").SetLeafValue(source.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(source.Root.GetProperty("name"));

        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = false, SkipValidation = true });
        document.WriteDataTo(writer);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    [Fact]
    public async Task Write_Document_To_PipeWriter()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), result, result.Length);
        var i = 0;

        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));
        }

        // act
        await using var memoryStream = new MemoryStream();
        var writer = PipeWriter.Create(memoryStream);
        var operationResultData = new OperationResultData(
            compositeResult,
            compositeResult.Data.IsNullOrInvalidated,
            compositeResult,
            compositeResult);
        var operationResult = new OperationResult(
            operationResultData);

        new JsonResultFormatter(indented: true).Format(operationResult, writer);

        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        var json = Encoding.UTF8.GetString(memoryStream.ToArray());
        json.MatchSnapshot();
    }
}
