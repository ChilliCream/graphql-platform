using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireCrossProviderTests : FusionTestBase
{
    [Fact]
    public async Task Feed_ByNovice_Requires_Author_YearsOfExperience_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CircularCrossProvider.SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<CircularCrossProvider.SchemaB.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<CircularCrossProvider.SchemaC.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { byNovice } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "feed": [
                  {
                    "byNovice": true
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Feed_NeedsFlag_Requires_Flag_SameEntity_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SameEntityCrossProvider.SchemaA.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SameEntityCrossProvider.SchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { needsFlag } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "feed": [
                  {
                    "needsFlag": true
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Feed_Author_Requires_Comments_AuthorId_List_CrossProvider()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<ListCrossProvider.SchemaC.Query>());

        using var server2 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<ListCrossProvider.SchemaD.Query>());

        using var server3 = CreateSourceSchema(
            "e",
            b => b.AddQueryType<ListCrossProvider.SchemaE.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("c", server1),
            ("d", server2),
            ("e", server3)
        ],
        configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ feed { author { id } } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "feed": [
                  {
                    "author": {
                      "id": "10_11"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Root_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                name
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "entity": {
                  "name": "1:20"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Nested_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                entity {
                  name
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Deeply_Nested_Name_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                entity {
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "entity": {
                  "entity": {
                    "entity": {
                      "name": "3:40"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Name_At_Every_Depth_When_Require_Selects_Self_Reference_From_Other_Schema()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                name
                entity {
                  name
                  entity {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_Reenter_Requiring_Schema_When_Nested_Required_Field_Follows_Remote_First_Level()
    {
        // arrange
        using var schemaA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<NestedReentryRequire.SchemaA.Query>());
        using var schemaB = CreateSourceSchema(
            "b",
            builder => builder.AddQueryType<NestedReentryRequire.SchemaB.Query>());
        using var gateway = await CreateCompositeSchemaAsync([("a", schemaA), ("b", schemaB)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              orders {
                id
                customer {
                  id
                }
                price
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_Reenter_Requiring_Schema_When_Abstract_First_Level_Has_No_Type_Condition()
    {
        // arrange
        using var schemaA = CreateSourceSchema(
            "a",
            builder => builder
                .AddQueryType<AbstractNestedReentryRequire.SchemaA.Query>()
                .AddType<AbstractNestedReentryRequire.SchemaA.IPerson>());
        using var schemaB = CreateSourceSchema(
            "b",
            builder => builder
                .AddQueryType<AbstractNestedReentryRequire.SchemaB.Query>()
                .AddType<AbstractNestedReentryRequire.SchemaB.Customer>());
        using var gateway = await CreateCompositeSchemaAsync([("a", schemaA), ("b", schemaB)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              orders {
                id
                price
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private Task<Gateway> CreateSelfReferenceGatewayAsync(bool includeOperationPlan)
    {
        var schemaA = CreateSourceSchema(
            "a",
            builder => builder.AddQueryType<SelfReferenceRequire.SchemaA.Query>());
        var schemaB = CreateSourceSchema(
            "b",
            builder => builder.AddQueryType<SelfReferenceRequire.SchemaB.Query>());

        return CreateCompositeSchemaAsync(
            [("a", schemaA), ("b", schemaB)],
            includeOperationPlan: includeOperationPlan);
    }

    // Two-hop object require from c crossing b's Post.author into a's Author.
    public static class CircularCrossProvider
    {
        public static class SchemaA
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Author? GetAuthorById([ID] int id)
                    => new Author(id, id == 10 ? 5 : 0);
            }

            [EntityKey("id")]
            public record Post([property: ID] int Id);

            public record Author([property: ID] int Id, int YearsOfExperience);
        }

        public static class SchemaB
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public Author GetAuthor() => new(10);
            }

            [EntityKey("id")]
            public record Author([property: ID] int Id);
        }

        public static class SchemaC
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public bool GetByNovice(
                    [Require("author.yearsOfExperience")] int yearsOfExperience)
                    => yearsOfExperience >= 3;
            }
        }
    }

    // Same-entity scalar require crossing from Post.needsFlag to Post.flag.
    public static class SameEntityCrossProvider
    {
        public static class SchemaA
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public bool GetNeedsFlag([Require("flag")] bool flag) => flag;
            }
        }

        public static class SchemaB
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id, true);
            }

            public record Post([property: ID] int Id, bool Flag);
        }
    }

    // List require from e crossing d's Post.comments into c's Comment.authorId.
    public static class ListCrossProvider
    {
        public static class SchemaC
        {
            public class Query
            {
                public IEnumerable<Post> GetFeed() => [new Post(1)];

                [Lookup]
                [Internal]
                public Comment? GetCommentById([ID] int id)
                    => new Comment(id, id == 100 ? 10 : 11, $"body{id}");
            }

            [EntityKey("id")]
            public record Post([property: ID] int Id);

            public record Comment([property: ID] int Id, [property: ID] int AuthorId, string Body);
        }

        public static class SchemaD
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public IEnumerable<Comment> GetComments(int? limit)
                    => [new Comment(100), new Comment(101)];
            }

            [EntityKey("id")]
            public record Comment([property: ID] int Id);
        }

        public static class SchemaE
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Post? GetPostById([ID] int id) => new Post(id);
            }

            public record Post([property: ID] int Id)
            {
                public Author GetAuthor(
                    [Require("comments[authorId]")][ID] IReadOnlyList<int> commentAuthorIds)
                    => new(string.Join("_", commentAuthorIds));
            }

            public record Author(string Id);
        }
    }

    // Nested require where Order.price in a requires customer.discount; customer is only served
    // by b and discount by a.
    public static class NestedReentryRequire
    {
        // a computes price as id * 100 minus the discount of the order's customer.
        public static class SchemaA
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Order? GetOrderById(int id) => new(id);

                [Lookup]
                [Internal]
                public Customer? GetCustomerById(int id) => new(id);
            }

            public sealed class Order(int id)
            {
                [Shareable]
                public int Id => id;

                public int GetPrice([Require("customer.discount")] int discount) => (id * 100) - discount;
            }

            public sealed class Customer(int id)
            {
                [Shareable]
                public int Id => id;

                public int Discount => id / 10;
            }
        }

        // b serves orders 1, 2 and 3 placed by customers 20, 30 and 20.
        public static class SchemaB
        {
            public class Query
            {
                public IEnumerable<Order> GetOrders() => [new(1, 20), new(2, 30), new(3, 20)];
            }

            public sealed class Order(int id, int customerId)
            {
                [Shareable]
                public int Id => id;

                public Customer Customer => new(customerId);
            }

            public sealed class Customer(int id)
            {
                [Shareable]
                public int Id => id;
            }
        }
    }

    // Nested require where Order.price in a requires customer.discount; customer is an interface
    // only served by b, and discount is only served by the Customer type of a.
    public static class AbstractNestedReentryRequire
    {
        // a computes price as id * 100 minus the discount of the order's customer.
        public static class SchemaA
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Order? GetOrderById(int id) => new(id);

                [Lookup]
                [Internal]
                public Customer? GetCustomerById(int id) => new(id);
            }

            [InterfaceType("Person")]
            public interface IPerson
            {
                int Id { get; }

                int Discount { get; }
            }

            public sealed class Order(int id)
            {
                [Shareable]
                public int Id => id;

                public int GetPrice([Require("customer.discount")] int discount) => (id * 100) - discount;
            }

            public sealed class Customer(int id) : IPerson
            {
                [Shareable]
                public int Id => id;

                public int Discount => id / 10;
            }
        }

        // b serves orders 1, 2 and 3 placed by customers 20, 30 and 20.
        public static class SchemaB
        {
            public class Query
            {
                public IEnumerable<Order> GetOrders() => [new(1, 20), new(2, 30), new(3, 20)];
            }

            [InterfaceType("Person")]
            public interface IPerson
            {
                int Id { get; }
            }

            public sealed class Order(int id, int customerId)
            {
                [Shareable]
                public int Id => id;

                public IPerson Customer => new Customer(customerId);
            }

            public sealed class Customer(int id) : IPerson
            {
                [Shareable]
                public int Id => id;
            }
        }
    }

    // Self-referencing require where Entity.name in a requires entity.age, which only b resolves.
    public static class SelfReferenceRequire
    {
        public static class SchemaA
        {
            public class Query
            {
                [Shareable]
                public Entity GetEntity() => new(1);

                [Lookup]
                [Internal]
                public Entity? GetEntityById([ID] int id) => new(id);
            }

            [EntityKey("id")]
            [Shareable]
            public sealed class Entity(int id)
            {
                [ID]
                public int? Id => id;

                public string GetName([Require("entity.age")] int entityAge) => $"{id}:{entityAge}";
            }
        }

        // b serves the chain 1 -> 2 -> 3 -> 4, where each entity has the age id * 10.
        public static class SchemaB
        {
            public class Query
            {
                [Shareable]
                public Entity? GetEntity() => new(1);
            }

            [Shareable]
            public sealed class Entity(int id)
            {
                [ID]
                public int? Id => id;

                public int Age => id * 10;

                public Entity GetEntity() => new(id + 1);
            }
        }
    }
}
