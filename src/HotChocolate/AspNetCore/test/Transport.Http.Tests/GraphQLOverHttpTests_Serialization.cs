namespace HotChocolate.Transport.Http.Tests;

public class GraphQLOverHttpTests_Serialization
{
    [Fact]
    // The GraphQL specification allows for many serialization formats to be implemented. Servers and clients MUST support JSON and MAY support other, additional serialization formats.
    public void Serialization_JsonSupported()
    {

    }

    [Fact]
    // The following are the officially recognized GraphQL media types to designate using the JSON encoding for GraphQL requests:
    //   application/json
    public void MediaTypesInRequest_ApplicationJson_Supported()
    {

    }

    [Fact]
    // The following are the officially recognized GraphQL media types to designate using the JSON encoding for GraphQL responses:
    //   application/json
    public void MediaTypesInResponse_ApplicationJson_Supported()
    {

    }

    [Fact]
    // The following are the officially recognized GraphQL media types to designate using the JSON encoding for GraphQL responses:
    //   application/graphql-response+json
    public void MediaTypesInResponse_GraphQLResponsePlusJson_Supported()
    {

    }

    [Fact]
    // If the media type in a Content-Type or Accept header includes encoding information, then the encoding MUST be utf-8 (e.g. Content-Type: application/graphql-response+json; charset=utf-8). If encoding information is not included then utf-8 MUST be assumed.
    public void ExplicitEncoding_ContentType_UTF8()
    {

    }

    [Fact]
    // If the media type in a Content-Type or Accept header includes encoding information, then the encoding MUST be utf-8 (e.g. Content-Type: application/graphql-response+json; charset=utf-8). If encoding information is not included then utf-8 MUST be assumed.
    public void ExplicitEncoding_ContentType_Invalid()
    {

    }

    [Fact]
    // If the media type in a Content-Type or Accept header includes encoding information, then the encoding MUST be utf-8 (e.g. Content-Type: application/graphql-response+json; charset=utf-8). If encoding information is not included then utf-8 MUST be assumed.
    public void ExplicitEncoding_Accept_UTF8()
    {

    }

    [Fact]
    // If the media type in a Content-Type or Accept header includes encoding information, then the encoding MUST be utf-8 (e.g. Content-Type: application/graphql-response+json; charset=utf-8). If encoding information is not included then utf-8 MUST be assumed.
    public void ExplicitEncoding_Accept_Invalid()
    {

    }

    [Fact]
    // If the media type in a Content-Type or Accept header includes encoding information, then the encoding MUST be utf-8 (e.g. Content-Type: application/graphql-response+json; charset=utf-8). If encoding information is not included then utf-8 MUST be assumed.
    public void ImplicitEncoding_UTF8()
    {

    }
}
