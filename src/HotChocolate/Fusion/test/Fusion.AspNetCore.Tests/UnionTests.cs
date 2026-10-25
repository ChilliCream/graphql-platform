using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

// TODO once execution is implemented:
// - Returning null for a union (list item)
// - Returning different combinations inside of list
// TODO
// - spreading interface selection on union field
public class UnionTests : FusionTestBase
{
    #region union { ... }

    [Fact]
    public async Task Union_Field_Should_Error_When_TypeNameIsEscaped()
    {
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              imageUrl: String!
            }

            type Discussion {
              title: String
            }
            """,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"data":{"post":{"__typename":"Ph\u006fto","imageUrl":"image.jpg"}}}""",
                        Encoding.UTF8,
                        "application/json")
                }));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b =>
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ post { __typename ... on Photo { imageUrl } } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "post": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "post"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Union_Field_Should_UseFallback_When_UnionHasMoreThanFourMembers()
    {
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Post1 | Post2 | Post3 | Post4 | Post5

            type Post1 { value: String }
            type Post2 { value: String }
            type Post3 { value: String }
            type Post4 { value: String }
            type Post5 { value: String }
            """,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"data":{"post":{"__typename":"Post5","value":"five"}}}""",
                        Encoding.UTF8,
                        "application/json")
                }));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b =>
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ post { __typename ... on Post5 { value } } }",
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "post": {
                  "__typename": "Post5",
                  "value": "five"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Union_Field_Just_Typename_Selected()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              imageUrl: String!
            }

            type Discussion {
              id: ID!
              title: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              post {
                __typename
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              post {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Selections_Differ_Nested_Lookup_Still_Issued()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              search: [CandidateResult!]! @returns(types: ["CustomResult", "CustomResult", "CustomResult"])
              meterTypeById(id: Int!): MeterType @lookup @internal
              categoryByDbId(dbId: Int!): Category @lookup @internal
            }

            union CandidateResult =
              | NotSubmissableResult
              | ExistingResult
              | ConfigMatchesResult
              | CustomResult

            type ExistingResult {
              asset: Asset
            }

            type ConfigMatchesResult {
              asset: Asset
            }

            type NotSubmissableResult {
              asset: Asset
            }

            type CustomResult {
              asset: Asset
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
            }

            type MeterType @key(fields: "id") {
              id: Int!
              type: String!
            }

            type Category @key(fields: "dbId") {
              dbId: Int!
              allowedMeterTypes: [MeterType!]!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              assetByDbId(dbId: Int!): Asset @lookup @internal
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
              statusMessage: String
              meterType: MeterType
              category: Category
            }

            type MeterType @key(fields: "id") {
              id: Int!
            }

            type Category @key(fields: "dbId") {
              dbId: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // four sibling fragments select the same `asset` field: two identical
        // (ExistingResult, CustomResult), one with the entity fields reordered
        // (ConfigMatchesResult) and one with an extra field (NotSubmissableResult)
        var request = new OperationRequest(
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                    meterType {
                      type
                    }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType {
                      type
                    }
                    statusMessage
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);

                var meterType = response.Data
                    .GetProperty("search")[0]
                    .GetProperty("asset")
                    .GetProperty("meterType");
                Assert.Equal(JsonValueKind.String, meterType.GetProperty("type").ValueKind);

                // guard: no candidate is a ConfigMatchesResult, so its dedicated
                // asset lookup is skipped and the nested lookups must still run
                var rootInteraction = gateway.Interactions["A"].Values
                    .Single(i => Encoding.UTF8.GetString(i.Request!.Body.ToArray()).Contains("search {"));
                using var rootDocument = JsonDocument.Parse(rootInteraction.Results.Single());
                Assert.Equal(
                    ["CustomResult", "CustomResult", "CustomResult"],
                    rootDocument.RootElement
                        .GetProperty("data")
                        .GetProperty("search")
                        .EnumerateArray()
                        .Select(e => e.GetProperty("__typename").GetString()));

                var schemaARequests = gateway.Interactions["A"].Values
                    .Select(i => Encoding.UTF8.GetString(i.Request!.Body.ToArray()))
                    .ToArray();
                Assert.Contains(schemaARequests, r => r.Contains("meterTypeById"));
            });
    }

    [Fact]
    public async Task Union_Field_Concrete_Type_Selections_Differ_Nested_Lookup_Still_Issued_Without_Request_Grouping()
    {
        // arrange
        // without request grouping the merged nested lookups become standalone
        // single-definition batch nodes, exercising the node-level dependency
        // wiring instead of the per-definition batch dispatch
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              search: [CandidateResult!]! @returns(types: ["CustomResult", "CustomResult", "CustomResult"])
              meterTypeById(id: Int!): MeterType @lookup @internal
              categoryByDbId(dbId: Int!): Category @lookup @internal
            }

            union CandidateResult =
              | NotSubmissableResult
              | ExistingResult
              | ConfigMatchesResult
              | CustomResult

            type ExistingResult {
              asset: Asset
            }

            type ConfigMatchesResult {
              asset: Asset
            }

            type NotSubmissableResult {
              asset: Asset
            }

            type CustomResult {
              asset: Asset
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
            }

            type MeterType @key(fields: "id") {
              id: Int!
              type: String!
            }

            type Category @key(fields: "dbId") {
              dbId: Int!
              allowedMeterTypes: [MeterType!]!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              assetByDbId(dbId: Int!): Asset @lookup @internal
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
              statusMessage: String
              meterType: MeterType
              category: Category
            }

            type MeterType @key(fields: "id") {
              id: Int!
            }

            type Category @key(fields: "dbId") {
              dbId: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1),
                ("B", server2)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyPlannerOptions(options => options.EnableRequestGrouping = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // four sibling fragments select the same `asset` field: two identical
        // (ExistingResult, CustomResult), one with the entity fields reordered
        // (ConfigMatchesResult) and one with an extra field (NotSubmissableResult)
        var request = new OperationRequest(
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                    meterType {
                      type
                    }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType {
                      type
                    }
                    statusMessage
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType {
                      type
                    }
                    category {
                      allowedMeterTypes {
                        type
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);

                var meterType = response.Data
                    .GetProperty("search")[0]
                    .GetProperty("asset")
                    .GetProperty("meterType");
                Assert.Equal(JsonValueKind.String, meterType.GetProperty("type").ValueKind);

                // guard: no candidate is a ConfigMatchesResult, so its dedicated
                // asset lookup is skipped and the nested lookups must still run
                var rootInteraction = gateway.Interactions["A"].Values
                    .Single(i => Encoding.UTF8.GetString(i.Request!.Body.ToArray()).Contains("search {"));
                using var rootDocument = JsonDocument.Parse(rootInteraction.Results.Single());
                Assert.Equal(
                    ["CustomResult", "CustomResult", "CustomResult"],
                    rootDocument.RootElement
                        .GetProperty("data")
                        .GetProperty("search")
                        .EnumerateArray()
                        .Select(e => e.GetProperty("__typename").GetString()));

                var schemaARequests = gateway.Interactions["A"].Values
                    .Select(i => Encoding.UTF8.GetString(i.Request!.Body.ToArray()))
                    .ToArray();
                Assert.Contains(schemaARequests, r => r.Contains("meterTypeById"));
            });
    }

    [Fact]
    public async Task Union_Sibling_Lookup_Partially_Skipped_Nested_Lookup_Still_Issued_When_Other_Branch_Fails()
    {
        // arrange
        // the runtime data contains only ExistingResult and OtherResult items, so
        // the ExistingResult2 sibling's asset lookup (the lowest-id member of the
        // sibling lookup batch) is skipped while its sibling succeeds; the doc
        // branch targets a subgraph that fails slowly so its failure is processed
        // after the sibling batch already completed
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              search: [CandidateResult!]! @returns(types: ["ExistingResult", "ExistingResult", "OtherResult"])
              meterTypeById(id: Int!): MeterType @lookup @internal
            }

            union CandidateResult =
              | ExistingResult
              | ExistingResult2
              | OtherResult

            type ExistingResult {
              asset: Asset
            }

            type ExistingResult2 {
              asset: Asset
            }

            type OtherResult {
              doc: Doc
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
            }

            type Doc @key(fields: "id") {
              id: Int!
            }

            type MeterType @key(fields: "id") {
              id: Int!
              type: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              assetByDbId(dbId: Int!): Asset @lookup @internal
            }

            type Asset @key(fields: "dbId") {
              dbId: Int!
              statusMessage: String
              meterType: MeterType
            }

            type MeterType @key(fields: "id") {
              id: Int!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              docById(id: Int!): Doc @lookup @internal
            }

            type Doc @key(fields: "id") {
              id: Int!
              meterType: MeterType
            }

            type MeterType @key(fields: "id") {
              id: Int!
            }
            """,
            mockHttpResponse: async _ =>
            {
                await Task.Delay(300);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // the two asset siblings select differing sub-selections so each gets its
        // own lookup; the nested meterType lookups from both branches merge into
        // one batch that optionally depends on the sibling lookup batch and on the
        // failing doc lookup
        var request = new OperationRequest(
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType {
                      type
                    }
                  }
                }
                ... on ExistingResult2 {
                  asset {
                    meterType {
                      type
                    }
                    statusMessage
                  }
                }
                ... on OtherResult {
                  doc {
                    meterType {
                      type
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);

                // the doc branch failed, so an error on its path is expected, but
                // the surviving asset branch must still resolve its nested meterType
                var meterType = response.Data
                    .GetProperty("search")[0]
                    .GetProperty("asset")
                    .GetProperty("meterType");
                Assert.Equal(JsonValueKind.String, meterType.GetProperty("type").ValueKind);

                var schemaARequests = gateway.Interactions["A"].Values
                    .Select(i => Encoding.UTF8.GetString(i.Request!.Body.ToArray()))
                    .ToArray();
                Assert.Contains(schemaARequests, r => r.Contains("meterTypeById"));
            });
    }

    #endregion

    #region unions { ... }

    [Fact]
    public async Task Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              posts {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region objects { union { ... } }

    [Fact]
    public async Task Object_List_Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region objects { unions { ... } }

    [Fact]
    public async Task Object_List_Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Object_List_Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion
}
