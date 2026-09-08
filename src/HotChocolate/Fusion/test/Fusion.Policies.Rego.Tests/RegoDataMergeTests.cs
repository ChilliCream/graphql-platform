using System.Text;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoDataMergeTests
{
    [Fact]
    public void Merge_Should_CombineTopLevelKeys_When_DocumentsAreDisjointObjects()
    {
        // arrange
        var documents = new[] { Bytes("""{"far":{"a":1}}"""), Bytes("""{"orders":{"b":2}}""") };

        // act
        var merged = RegoDataMerge.Merge(documents);

        // assert
        Encoding.UTF8.GetString(merged).MatchInlineSnapshot(
            """{"far":{"a":1},"orders":{"b":2}}""");
    }

    [Fact]
    public void Merge_Should_RejectProviderProviderCollision_When_TopLevelKeyIsDuplicated()
    {
        // arrange
        var documents = new[]
        {
            Bytes("""{"far":{}}"""),
            Bytes("""{"orders":{"a":1}}"""),
            Bytes("""{"orders":{"b":2}}""")
        };

        // act
        void Act() => RegoDataMerge.Merge(documents);

        // assert
        var error = Assert.Throws<RegoDataMergeException>(Act);
        Assert.Contains("orders", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_Should_RejectProviderFarCollision_When_TopLevelKeyIsDuplicated()
    {
        // arrange
        var documents = new[] { Bytes("""{"orders":{"a":1}}"""), Bytes("""{"orders":{"b":2}}""") };

        // act
        void Act() => RegoDataMerge.Merge(documents);

        // assert
        var error = Assert.Throws<RegoDataMergeException>(Act);
        Assert.Contains("orders", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_Should_RejectNonObjectRoot_When_DocumentRootIsAnArray()
    {
        // arrange
        var documents = new[] { Bytes("""{"far":{}}"""), Bytes("""[1,2,3]""") };

        // act
        void Act() => RegoDataMerge.Merge(documents);

        // assert
        Assert.Throws<RegoDataMergeException>(Act);
    }

    [Fact]
    public void Merge_Should_RejectNonObjectRoot_When_DocumentRootIsAScalar()
    {
        // arrange
        var documents = new[] { Bytes("""{"far":{}}"""), Bytes("42") };

        // act
        void Act() => RegoDataMerge.Merge(documents);

        // assert
        Assert.Throws<RegoDataMergeException>(Act);
    }

    [Fact]
    public void Merge_Should_RejectInvalidJson_When_DocumentIsMalformed()
    {
        // arrange
        var documents = new[] { Bytes("""{"far":{}}"""), Bytes("{not-json") };

        // act
        void Act() => RegoDataMerge.Merge(documents);

        // assert
        Assert.Throws<RegoDataMergeException>(Act);
    }

    private static ReadOnlyMemory<byte> Bytes(string json) => Encoding.UTF8.GetBytes(json);
}
