using HotChocolate.OpenApi.Helpers;

namespace HotChocolate.OpenApi.Tests;

public sealed class GraphQLNamingHelperTests
{
    [Theory]
    [InlineData("Test operation ID!", "TestOperationID")]
    [InlineData("123test", "_123test")]
    [InlineData("Get /user", "GetUser")]
    [InlineData("Get /user/{id}", "GetUserId")]
    public void CreateName_Default_ReturnsExpectedName(string input, string result)
    {
        Assert.Equal(result, GraphQLNamingHelper.CreateName(input));
    }
}
