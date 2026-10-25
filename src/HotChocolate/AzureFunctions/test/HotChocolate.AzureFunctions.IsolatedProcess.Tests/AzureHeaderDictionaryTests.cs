using HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests;

public sealed class AzureHeaderDictionaryTests
{
    [Fact]
    public void Indexer_Should_ReplaceResponseDataValues_When_HeaderIsSetAgain()
    {
        // arrange
        using var functionContext = new MockFunctionContext(
            new ServiceCollection().BuildServiceProvider());
        using var responseData = new MockHttpResponseData(functionContext);
        var headers = new AzureHeaderDictionary(new DefaultHttpContext().Response, responseData);
        headers["Vary"] = "Accept";

        // act
        headers["Vary"] = new StringValues(["Accept", "x-foo"]);

        // assert
        Assert.Equal(["Accept", "x-foo"], responseData.Headers.GetValues("Vary"));
    }

    [Fact]
    public void ContentLength_Should_ReplaceResponseDataValue_When_ContentLengthIsSetAgain()
    {
        // arrange
        using var functionContext = new MockFunctionContext(
            new ServiceCollection().BuildServiceProvider());
        using var responseData = new MockHttpResponseData(functionContext);
        var headers = new AzureHeaderDictionary(new DefaultHttpContext().Response, responseData)
        {
            ContentLength = 10
        };

        // act
        headers.ContentLength = 20;

        // assert
        Assert.Equal(["20"], responseData.Headers.GetValues("Content-Length"));
    }
}
