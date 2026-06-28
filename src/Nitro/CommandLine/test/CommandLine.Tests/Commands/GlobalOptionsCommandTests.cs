namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

public sealed class GlobalOptionsCommandTests(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    [Fact]
    public async Task ExecuteAsync_InvalidOutputValue_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("api", "list", "--output", "wtf");

        // assert
        Assert.Equal(1, result.ExitCode);
        result.StdErr.MatchInlineSnapshot(
            """
            Argument 'wtf' not recognized. Must be one of:
            	'json'
            """);
    }
}
