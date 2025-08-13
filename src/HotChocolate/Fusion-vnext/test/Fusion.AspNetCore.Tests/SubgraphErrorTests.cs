using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SubgraphErrorTests : FusionTestBase
{
    #region Root

    [Fact]
    public async Task Error_On_Root_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema3.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Error_On_Root_Leaf()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                  name
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

     [Fact]
    public async Task No_Data_And_Error_With_Path_For_Root_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema4.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task No_Data_And_Error_Without_Path_For_Root_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema5.Query>()
                .InsertUseRequest(
                    before: "OperationExecutionMiddleware",
                    middleware: (_, _) =>
                    {
                        return context =>
                        {
                            context.Result = OperationResultBuilder.CreateError(
                                ErrorBuilder.New()
                                    .SetMessage("A global error")
                                    .Build());

                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "error"));

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_Request_Fails_For_Root_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>(),
            isOffline: true);

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    #endregion

    #region Lookup

    [Fact]
    public async Task Error_On_Lookup_Leaf()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Error_On_Lookup_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task No_Data_And_Error_With_Path_For_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema4.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task No_Data_And_Error_With_Path_For_Lookup_Leaf()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema6.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task No_Data_And_Error_Without_Path_For_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema5.Query>()
                .InsertUseRequest(
                    before: "OperationExecutionMiddleware",
                    middleware: (_, _) =>
                    {
                        return context =>
                        {
                            context.Result = OperationResultBuilder.CreateError(
                                ErrorBuilder.New()
                                    .SetMessage("A global error")
                                    .Build());

                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "error"));

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_Request_Fails_For_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>(),
            isOffline: true);

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_Request_Fails_For_Lookup_On_List()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>(),
            isOffline: true);

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    #endregion

    public static class SourceSchema1
    {
        public class Query
        {
            public Product GetTopProduct() => new(1, 13.99);

            public List<Product> GetTopProducts()
                => [new(1, 13.99), new(2, 13.99), new(3, 13.99)];

            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new(id, 13.99);
        }

        public record Product(int Id, double Price);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => new(id);
        }

        public record Product(int Id)
        {
            public string? GetName(IResolverContext context)
            {
                throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product.name")
                    .SetPath(context.Path).Build());
            }
        }
    }

    public static class SourceSchema3
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id, IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product")
                    .SetPath(context.Path).Build());
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema4
    {
        public class Query
        {
            [Lookup]
            public Product GetProductById(int id, IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product")
                    .SetPath(context.Path).Build());
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema5
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => null;
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema6
    {
        public class Query
        {
            [Lookup]
            public Product GetProductById(int id) => new(id);
        }

        public record Product(int Id)
        {
            public string GetName(IResolverContext context)
            {
                throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product.name")
                    .SetPath(context.Path).Build());
            }
        }
    }
}
