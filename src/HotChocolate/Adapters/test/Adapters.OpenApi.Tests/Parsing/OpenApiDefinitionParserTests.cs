using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Parsing;

public class OpenApiDefinitionParserTests
{
    [Fact]
    public void Document_No_Endpoint_Or_Model_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            type Query {
              users: [User]
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("Document must contain either a single operation or at least one fragment definition.",
            exception.Message);
    }

    [Fact]
    public void Document_Multiple_Endpoints_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: GET, route: "/users") {
              users {
                id
              }
            }
            query GetUser @http(method: GET, route: "/user") {
              user {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("Document must contain either a single operation or at least one fragment definition.",
            exception.Message);
    }

    [Fact]
    public void Endpoint_Empty_Route_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: GET, route: "") {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("'route' argument on @http directive must be a non-empty string.",
            exception.Message);
    }

    [Fact]
    public void Endpoint_Invalid_HttpMethod_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: INVALID, route: "/users") {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("'method' argument on @http directive received an invalid value 'INVALID'.", exception.Message);
    }

    [Fact]
    public void Endpoint_Missing_HttpDirective_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("Operation must be annotated with @http directive.", exception.Message);
    }

    [Fact]
    public void Endpoint_Missing_Method_Argument_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(route: "/users") {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("@http directive must have a 'method' argument.", exception.Message);
    }

    [Fact]
    public void Endpoint_Missing_Route_Argument_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: GET) {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("@http directive must have a 'route' argument.", exception.Message);
    }

    [Fact]
    public void Endpoint_Invalid_Route_Parameter_Syntax_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUser @http(method: GET, route: "/users/{userId:invalid}") {
              userById(id: $userId) {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal(
            "Parameter variable mappings must start with '$', got 'userId:invalid'.",
            exception.Message);
    }

    [Fact]
    public void Endpoint_Invalid_QueryParameters_Type_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: GET, route: "/users", queryParameters: 123) {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal("'queryParameters' argument on @http directive must be a list of strings.",
            exception.Message);
    }

    [Fact]
    public void Endpoint_Invalid_QueryParameters_Item_Type_ThrowsException()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query GetUsers @http(method: GET, route: "/users", queryParameters: [123, "valid"]) {
              users {
                id
              }
            }
            """);

        // act
        var exception = Assert.Throws<OpenApiDefinitionParsingException>(
            () => OpenApiDefinitionParser.Parse(document));

        // assert
        Assert.Equal(
            "'queryParameters' argument on @http directive must contain only string values.",
            exception.Message);
    }
}
