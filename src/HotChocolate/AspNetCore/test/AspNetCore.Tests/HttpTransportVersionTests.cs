namespace HotChocolate.AspNetCore;

public sealed class HttpTransportVersionTests
{
    [Fact]
    public void ToString_Should_ReturnSeptember2026_When_VersionIsDraft20260903()
    {
        // arrange
#pragma warning disable CS0618 // The obsolete name is under test.
        const HttpTransportVersion version = HttpTransportVersion.Draft20260903;
#pragma warning restore CS0618

        // act
        var name = version.ToString();

        // assert
        Assert.Equal("September2026", name);
    }
}
