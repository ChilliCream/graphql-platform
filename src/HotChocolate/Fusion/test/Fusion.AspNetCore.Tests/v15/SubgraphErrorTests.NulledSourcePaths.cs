using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepLookupData_When_SharedUnionMemberFieldFails(ErrorHandlingMode mode)
    {
        // arrange
        using var products = CreateSubgraph("A",
            """
            type Query { product: Product localProduct(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! item: Item @shareable }
            union Item = Book
            type Book @shareable { title: String! }
            """,
            """{"data":{"product":{"id":1,"item":null}},"errors":[{"message":"title failed","path":["product","item","name"]}]}""");
        using var books = CreateSubgraph("B",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! item: Item @shareable }
            union Item = Book
            type Book @shareable { title: String author: String }
            """,
            """{"data":{"byId":{"item":{"__typename":"Book","author":"B"}}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", books)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ product { item { ... on Book { name:title author } } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_KeepIndexedError_When_LookupSuppliesSharedList(ErrorHandlingMode mode, bool explicitNull)
    {
        // arrange
        using var products = CreateSubgraph("A",
            """
            type Query { product: Product localProduct(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! items: [Item!] @shareable }
            type Item @shareable { name: String! }
            """,
            explicitNull
                ? """{"data":{"product":{"id":1,"items":null}},"errors":[{"message":"no name","path":["product","items",0,"name"]}]}"""
                : """{"data":{"product":{"id":1}},"errors":[{"message":"no name","path":["product","items",0,"name"]}]}""");
        using var prices = CreateSubgraph("B",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! items: [Item!] @shareable }
            type Item @shareable { price: Int }
            """,
            """{"data":{"byId":{"items":[{"price":1},{"price":2}]}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ product { items { name price } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: (explicitNull ? "ExplicitNull" : "Omitted") + "_OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepFragmentAlias_When_UnionListElementIsNull(ErrorHandlingMode mode)
    {
        // arrange
        using var source = CreateSubgraph("A",
            """
            type Query { items: [Item] }
            union Item = Book | Photo
            interface Named { title: String! }
            type Book implements Named { title: String! }
            type Photo { id: ID! }
            """,
            """{"data":{"rows":[{"__typename":"Book","name":"Good"},null,{"__typename":"Photo","id":"1"}]},"errors":[{"message":"failed","path":["rows",1,"name"]}]}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ rows:items { __typename ... on Named { name:title } ... on Photo { id } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_UseMatchingFragment_When_MembersShareAnAlias(ErrorHandlingMode mode)
    {
        // arrange
        using var source = CreateSubgraph("A",
            """
            type Query { item: Item }
            union Item = Book | Photo
            type Book { author: Author }
            type Author { name: String }
            type Photo { camera: Camera }
            type Camera { model: String }
            """,
            """{"data":{"item":null},"errors":[{"message":"failed","path":["item","details","model"]}]}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ item { ... on Book { details:author { name } } ... on Photo { details:camera { model } } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_RebaseIndexedFragmentErrors_When_AliasedLookupListItemsFail(
        ErrorHandlingMode mode,
        bool requiredItems)
    {
        // arrange
        using var products = CreateSubgraph("A", "type Query { product: Product } type Product @key(fields: \"id\") { id: Int! }", """{"data":{"p":{"id":1}}}""");
        var listType = requiredItems ? "[Item!]" : "[Item]";
        var items = requiredItems ? "null" : "[null,null]";
        using var names = CreateSubgraph("B",
            $$"""
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! items: {{listType}} }
            union Item = Book | Photo
            type Book { name: String! }
            type Photo { id: ID! }
            """,
            $$$"""
            {"data":{"byId":{"r":{{{items}}}}},"errors":[
              {"message":"first","path":["byId","r",0,"n"]},
              {"message":"second","path":["byId","r",1,"n"]}]}
            """);
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", names)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ p:product { r:items { ... on Book { n:name } } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: (requiredItems ? "RequiredItems" : "NullableItems") + "_OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepFirstReportedDescendant_When_SourceReturnsNullList(ErrorHandlingMode mode)
    {
        // arrange
        using var source = CreateSubgraph("A", "type Query { rows: [Row!] tail: Int } type Row { name: String! }",
            """
            {"data":{"rows":null,"tail":null},"errors":[
              {"message":"name failed","path":["rows",1,"name"]},
              {"message":"row failed","path":["rows",1]},
              {"message":"another row failed","path":["rows",0]},
              {"message":"tail failed","path":["tail"]}]}
            """);
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ rows { name } tail }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Fact]
    public async Task Execute_Should_PreserveRequiredSharedObject_When_OnlyOneSourceBubblesAChildError()
    {
        // arrange
        using var names = CreateSubgraph("A",
            "type Query { x: Row! @shareable } type Row @shareable { a: String! }",
            """{"data":null,"errors":[{"message":"failed","path":["x","a"]}]}""");
        using var prices = CreateSubgraph("B",
            "type Query { x: Row! @shareable tail: Int } type Row @shareable { b: Int }",
            """{"data":{"x":{"b":3},"tail":4}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", names), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ x { a b } tail }",
            onError: ErrorHandlingMode.Null);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_MergeChildren_When_DirectAndFragmentSelectionsShareAResponseName(
        ErrorHandlingMode mode)
    {
        // arrange
        const string types = """
            type Query { x: Node @shareable }
            interface Node { detail: Detail }
            type Row implements Node @shareable { detail: Detail @shareable }
            type Other implements Node @shareable { detail: Detail @shareable }
            """;
        using var names = CreateSubgraph("A", types + "\n" + "type Detail @shareable { a: String c: String! }",
            """{"data":{"x":{"__typename":"Row","d":null}},"errors":[{"message":"failed","path":["x","d","c"]}]}""");
        using var prices = CreateSubgraph("B", types + "\n" + "type Detail @shareable { b: Int }",
            """{"data":{"x":{"__typename":"Row","d":{"b":3}}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", names), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ x { __typename d:detail { a } ... on Row { d:detail { c b } } } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepBothPaths_When_SourceReportsParentAndChildErrors(ErrorHandlingMode mode)
    {
        // arrange
        using var names = CreateSubgraph("A",
            "type Query { x: Row @shareable } type Row @shareable { a: String! }",
            """{"data":{"x":null},"errors":[{"message":"parent","path":["x"]},{"message":"child","path":["x","a"]}]}""");
        using var prices = CreateSubgraph("B",
            "type Query { x: Row @shareable } type Row @shareable { b: Int }",
            """{"data":{"x":{"b":3}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", names), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ x { a b } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_RespectNullableBoundaries_When_SubgraphReportsSiblingErrors(
        ErrorHandlingMode onError,
        bool sourcePropagates)
    {
        // arrange
        var products = sourcePropagates
            ? "null"
            : """
              [
                { "details": { "name": null, "description": null }, "sku": "P0" },
                { "details": { "name": "Mug", "description": "Ceramic mug" }, "sku": null },
                { "details": { "name": "Bowl", "description": "Glass bowl" }, "sku": null }
              ]
              """;
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { products: [Product!] status: String }
            type Product { details: Details sku: String! }
            type Details { name: String! description: String! }
            """,
            $$"""
            {
              "data": { "products": {{products}}, "status": "ok" },
              "errors": [
                { "message": "Name failed.", "path": ["products", 0, "details", "name"] },
                { "message": "Description failed.", "path": ["products", 0, "details", "description"] },
                { "message": "First SKU failed.", "path": ["products", 1, "sku"] },
                { "message": "Second SKU failed.", "path": ["products", 2, "sku"] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              products { details { name description } sku }
              status
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = sourcePropagates ? "OnError_" + onError : "PartialData_OnError_" + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportClientAlias_When_NullListItemHasGeneratedSourceAlias(ErrorHandlingMode mode)
    {
        // arrange
        using var accounts = CreateSubgraph(
            "accounts",
            """
            type Query { accounts: [Account] }
            union Account = User | Admin
            type User { id: String }
            type Admin @shareable { id: String! }
            """,
            """
            {
              "data": { "rows": [null, { "__typename": "Admin", "fusion__field_1": "OK" }] },
              "errors": [
                { "message": "first", "path": ["rows", 0, "fusion__field_1"] },
                { "message": "second", "path": ["rows", 1, "fusion__field_1"] }
              ]
            }
            """);
        using var directory = CreateSubgraph(
            "directory",
            """
            type Query { admin: Admin }
            type Admin @shareable { id: String }
            """,
            """{"data":{}}""");
        using var gateway = await CreateCompositeSchemaAsync([("accounts", accounts), ("directory", directory)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              rows: accounts {
                ... on User { n: id }
                ... on Admin { n: id }
              }
            }
            """,
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }
}
