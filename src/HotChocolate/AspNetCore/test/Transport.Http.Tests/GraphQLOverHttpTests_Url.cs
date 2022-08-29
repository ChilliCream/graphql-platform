namespace HotChocolate.Transport.Http.Tests;

public class GraphQLOverHttpTests
{
    [Fact]
    // A server MUST enable GraphQL requests to one or more GraphQL schemas.
    public void Url_MultipleSchemas_Supported()
    {

    }

    [Fact]
    // Each GraphQL schema a server provides MUST be served via one or more URLs.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

    [Fact]
    // A server MUST NOT require the client to use different URLs for different GraphQL query and mutation requests to the same GraphQL schema.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

    [Fact]
    // The GraphQL schema available via a single URL MAY be different for different clients. For example, alpha testers or authenticated users may have access to a schema with additional fields.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

    [Fact]
    // A server MAY forbid individual requests by a client to any endpoint for any reason, for example to require authentication or payment; when doing so it SHOULD use the relevant 4xx or 5xx status code. This decision SHOULD NOT be based on the contents of a well formed GraphQL request.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

    [Fact]
    // Note: The server should not make authorization decisions based on any part of the GraphQL request; these decisions should be made by the GraphQL schema during GraphQL's ExecuteRequest(), allowing for a partial response to be generated.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

    [Fact]
    // Server URLs which enable GraphQL requests MAY also be used for other purposes, as long as they don't conflict with the server's responsibility to handle GraphQL requests.
    public void Url_MultipleEndPointsPerSchema_Supported()
    {

    }

}
