using System.Security.Cryptography;

namespace HotChocolate.Language;

public class Sha256DigestTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(55)]
    [InlineData(56)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(119)]
    [InlineData(120)]
    [InlineData(128)]
    [InlineData(1000)]
    [InlineData(70000)]
    public void Compute_Should_MatchPlatformDigest_When_SourceHasLength(int length)
    {
        // arrange
        var source = new byte[length];
        new Random(length).NextBytes(source);
        var expected = SHA256.HashData(source);
        var actual = new byte[Sha256Digest.SizeInBytes];

        // act
        Sha256Digest.Compute(source, actual);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compute_Should_LeaveBytesAfterTheDigest_When_DestinationIsLonger()
    {
        // arrange
        var source = "abc"u8.ToArray();
        var destination = new byte[Sha256Digest.SizeInBytes + 4];
        destination.AsSpan(Sha256Digest.SizeInBytes).Fill(0xAB);

        // act
        Sha256Digest.Compute(source, destination);

        // assert
        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
            Convert.ToHexString(destination, 0, Sha256Digest.SizeInBytes));
        Assert.Equal(new byte[] { 0xAB, 0xAB, 0xAB, 0xAB }, destination[Sha256Digest.SizeInBytes..]);
    }
}
