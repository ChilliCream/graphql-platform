using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchResponseSplitterTests : FusionTestBase
{
    private const string Lookup = "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name}}";

    [Fact]
    public void TrySplit_Should_RouteFieldsByName_When_ResponseIsOutOfOrder()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """{"data":{"_1_productById":{"name":"B"},"_0_productById":{"name":"A"}}}""",
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Split, status);
        Assert.Equal("A", ReadName(fixture.Results[0]));
        Assert.Equal("B", ReadName(fixture.Results[1]));
    }

    [Fact]
    public void TrySplit_Should_LeaveItemUndefined_When_ItsFieldIsMissing()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """{"data":{"_1_productById":{"name":"B"}}}""",
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Split, status);
        Assert.Equal(JsonValueKind.Undefined, ReadItem(fixture.Results[0]).ValueKind);
        Assert.Equal("B", ReadName(fixture.Results[1]));
    }

    [Fact]
    public void TrySplit_Should_ReturnInvalid_When_FieldCarriesNoItemPrefix()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """{"data":{"productById":{"name":"A"},"_1_productById":{"name":"B"}}}""",
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Invalid, status);
    }

    [Fact]
    public void TrySplit_Should_ReturnInvalid_When_FieldNamesAnUnknownItem()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """{"data":{"_0_productById":{"name":"A"},"_7_productById":{"name":"B"}}}""",
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Invalid, status);
    }

    [Fact]
    public void TrySplit_Should_ReturnBroadcast_When_ErrorCarriesNoPath()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """{"errors":[{"message":"Unknown variable."}],"data":null}""",
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Broadcast, status);
    }

    [Fact]
    public void TrySplit_Should_RewritePathHead_When_ErrorIsRoutedToItem()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """
            {"errors":[{"message":"Boom.","path":["_1_productById","name"]}],
             "data":{"_0_productById":{"name":"A"},"_1_productById":{"name":null}}}
            """,
            itemCount: 2);

        // act
        var status = AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        Assert.Equal(AliasBatchSplitStatus.Split, status);
        Assert.Null(fixture.Results[0].Errors);
        Assert.Equal("productById.name", ReadRoutedErrorPath(fixture.Results[1]));
    }

    [Fact]
    public void TrySplit_Should_ReportErrorsPerItem_When_OnlyOneItemFails()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """
            {"errors":[{"message":"Boom.","path":["_1_productById","name"]}],
             "data":{"_0_productById":{"name":"A"},"_1_productById":{"name":null}}}
            """,
            itemCount: 2);

        // act
        AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        // The items share one response document, so an item that no error was routed to must not
        // report the errors of its siblings.
        Assert.False(fixture.Results[0].HasErrors);
        Assert.Null(fixture.Results[0].Errors);
        Assert.True(fixture.Results[1].HasErrors);
        Assert.Equal("productById.name", ReadRoutedErrorPath(fixture.Results[1]));
    }

    [Fact]
    public void TrySplit_Should_ExposeTheSameExtensions_When_ResponseIsSplit()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """
            {"data":{"_0_productById":{"name":"A"},"_1_productById":{"name":"B"}},
             "extensions":{"trace":"abc"}}
            """,
            itemCount: 2);

        // act
        AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // assert
        var first = fixture.Results[0].Extensions;
        var second = fixture.Results[1].Extensions;
        Assert.Same(first._parent, second._parent);
        Assert.Equal(first._cursor, second._cursor);
    }

    [Fact]
    public void TrySplit_Should_FreeTheDocument_When_TheLastResultIsDisposed()
    {
        // arrange
        using var fixture = new Fixture(
            Lookup,
            """
            {"data":{"_0_productById":{"name":"A"},"_1_productById":{"name":"B"},
             "_2_productById":{"name":"C"}}}
            """,
            itemCount: 3);
        AliasBatchResponseSplitter.TrySplit(fixture.Document, fixture.Items, fixture.Results);

        // act
        // The results of a batched response are disposed in whatever order the plan consumes
        // them, so the first item goes before the item that is still being read.
        fixture.Results[0].Dispose();
        fixture.Results[2].Dispose();

        // assert
        var item = ReadItem(fixture.Results[1]);
        Assert.Equal("B", item.GetProperty("name").GetString());
        fixture.Results[1].Dispose();
        Assert.Throws<ObjectDisposedException>(() => item.GetProperty("name").GetString());
    }

    private static SourceResultElement ReadItem(SourceSchemaResult result)
        => result.LookupData ?? default;

    private static string? ReadName(SourceSchemaResult result)
        => ReadItem(result).GetProperty("name").GetString();

    private static string? ReadRoutedErrorPath(SourceSchemaResult result)
    {
        var trie = result.Errors!.Trie;
        Assert.True(trie.TryGetValue("productById", out var productTrie));
        Assert.True(productTrie!.TryGetValue("name", out var nameTrie));
        return nameTrie!.Error!.Path!.ToString();
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ChunkedArrayWriter _memory = new();
        private readonly Utf8OperationDocument _operation;
        private readonly AliasBatchItem[] _items;

        public Fixture(string lookup, string response, int itemCount)
        {
            _operation = ParseLookup(lookup);
            _items = new AliasBatchItem[itemCount];

            for (var i = 0; i < itemCount; i++)
            {
                _items[i] = CreateItem(_memory, _operation, $$"""{"__fusion_1_id":"{{i}}"}""");
            }

            var payload = Encoding.UTF8.GetBytes(response);
            Document = SourceResultDocument.Parse(
                CommonTestExtensions.CreateArena(),
                payload,
                payload.Length);
            Results = new SourceSchemaResult[itemCount];
        }

        public SourceResultDocument Document { get; }

        public ReadOnlySpan<AliasBatchItem> Items => _items;

        public SourceSchemaResult[] Results { get; }

        public void Dispose()
        {
            Document.Dispose();
            _operation.Dispose();
            _memory.Dispose();
        }
    }
}
