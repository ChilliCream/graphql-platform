using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Extensions;

public sealed class HttpResponseExtensionsTests
{
    [Theory]
    [InlineData(new string[0], "Accept", new[] { "Accept" })]
    [InlineData(new[] { "Origin" }, "Accept", new[] { "Origin", "Accept" })]
    [InlineData(new[] { "accept" }, "Accept", new[] { "accept" })]
    [InlineData(new[] { "Accept" }, "x-bar, x-foo", new[] { "Accept", "x-bar, x-foo" })]
    [InlineData(new[] { "Accept" }, "accept, x-foo", new[] { "Accept", "x-foo" })]
    [InlineData(new[] { "Origin, Accept" }, "ACCEPT", new[] { "Origin, Accept" })]
    [InlineData(new[] { "Origin, , Accept" }, "accept", new[] { "Origin, , Accept" })]
    [InlineData(new[] { "*" }, "Accept", new[] { "*" })]
    [InlineData(new[] { "Origin", "*" }, "Accept, x-foo", new[] { "Origin", "*" })]
    public void AppendVary_Should_AddOnlyNamesNotYetListed_When_NamesAreGiven(
        string[] existing,
        string fieldNames,
        string[] expected)
    {
        // arrange
        IHeaderDictionary headers = new HeaderDictionary();

        if (existing.Length > 0)
        {
            headers.Vary = existing;
        }

        // act
        headers.AppendVary(fieldNames);

        // assert
        Assert.Equal(expected, headers.Vary.ToArray());
    }
}
