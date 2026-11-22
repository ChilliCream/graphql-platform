using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Text.Json;

public class MinimizedRawJsonFormatterTests : FusionTestBase
{
    private const string AllValueKindsOperation =
        """
        {
            item {
                id
                name
                description
                count
                price
                active
                tags
                scores
                child {
                    id
                    name
                    active
                    price
                    children {
                        id
                        name
                    }
                }
                children {
                    id
                }
            }
        }
        """;

    private const string AllValueKindsData =
        """
        {"item":{"id":"1","name":"Table","description":"A description that is longer than thirty-two bytes","count":3,"price":12.5,"active":true,"tags":["a","b",null],"scores":[],"child":{"id":"2","name":null,"active":false,"children":[{"id":"3","name":""},{"id":"4","name":"x"}]},"children":null}}
        """;

    private const string AllValueKindsResult =
        """
        {"item":{"id":"1","name":"Table","description":"A description that is longer than thirty-two bytes","count":3,"price":12.5,"active":true,"tags":["a","b",null],"scores":[],"child":{"id":"2","name":null,"active":false,"price":null,"children":[{"id":"3","name":""},{"id":"4","name":"x"}]},"children":null}}
        """;

    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(
        """
        type Query {
          item: Item
          entities: [Entity]
        }

        interface Entity {
          id: ID!
        }

        type Item implements Entity {
          id: ID!
          name: String
          description: String
          count: Int
          price: Float
          active: Boolean
          tags: [String]
          scores: [Int]
          child: Item
          children: [Item]
        }

        type Tag implements Entity {
          id: ID!
          label: String
        }
        """);

