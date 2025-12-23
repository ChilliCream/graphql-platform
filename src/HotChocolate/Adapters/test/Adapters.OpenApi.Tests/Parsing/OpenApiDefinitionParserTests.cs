using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Parsing;

public class OpenApiDefinitionParserTests
{
    [Fact]
    public void Document_No_Endpoint_Or_Model_RaisesError()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            type Query {
              users: [User]
            }
            """);

        // act
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("Document must contain either a single operation or at least one fragment definition.",
            error.Message);
    }

    [Fact]
    public void Document_Multiple_Endpoints_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("Document must contain either a single operation or at least one fragment definition.",
            error.Message);
    }

    [Fact]
    public void Endpoint_Empty_Route_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("'route' argument on @http directive must be a non-empty string.",
            error.Message);
    }

    [Fact]
    public void Endpoint_Invalid_HttpMethod_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("'method' argument on @http directive received an invalid value 'INVALID'.", error.Message);
    }

    [Fact]
    public void Endpoint_Missing_HttpDirective_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("Operation must be annotated with @http directive.", error.Message);
    }

    [Fact]
    public void Endpoint_Missing_Method_Argument_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("@http directive must have a 'method' argument.", error.Message);
    }

    [Fact]
    public void Endpoint_Missing_Route_Argument_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("@http directive must have a 'route' argument.", error.Message);
    }

    [Fact]
    public void Endpoint_Invalid_Route_Parameter_Syntax_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Parameter variable mappings must start with '$', got 'userId:invalid'.",
            error.Message);
    }

    [Fact]
    public void Endpoint_Invalid_QueryParameters_Type_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal("'queryParameters' argument on @http directive must be a list of strings.",
            error.Message);
    }

    [Fact]
    public void Endpoint_Invalid_QueryParameters_Item_Type_RaisesError()
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
        var result = OpenApiDefinitionParser.Parse(document);

        // assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "'queryParameters' argument on @http directive must contain only string values.",
            error.Message);
    }
}
