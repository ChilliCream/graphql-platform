using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class AutomaticMockingTests
{
        [Fact]
    public async Task Object()
    {
        var request = """
                      query {
                        obj {
                          id
                          str
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              obj: Object
            }

            type Object {
              id: ID!
              str: String!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Singular_ById()
    {
        var request = """
                      query {
                        productById(id: "5") {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Singular_ById_Error()
    {
        var request = """
                      query {
                        productById(id: "5") {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productById(id: ID!): Product @error
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Singular_ById_Null()
    {
        var request = """
                      query {
                        productById(id: "5") {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productById(id: ID!): Product @null
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Plural_ById()
    {
        var request = """
                      query {
                        productsById(ids: ["5", "6"]) {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product!]!
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Plural_ById_Error()
    {
        var request = """
                      query {
                        productsById(ids: ["5", "6"]) {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @error
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Plural_ById_Error_At_Index()
    {
        var request = """
                      query {
                        productsById(ids: ["5", "6"]) {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Plural_ById_Null()
    {
        var request = """
                      query {
                        productsById(ids: ["5", "6"]) {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @null
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Plural_ById_Null_At_Index()
    {
        var request = """
                      query {
                        productsById(ids: ["5", "6"]) {
                          id
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @null(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ListOfScalars()
    {
        var request = """
                      query {
                        scalars
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
              scalars: [String!]!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ListOfObjects()
    {
        var request = """
                      query {
                        objs {
                          id
                          str
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
                objs: [Object!]!
            }

            type Object {
              id: ID!
              str: String!
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ListOfObjects_Property_Error_At_Index()
    {
        var request = """
                      query {
                        objs {
                          str
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
                objs: [Object!]!
            }

            type Object {
              str: String @error(atIndex: 1)
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ListOfObjects_Property_Null_At_Index()
    {
        var request = """
                      query {
                        objs {
                          str
                        }
                      }
                      """;

        var result = await ExecuteRequestAgainstSchemaAsync(
            """
            type Query {
                objs: [Object!]!
            }

            type Object {
              str: String @null(atIndex: 1)
            }
            """,
            request);

        MatchMarkdownSnapshot(request, result);
    }

    private static async Task<IExecutionResult> ExecuteRequestAgainstSchemaAsync(
        string schemaText,
        string request)
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(schemaText)
            .AddResolverMocking()
            .AddTestDirectives()
            .BuildRequestExecutorAsync();

        return await executor.ExecuteAsync(request);
    }
}