    [Fact]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_ObjectsHoldValuesOfEveryKind()
    {
        // arrange
        var operation = Compile(AllValueKindsOperation);
        using var document = Materialize(operation, AllValueKindsData);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(AllValueKindsResult, Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Theory]
    [InlineData(
        2,
        """
        {"item":{"id":"i0","name":"n1","count":10,"price":2.1,"active":true,"child":{"id":"c1","name":"d0"},"children":[{"id":"e1","name":"f0"},{"id":"g1","name":"h0"}]}}
        """)]
    [InlineData(
        3,
        """
        {"item":{"id":"i0","name":"n1","count":12,"price":2.0,"active":true,"child":{"id":"c2","name":"d0"},"children":[{"id":"e1","name":"f2"},{"id":"g0","name":"h1"}]}}
        """)]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_LeavesComeFromDifferentSources(
        int sourceCount,
        string expected)
    {
        // arrange
        // every source holds its own index in each value, and the leaves use the sources in turn
        var operation = Compile(
            """
            {
                item {
                    id
                    name
                    count
                    price
                    active
                    child {
                        id
                        name
                    }
                    children {
                        id
                        name
                    }
                }
            }
            """);
        var data = new string[sourceCount];

        for (var i = 0; i < sourceCount; i++)
        {
            data[i] =
                $$$"""
                {"item":{"id":"i{{{i}}}","name":"n{{{i}}}","count":1{{{i}}},"price":2.{{{i}}},"active":true,"child":{"id":"c{{{i}}}","name":"d{{{i}}}"},"children":[{"id":"e{{{i}}}","name":"f{{{i}}}"},{"id":"g{{{i}}}","name":"h{{{i}}}"}]}}
                """;
        }

        using var document = Materialize(operation, data);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(expected, Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_NameTokensAreAtTheBlockSizeLimit()
    {
        // arrange
        // a name of 28 bytes is the longest one whose token fits a block
        var operation = Compile(
            """
            {
                item {
                    n: name
                    aliasWithTwentyEightBytes000: name
                    aliasWithTwentyNineBytes00000: name
                    aliasThatIsMuchLongerThanTheBlockSizeOfThirtyTwoBytes: name
                    id
                }
            }
            """);
        using var document = Materialize(
            operation,
            """
            {"item":{"n":"Table","aliasWithTwentyEightBytes000":"Table","aliasWithTwentyNineBytes00000":"Table","aliasThatIsMuchLongerThanTheBlockSizeOfThirtyTwoBytes":"Table","id":"1"}}
            """);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """
            {"item":{"n":"Table","aliasWithTwentyEightBytes000":"Table","aliasWithTwentyNineBytes00000":"Table","aliasThatIsMuchLongerThanTheBlockSizeOfThirtyTwoBytes":"Table","id":"1"}}
            """,
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_SkipInternalProperties_When_TheyAreFirstLastOrAllPropertiesOfAnObject()
    {
        // arrange
        var operation = Compile(
            """
            {
                item {
                    id @fusion__requirement
                    name
                    count @fusion__requirement
                    active
                    child {
                        id @fusion__requirement
                        name @fusion__requirement
                    }
                    children {
                        id
                        name @fusion__requirement
                    }
                    price @fusion__requirement
                }
            }
            """);
        using var document = Materialize(
            operation,
            """
            {"item":{"id":"1","name":"Table","count":3,"active":true,"child":{"id":"2","name":"a"},"children":[{"id":"3","name":"b"},{"id":"4","name":"c"}],"price":1.5}}
            """);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """{"item":{"name":"Table","active":true,"child":{},"children":[{"id":"3"},{"id":"4"}]}}""",
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Theory]
    [InlineData(0ul, """{"item":{"name":"Table","child":{}}}""")]
    [InlineData(1ul, """{"item":{"name":"Table","child":{"id":"2"},"id":"1","active":true}}""")]
    [InlineData(2ul, """{"item":{"name":"Table","child":{"name":"a"},"count":3}}""")]
    [InlineData(3ul, """{"item":{"name":"Table","child":{"id":"2","name":"a"},"id":"1","active":true,"count":3}}""")]
    public void Write_Should_SkipExcludedProperties_When_SelectionsAreConditional(
        ulong includeFlags,
        string expected)
    {
        // arrange
        // the first flag stands for $a and the second one for $b
        var operation = Compile(
            """
            query($a: Boolean!, $b: Boolean!) {
                item {
                    id @include(if: $a)
                    name
                    count @include(if: $b)
                    child {
                        id @include(if: $a)
                        name @include(if: $b)
                    }
                    active @include(if: $a)
                }
            }
            """);
        using var document = Materialize(
            operation,
            """{"item":{"id":"1","name":"Table","count":3,"child":{"id":"2","name":"a"},"active":true}}""",
            includeFlags);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(expected, Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Theory]
    [InlineData(0ul, """{"item":{"id":"1","active":true,"name":"Table","count":3}}""")]
    [InlineData(ulong.MaxValue, """{"item":{"id":"1","active":true}}""")]
    public void Write_Should_SkipExcludedProperties_When_FragmentIsDeferred(ulong deferFlags, string expected)
    {
        // arrange
        var operation = Compile(
            """
            {
                item {
                    id
                    ... @defer {
                        name
                        count
                    }
                    active
                }
            }
            """);
        using var document = Materialize(
            operation,
            """{"item":{"id":"1","name":"Table","count":3,"active":true}}""",
            deferFlags: deferFlags);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(expected, Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_ListElementsHaveDifferentTypes()
    {
        // arrange
        // __typename is written from the selection set, every other leaf from the source data
        var operation = Compile(
            """
            {
                entities {
                    __typename
                    id
                    ... on Item {
                        name
                        count
                    }
                    ... on Tag {
                        label
                    }
                }
            }
            """);
        using var document = Materialize(
            operation,
            """
            {"entities":[{"__typename":"Item","id":"1","name":"a","count":1},{"__typename":"Tag","id":"2","label":"b"},null,{"__typename":"Item","id":"3","name":null,"count":2}]}
            """);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """
            {"entities":[{"__typename":"Item","id":"1","name":"a","count":1},{"__typename":"Tag","id":"2","label":"b"},null,{"__typename":"Item","id":"3","name":null,"count":2}]}
            """,
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_RowsAndSourceDataSpanChunks()
    {
        // arrange
        // 400 objects and source segments of 1000 bytes make rows and values cross chunks
        var operation = Compile(
            """
            {
                item {
                    children {
                        id
                        name
                        count
                        active
                    }
                }
            }
            """);

        // ids and counts come from the first source, names and booleans from the second one
        var first = new StringBuilder("""{"item":{"children":[""");
        var second = new StringBuilder("""{"item":{"children":[""");
        var result = new StringBuilder("""{"item":{"children":[""");

        for (var i = 0; i < 400; i++)
        {
            if (i > 0)
            {
                first.Append(',');
                second.Append(',');
                result.Append(',');
            }

            var name = new string('n', i % 70);
            var active = i % 2 == 0 ? "true" : "false";
            first.Append($$"""{"id":"a{{i}}","name":"a{{name}}","count":1{{i}},"active":{{active}}}""");
            second.Append($$"""{"id":"b{{i}}","name":"b{{name}}","count":2{{i}},"active":{{active}}}""");
            result.Append($$"""{"id":"a{{i}}","name":"b{{name}}","count":1{{i}},"active":{{active}}}""");
        }

        var expected = result.Append("]}}").ToString();
        using var document = Materialize(
            operation,
            [first.Append("]}}").ToString(), second.Append("]}}").ToString()],
            sourceSegmentSize: 1000);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(expected, Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_WriteSameBytesAsPropertyRowPath_When_NestingReachesTheDepthLimit()
    {
        // arrange
        // the data object, the item and 62 children are the 64 objects the writer allows
        const int children = 62;
        var operation = Compile(CreateNestedOperation(children));
        using var document = MaterializeNested(operation, children);
        var expected = new StringBuilder("""{"item":""");

        for (var i = 0; i < children; i++)
        {
            expected.Append("""{"id":"1","child":""");
        }

        expected.Append("""{"id":"1","child":null}""").Append('}', children + 1);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);

        // assert
        Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
    }

    [Theory]
    [InlineData(WriteMode.NameTables)]
    [InlineData(WriteMode.PropertyRows)]
    public void Write_Should_Throw_When_NestingExceedsTheDepthLimit(WriteMode mode)
    {
        // arrange
        const int children = 63;
        var operation = Compile(CreateNestedOperation(children));
        using var document = MaterializeNested(operation, children);

        // act
        void Act() => Write(document, mode);

        // assert
        Assert.Equal(
            "The JSON depth is exceeds the max allowed depth.",
            Assert.Throws<InvalidOperationException>(Act).Message);
    }

    [Fact]
    public void Write_Should_ReadPropertyRows_When_RowsDifferFromTheObjectTemplate()
    {
        // arrange
        // property rows: name, id, count marked as excluded, and active as the template has it
        var operation = Compile(
            """
            {
                item {
                    id
                    name
                    count
                    active
                }
            }
            """);
        using var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");
        var selectionSet = operation.GetSelectionSet(item.AssertSelection());
        var selections = selectionSet.Selections;
        ref var metaDb = ref document._metaDb;

        var start = metaDb.AppendStartObject(
            item.Cursor.Value,
            selectionSet.Id,
            selections.Length,
            CompositeResultDocument.ElementFlags.None);

        int[] order = [1, 0, 2, 3];

        foreach (var index in order)
        {
            metaDb.AppendEmptyPropertyWithNullValue(
                start.Value,
                selections[index].Id,
                index == 2
                    ? CompositeResultDocument.ElementFlags.IsExcluded
                    : CompositeResultDocument.ElementFlags.IsNullable);
        }

        metaDb.AppendEndObject();
        metaDb.Replace(
            item.Cursor,
            ElementTokenType.Reference,
            location: start.Value,
            parentRow: (item.Cursor - 1).Value);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """{"item":{"name":null,"id":null,"active":null}}""",
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_ReadPropertyRows_When_RowCountDiffersFromTheObjectTemplate()
    {
        // arrange
        // three property rows name the only selection of the selection set
        var operation = Compile(
            """
            {
                item {
                    id
                }
            }
            """);
        using var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");
        var selectionSet = operation.GetSelectionSet(item.AssertSelection());
        ref var metaDb = ref document._metaDb;

        var start = metaDb.AppendStartObject(
            item.Cursor.Value,
            selectionSet.Id,
            propertyCount: 3,
            CompositeResultDocument.ElementFlags.None);

        for (var i = 0; i < 3; i++)
        {
            metaDb.AppendEmptyPropertyWithNullValue(
                start.Value,
                selectionSet.Selections[0].Id,
                CompositeResultDocument.ElementFlags.None);
        }

        metaDb.AppendEndObject();
        metaDb.Replace(
            item.Cursor,
            ElementTokenType.Reference,
            location: start.Value,
            parentRow: (item.Cursor - 1).Value);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """{"item":{"id":null,"id":null,"id":null}}""",
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    [Fact]
    public void Write_Should_AddQuotes_When_SourceStringDoesNotStartWithAQuote()
    {
        // arrange
        // the value row of name becomes a string row that holds the location of a number
        var operation = Compile(
            """
            {
                item {
                    id
                    name
                    count
                }
            }
            """);
        using var document = Materialize(
            operation,
            """
            {"item":{"id":"1","name":12345,"count":7,"padding":"keeps the values away from the end of the data"}}
            """);
        var name = document.Data.GetProperty("item").GetProperty("name");
        var row = document._metaDb.Get(name.Cursor);
        document._metaDb.Replace(
            name.Cursor,
            ElementTokenType.String,
            location: row.Location,
            sizeOrLength: row.SizeOrLength,
            sourceDocumentId: row.SourceDocumentId,
            parentRow: row.Parent,
            flags: row.Flags);

        // act
        var nameTables = Write(document, WriteMode.NameTables);
        var propertyRows = Write(document, WriteMode.PropertyRows);
        var reference = Write(document, WriteMode.Reference);

        // assert
        Assert.Equal(
            """{"item":{"id":"1","name":"12345","count":7}}""",
            Encoding.UTF8.GetString(nameTables));
        Assert.Equal(propertyRows, nameTables);
        Assert.Equal(reference, nameTables);
    }

    private static Operation Compile(string operationText)
    {
        var document = Utf8GraphQLParser.Parse(operationText);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var compiler = new OperationCompiler(
            s_schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));

        return compiler.Compile("1", "1", "1", operationDefinition);
    }

    private static string CreateNestedOperation(int children)
    {
        var operation = new StringBuilder("{ item { id ");

        for (var i = 0; i < children; i++)
        {
            operation.Append("child { id ");
        }

        return operation.Append("child { id } ").Append('}', children + 2).ToString();
    }

    private static CompositeResultDocument MaterializeNested(Operation operation, int children)
    {
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var payload = """{"id":"1","padding":"a value that keeps the id away from the end of the data"}"""u8.ToArray();
        var source = SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), payload, payload.Length);
        var current = document.Data.GetProperty("item");

        for (var i = 0; i <= children; i++)
        {
            current.SetObjectValue(operation.GetSelectionSet(current.AssertSelection()));
            current.GetProperty("id").SetLeafValue(source.Root.GetProperty("id"));
            current = current.GetProperty("child");
        }

        current.SetNullValue();
        return document;
    }

    private static CompositeResultDocument Materialize(
        Operation operation,
        string data,
        ulong includeFlags = 0,
        ulong deferFlags = 0)
        => Materialize(operation, [data], includeFlags, deferFlags);

    /// <summary>
    /// Builds the result document of <paramref name="operation"/>. Every item of
    /// <paramref name="data"/> is the JSON data object of one source document, and the leaf
    /// values are taken from the source documents in turn. A selection that the first source
    /// document has no value for stays unset.
    /// </summary>
    private static CompositeResultDocument Materialize(
        Operation operation,
        string[] data,
        ulong includeFlags = 0,
        ulong deferFlags = 0,
        int sourceSegmentSize = 0)
    {
        var document = new CompositeResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            includeFlags,
            deferFlags);
        var sources = new SourceResultElement[data.Length];

        for (var i = 0; i < data.Length; i++)
        {
            var payload = Encoding.UTF8.GetBytes(data[i]);
            sources[i] = sourceSegmentSize == 0
                ? SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), payload, payload.Length).Root
                : SourceResultDocument.Parse(CommonTestExtensions.CreateArena(), Split(payload, sourceSegmentSize)).Root;
        }

        var leafCount = 0;
        FillObject(operation, document.Data, sources, ref leafCount);
        return document;
    }

    private static void FillObject(
        Operation operation,
        CompositeResultElement target,
        SourceResultElement[] sources,
        ref int leafCount)
    {
        foreach (var property in target.EnumerateObject())
        {
            var selection = property.AssertSelection();

            if (selection.ResponseName != "__typename"
                && sources[0].TryGetProperty(selection.ResponseName, out _))
            {
                var values = Array.ConvertAll(sources, source => source.GetProperty(selection.ResponseName));
                FillValue(operation, property.Value, selection, values, ref leafCount);
            }
        }
    }

    private static void FillValue(
        Operation operation,
        CompositeResultElement target,
        Selection selection,
        SourceResultElement[] values,
        ref int leafCount)
    {
        switch (values[0].ValueKind)
        {
            case JsonValueKind.Null:
                target.SetNullValue();
                break;

            case JsonValueKind.Object:
                var typeName = values[0].TryGetProperty("__typename", out var typeNameValue)
                    ? typeNameValue.GetString()!
                    : selection.NamedType.Name;
                var type = s_schema.Types.GetType<FusionObjectTypeDefinition>(typeName);
                target.SetObjectValue(operation.GetSelectionSet(selection, type));
                FillObject(operation, target, values, ref leafCount);
                break;

            case JsonValueKind.Array:
                target.SetArrayValue(values[0].GetArrayLength());
                var index = 0;

                foreach (var element in target.EnumerateArray())
                {
                    var elementIndex = index++;
                    var elementValues = Array.ConvertAll(values, value => value[elementIndex]);
                    FillValue(operation, element, selection, elementValues, ref leafCount);
                }

                break;

            default:
                target.SetLeafValue(values[leafCount++ % values.Length]);
                break;
        }
    }

    private static ReadOnlySequence<byte> Split(byte[] data, int segmentSize)
    {
        Segment? first = null;
        Segment? last = null;

        for (var offset = 0; offset < data.Length; offset += segmentSize)
        {
            var memory = data.AsMemory(offset, Math.Min(segmentSize, data.Length - offset));
            last = last is null ? first = new Segment(memory, 0) : last.Append(memory);
        }

        return new ReadOnlySequence<byte>(first!, 0, last!, last!.Memory.Length);
    }

    private static byte[] Write(CompositeResultDocument document, WriteMode mode)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, new JsonWriterOptions { Indented = false, SkipValidation = true });

        switch (mode)
        {
            case WriteMode.NameTables:
                new CompositeResultDocument.MinimizedRawJsonFormatter(document, useNameTables: true).Write(writer);
                break;

            case WriteMode.PropertyRows:
                new CompositeResultDocument.MinimizedRawJsonFormatter(document, useNameTables: false).Write(writer);
                break;

            default:
                new CompositeResultDocument.RawJsonFormatter(document, writer).Write();
                break;
        }

        return buffer.WrittenSpan.ToArray();
    }

    public enum WriteMode
    {
        NameTables,
        PropertyRows,
        Reference
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }
}
