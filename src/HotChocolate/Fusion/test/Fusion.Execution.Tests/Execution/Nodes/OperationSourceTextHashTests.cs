using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationSourceTextHashTests
{
    private const string SourceText = "query Op_1 { books { title } }";

    [Fact]
    public void Compute_Should_ReturnEveryRendering_When_TheSourceTextIsHashed()
    {
        // act
        var hash = OperationSourceTextHash.Compute(Encoding.UTF8.GetBytes(SourceText));

        // assert
        hash.ToString().MatchInlineSnapshot(
            "OperationSourceTextHash { "
            + "Sha256 = f26ee48d4a5fd424914ac3c3d2256b4480a1d09f64afe1b27f3a7eaa980c966c, "
            + "Sha256Short = f26ee48d, "
            + "Xxx = 11223086556446842746 }");
    }

    [Fact]
    public void Compute_Should_ReturnThePrefixOfTheSha256_When_TheShortHashIsRead()
    {
        // act
        var hash = OperationSourceTextHash.Compute(Encoding.UTF8.GetBytes(SourceText));

        // assert
        Assert.StartsWith(hash.Sha256Short, hash.Sha256, StringComparison.Ordinal);
    }

    [Fact]
    public void From_Should_TakeTheStatedValues_When_TheHashIsRebuilt()
    {
        // arrange
        // The stated XxHash64 does not match the source text, so a hash that recomputed it
        // would report a different value.
        var computed = OperationSourceTextHash.Compute(Encoding.UTF8.GetBytes(SourceText));

        // act
        var hash = OperationSourceTextHash.From(computed.Sha256, computed.Xxx + 1);

        // assert
        Assert.Equal(new OperationSourceTextHash(computed.Sha256, computed.Sha256Short, computed.Xxx + 1), hash);
    }
}
