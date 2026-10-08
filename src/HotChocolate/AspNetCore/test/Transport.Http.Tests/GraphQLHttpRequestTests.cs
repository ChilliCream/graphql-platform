namespace HotChocolate.Transport.Http.Tests;

public class GraphQLHttpRequestTests
{
    [Fact]
    public void DefaultAcceptContentTypes_Should_WeighJsonBelowGraphQLResponse()
    {
        // act
        var accept = string.Join(", ", GraphQLHttpRequest.DefaultAcceptContentTypes);

        // assert
        Assert.Equal(
            "application/graphql-response+json, application/json; q=0.9, text/event-stream, "
            + "application/graphql-response+jsonl",
            accept);
    }
}
