using System.Net;
using System.Text;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SubgraphErrorTests : FusionTestBase
{
    #region Required list items

    private const string ProductsSchema = """
        type Query { product: Product }
        type Product @key(fields: "id") { id: Int! }
        """;

    [Theory]
    [InlineData("AB", ErrorHandlingMode.Propagate, false)]
    [InlineData("BA", ErrorHandlingMode.Propagate, false)]
    [InlineData("AB", ErrorHandlingMode.Null, false)]
    [InlineData("BA", ErrorHandlingMode.Null, false)]
    [InlineData("AB", ErrorHandlingMode.Propagate, true)]
    [InlineData("BA", ErrorHandlingMode.Propagate, true)]
    [InlineData("AB", ErrorHandlingMode.Null, true)]
    [InlineData("BA", ErrorHandlingMode.Null, true)]
    public async Task Execute_Should_PreserveRequiredSharedParent_When_PublicChildIsNullable(
        string arrivalOrder,
        ErrorHandlingMode mode,
        bool list)
    {
        // arrange
        var order = new SourceResultOrder(arrivalOrder);
        var queryType = list ? "type Query { x: [Row!]! @shareable } " : "type Query { x: Row! @shareable } ";
        using var names = CreateOrderedSubgraph("A", queryType + "type Row @shareable { a: Int! c: Int }",
            list
                ? """{"data":null,"errors":[{"message":"failed","path":["x",0,"a"]}]}"""
                : """{"data":null,"errors":[{"message":"failed","path":["x","a"]}]}""",
            order);
        using var prices = CreateOrderedSubgraph("B", queryType + "type Row @shareable { a: Int b: Int }",
            list ? """{"data":{"x":[{"b":3}]}}""" : """{"data":{"x":{"b":3}}}""",
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder.AddDiagnosticEventListener(_ => order));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ x { a c b } }", onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var shape = list ? "List" : "Object";
        await MatchSnapshotAsync(gateway, request, response, postFix: $"{shape}_{arrivalOrder}_OnError_{mode}");
    }

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
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepIndexedError_When_LookupSuppliesSharedList(ErrorHandlingMode mode)
    {
        // arrange
        using var products = CreateSubgraph("A",
            """
            type Query { product: Product localProduct(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! items: [Item!] @shareable }
            type Item @shareable { name: String! }
            """,
            """{"data":{"product":{"id":1,"items":null}},"errors":[{"message":"no name","path":["product","items",0,"name"]}]}""");
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
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_RespectReportOrder_When_SourceReportsParentAndRequiredChildErrors(
        ErrorHandlingMode mode,
        bool parentFirst)
    {
        // arrange
        using var names = CreateSubgraph("A",
            "type Query { x: Row @shareable } type Row @shareable { a: String! }",
            parentFirst
                ? """{"data":{"x":null},"errors":[{"message":"parent","path":["x"]},{"message":"child","path":["x","a"]}]}"""
                : """{"data":{"x":null},"errors":[{"message":"child","path":["x","a"]},{"message":"parent","path":["x"]}]}""");
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
        var order = parentFirst ? "ParentFirst" : "ChildFirst";
        await MatchSnapshotAsync(gateway, request, response, postFix: $"{order}_OnError_{mode}");
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportOneParentError_When_BothSourcesFailAtSamePosition(ErrorHandlingMode mode)
    {
        // arrange
        const string payload = """{"data":{"x":null},"errors":[{"message":"failed","path":["x"]}]}""";
        using var first = CreateSubgraph("A",
            """
            type Query { x: Item @shareable }
            type Item @shareable { name: String }
            """,
            payload);
        using var second = CreateSubgraph("B",
            """
            type Query { x: Item @shareable }
            type Item @shareable { price: Int }
            """,
            payload);
        using var gateway = await CreateCompositeSchemaAsync([("A", first), ("B", second)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ x { name price } }", onError: mode);

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
    public async Task Execute_Should_EnforceSharedChildNullability_When_LookupCompletesMissingItem(
        ErrorHandlingMode mode)
    {
        // arrange
        using var products = CreateSubgraph("A",
            """
            type Query { product: Product localProduct(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! item: Item @shareable }
            type Item @shareable { name: String! }
            """,
            """{"data":{"product":{"id":1,"item":null}}}""");
        using var prices = CreateSubgraph("B",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! item: Item @shareable }
            type Item @shareable { price: Int }
            """,
            """{"data":{"byId":{"item":{"price":1}}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ product { item { name price } } }", onError: mode);

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
    public async Task Execute_Should_ReportViolationsInEachNullableBranch_When_SharedObjectIsNull(
        ErrorHandlingMode mode)
    {
        // arrange
        using var names = CreateSubgraph("A",
            """
            type Query { box: Box @shareable }
            type Box @shareable { left: Item @shareable right: Item @shareable }
            type Item @shareable { name: String! code: Int! }
            """,
            """{"data":{"box":null}}""");
        using var prices = CreateSubgraph("B",
            """
            type Query { box: Box @shareable }
            type Box @shareable { left: Item @shareable right: Item @shareable }
            type Item @shareable { price: Int }
            """,
            """{"data":{"box":{"left":{"price":1},"right":{"price":2}}}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", names), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ box { left { n:name code price } right { n:name price } } }", onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + mode);
    }

    [Theory]
    [InlineData("[Item!]", ErrorHandlingMode.Propagate)]
    [InlineData("[Item]", ErrorHandlingMode.Propagate)]
    [InlineData("[Item!]", ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportViolationsAtSurvivingBranches_When_SharedListIsCompletedFromNull(
        string listType,
        ErrorHandlingMode mode)
    {
        // arrange
        using var names = CreateSubgraph("A",
            $$"""
            type Query { items: {{listType}} @shareable }
            type Item @shareable { name: String! }
            """,
            """{"data":{"items":null}}""");
        using var prices = CreateSubgraph("B",
            $$"""
            type Query { items: {{listType}} @shareable }
            type Item @shareable { price: Int }
            """,
            """{"data":{"items":[{"price":1},{"price":2}]}}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", names), ("B", prices)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ items { name price } }", onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var itemType = listType == "[Item!]" ? "RequiredItems" : "NullableItems";
        await MatchSnapshotAsync(gateway, request, response, postFix: $"{itemType}_OnError_{mode}");
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsAnAlreadyNulledList(ErrorHandlingMode onError)
    {
        // arrange
        var order = new SourceResultOrder("AB");
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { rows: [Row!] @shareable }
            type Row @shareable { name: String! }
            """,
            """{ "data": { "rows": [{ "name": null }] } }""",
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { rows: [Row!] @shareable total: Int }
            type Row @shareable { price: Int }
            """,
            """{ "data": { "rows": [{ "price": 3 }], "total": 4 } }""",
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder.AddDiagnosticEventListener(_ => order));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ rows { name price } total }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData("ABC", ErrorHandlingMode.Propagate)]
    [InlineData("BCA", ErrorHandlingMode.Propagate)]
    [InlineData("CAB", ErrorHandlingMode.Propagate)]
    [InlineData("ABC", ErrorHandlingMode.Null)]
    [InlineData("BCA", ErrorHandlingMode.Null)]
    [InlineData("CAB", ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsAnAlreadyNulledObject(
        string arrivalOrder,
        ErrorHandlingMode onError)
    {
        // arrange
        var order = new SourceResultOrder(arrivalOrder);
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { details: Details @shareable }
            type Details @shareable { name: String! }
            """,
            """
            {
              "data": { "details": null },
              "errors": [{ "message": "Name unavailable.", "path": ["details", "name"] }]
            }
            """,
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { details: Details @shareable }
            type Details @shareable { price: Int }
            """,
            """{ "data": { "details": { "price": 3 } } }""",
            order);
        using var stock = CreateOrderedSubgraph(
            "C",
            """
            type Query { details: Details @shareable total: Int }
            type Details @shareable { available: Boolean }
            """,
            """{ "data": { "details": { "available": true }, "total": 4 } }""",
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices), ("C", stock)],
            configureGatewayBuilder: builder => builder.AddDiagnosticEventListener(_ => order));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ details { name price available } total }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: arrivalOrder + "_OnError_" + onError);
    }

    private TestServer CreateOrderedSubgraph(string name, string schema, string payload, SourceResultOrder order)
        => CreateSourceSchema(name, schema, mockHttpResponse: async _ =>
        {
            await order.WaitForTurnAsync(name);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });

    private sealed class SourceResultOrder : FusionExecutionDiagnosticEventListener
    {
        private readonly Dictionary<string, Step> _steps = [];

        public SourceResultOrder(string order)
        {
            var previous = Task.CompletedTask;
            foreach (var name in order)
            {
                var step = new Step(previous);
                _steps.Add(name.ToString(), step);
                previous = step.Completed.Task;
            }
        }

        public Task WaitForTurnAsync(string name)
            => _steps[name].Previous.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        public override IDisposable ExecuteOperationNode(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName)
            => _steps[schemaName];

        // The next response is released after this node has enqueued its result for merging.
        private sealed class Step(Task previous) : IDisposable
        {
            public Task Previous { get; } = previous;

            public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Dispose() => Completed.TrySetResult();
        }
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    public async Task Execute_Should_ReportItemIndex_When_SubgraphReturnsNullForRequiredObjectItem(
        ErrorHandlingMode onError,
        bool sourceReportsError)
    {
        // arrange
        var payload = sourceReportsError
            ? """
              {
                "data": { "r": [{ "value": 1 }, null], "tail": 4 },
                "errors": [{ "message": "Row unavailable.", "path": ["r", 1] }]
              }
              """
            : """{ "data": { "r": [{ "value": 1 }, null], "tail": 4 } }""";
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { rows: [Row!] tail: Int }
            type Row { value: Int }
            """,
            payload);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ r: rows { value } tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (sourceReportsError ? "WithSourceError_" : "WithoutSourceError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepErrorsThroughTheCause_When_SourceReturnsACollapsedList(ErrorHandlingMode onError)
    {
        // arrange
        using var source = CreateSubgraph(
            "catalog",
            """
            type Query { rows: [Row!] tail: Int }
            type Row { note: Int name: String! }
            """,
            """
            {
              "data": { "rows": null, "tail": null },
              "errors": [
                { "message": "Earlier note.", "path": ["rows", 1, "note"] },
                { "message": "Another earlier note.", "path": ["rows", 0, "note"] },
                { "message": "Required name.", "path": ["rows", 1, "name"] },
                { "message": "Later name.", "path": ["rows", 0, "name"] },
                { "message": "Independent field.", "path": ["tail"] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ rows { note name } tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepEachDescendantError_When_LookupReturnsNull(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var reviews = CreateSubgraph(
            "reviews",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! reviews: [Review] }
            type Review { stars: Int }
            """,
            """
            {
              "data": { "byId": null },
              "errors": [
                { "message": "First review failed.", "path": ["byId", "reviews", 0, "stars"] },
                { "message": "Second review failed.", "path": ["byId", "reviews", 1, "stars"] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("reviews", reviews)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ item: product { id reviews { stars } } }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, "Book", "details", "n")]
    [InlineData(ErrorHandlingMode.Null, "Book", "details", "n")]
    [InlineData(ErrorHandlingMode.Propagate, "Photo", "details", "n")]
    [InlineData(ErrorHandlingMode.Null, "Photo", "details", "n")]
    [InlineData(ErrorHandlingMode.Propagate, "Photo", "fusion__field_1", "other")]
    [InlineData(ErrorHandlingMode.Null, "Photo", "fusion__field_1", "other")]
    public async Task Execute_Should_KeepErrorSuffix_When_LookupFragmentsUseDifferentSourceAliases(
        ErrorHandlingMode onError,
        string typeName,
        string sourceName,
        string leaf)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { asset: Asset }
            interface Asset @key(fields: "id") { id: ID! }
            type Book implements Asset @key(fields: "id") { id: ID! }
            type Photo implements Asset @key(fields: "id") { id: ID! }
            """,
            $$"""
            { "data": { "asset": { "__typename": "{{typeName}}", "id": "1" } } }
            """);
        using var details = CreateSubgraph(
            "details",
            """
            type Query { byId(id: ID!): Asset @lookup }
            interface Asset { id: ID! title: Detail }
            type Book implements Asset { id: ID! title: Detail! }
            type Photo implements Asset { id: ID! title: Detail! @shareable }
            type Detail @shareable { name: String! other: String! }
            """,
            $$"""
            {
              "data": { "byId": null },
              "errors": [{ "message": "Detail unavailable.", "path": ["byId", "{{sourceName}}", "{{leaf}}"] }]
            }
            """);
        using var photos = CreateSubgraph(
            "photos",
            """
            type Query { photoById(id: ID!): Photo @lookup }
            type Photo { id: ID! title: Detail @shareable }
            type Detail @shareable { name: String! other: String! }
            """,
            """{ "data": { "photoById": null } }""");
        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("details", details), ("photos", photos)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              asset {
                __typename
                details: title { n: name }
                ... on Photo { details: title { other } }
              }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: $"{typeName}_{leaf}_OnError_{onError}");
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
    public async Task Execute_Should_KeepCausalItemErrorAndSiblingError_When_SourceReturnsNullList(ErrorHandlingMode mode)
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

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_UsePublicPath_When_LookupKeyReportsAnError(
        ErrorHandlingMode onError,
        bool selectKey)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { product: Product }
            type Product @key(fields: "id") { id: Int! }
            """,
            """
            {
              "data": { "item": { "id": 1 } },
              "errors": [{ "message": "Key error.", "path": ["item", "id"] }]
            }
            """);
        using var details = CreateProductDetailsSubgraph(
            """{ "data": { "lookups": { "byId": { "label": "Mug" } } } }""");
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            selectKey
                ? "{ item: product { id label: description } }"
                : "{ item: product { label: description } }",
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (selectKey ? "SelectedKey_OnError_" : "HiddenKey_OnError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_RespectItemNullability_When_SourceReportsIndexedLeafErrors(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { scores: [Int!] colors: [Color!] tail: Int }
            enum Color { RED }
            """,
            """
            {
              "data": { "scores": [null, null, null], "colors": [null, null], "tail": 7 },
              "errors": [
                { "message": "First score failed.", "path": ["scores", 0] },
                { "message": "Last score failed.", "path": ["scores", 2] },
                { "message": "Last color failed.", "path": ["colors", 1] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ scores colors tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportLookupError_When_LookupReturnsAnObject(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": { "label": "Mug" } } },
              "errors": [{ "message": "Lookup error.", "path": ["lookups", "byId"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product { id label: description }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData("""{"data":null,"errors":[{"message":"wrapper","path":["lookups"]}]}""", "Wrapper")]
    [InlineData("""{"data":{"lookups":{"byId":{"n":"OK"}}},"errors":[{"message":"lookup","path":["lookups","byId"]}]}""", "Lookup")]
    public async Task Execute_Should_RebaseSourceError_When_LookupReportsError(string payload, string postFix)
    {
        // arrange
        using var products = CreateSubgraph("A", ProductsSchema, """{"data":{"p":{"id":1}}}""");
        using var names = CreateSubgraph("B",
            """
            type Query { lookups: Lookups! @internal }
            type Lookups @internal { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! name: String }
            """,
            payload);
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", names)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ p:product { id n:name } }", onError: ErrorHandlingMode.Propagate);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Fact]
    public async Task Execute_Should_ReportEachFieldPath_When_LookupFailsForMultipleNullableFields()
    {
        // arrange
        using var products = CreateSubgraph("A", ProductsSchema, """{"data":{"p":{"id":1}}}""");
        using var names = CreateSubgraph("B",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! name: String spare: String }
            """,
            """{"data":{"byId":null},"errors":[{"message":"lookup failed","path":["byId"]}]}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", names)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ p:product { id n:name spare } }", onError: ErrorHandlingMode.Propagate);

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
    public async Task Execute_Should_ReportUnresolvedFieldPaths_When_RequiredLookupFieldFails(ErrorHandlingMode mode)
    {
        // arrange
        using var products = CreateSubgraph("A", ProductsSchema, """{"data":{"p":{"id":1}}}""");
        using var names = CreateSubgraph("B",
            """
            type Query { byId(id: Int!): Product @lookup }
            type Product @key(fields: "id") { id: Int! name: String! spare: String }
            """,
            """{"data":{"byId":null},"errors":[{"message":"name failed","path":["byId","n"]}]}""");
        using var gateway = await CreateCompositeSchemaAsync([("A", products), ("B", names)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ p:product { id spare n:name } }", onError: mode);

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
    public async Task Execute_Should_KeepSiblingErrors_When_NullableItemCollapses(ErrorHandlingMode mode)
    {
        // arrange
        using var source = CreateSubgraph("A", "type Query { v: [Row] } type Row { a: Int b: Int }",
            """
            {"data":{"v":[null]},"errors":[{"message":"first","path":["v",0,"a"]},{"message":"second","path":["v",0,"b"]}]}
            """);
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ v { a b } }", onError: mode);

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
    public async Task Execute_Should_ReportOneError_When_SourceSuffixesMapToTheSameClientField(ErrorHandlingMode mode)
    {
        // arrange
        using var source = CreateSubgraph("A", "type Query { row: Row } type Row { n: Int }",
            """
            {"data":{"row":null},"errors":[
              {"message":"first","path":["row","extra"]},
              {"message":"second","path":["row","other"]},
              {"message":"selected","path":["row","n"]}]}
            """);
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ row { n } }", onError: mode);

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
    public async Task Execute_Should_RespectReportOrder_When_NulledObjectHasParentAndConditionalChildErrors(
        ErrorHandlingMode mode,
        bool parentFirst)
    {
        // arrange
        using var source = CreateSubgraph("catalog",
            "type Query { row: Row } type Row { value: Int! ignored: Int! }",
            parentFirst
                ? """{"data":{"row":null},"errors":[{"message":"Parent","path":["row"]},{"message":"Child","path":["row","n"]}]}"""
                : """{"data":{"row":null},"errors":[{"message":"Child","path":["row","n"]},{"message":"Parent","path":["row"]}]}""");
        using var gateway = await CreateCompositeSchemaAsync([("catalog", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "query($skip: Boolean! = false) { row { n:value @skip(if: $skip) ignored @include(if: false) } }",
            onError: mode);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var order = parentFirst ? "ParentFirst" : "ChildFirst";
        await MatchSnapshotAsync(gateway, request, response, postFix: $"{order}_OnError_{mode}");
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportItemIndex_When_SubgraphReturnsNullForRequiredScalarItem(
        ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query {
              ratings: [Int!]
              reviewCount: Int!
            }
            """,
            """
            {
              "data": { "scores": [5, null, 3], "reviewCount": 3 }
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              scores: ratings
              reviewCount
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepIndexedSubgraphError_When_RequiredEnumItemIsNull(
        ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query {
              weeklyAvailability: [[Availability!]]
              weekCount: Int!
            }

            enum Availability { IN_STOCK }
            """,
            """
            {
              "data": { "weeks": [["IN_STOCK", null], ["IN_STOCK"]], "weekCount": 2 },
              "errors": [{ "message": "Availability unavailable.", "path": ["weeks", 0, 1] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              weeks: weeklyAvailability
              weekCount
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportEachItemPath_When_RequiredListContainsMultipleNulls(
        ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query {
              product: Product
              productCount: Int!
            }

            type Product {
              ratings: [Int!]!
            }
            """,
            """
            {
              "data": { "product": { "scores": [null, 5, null] }, "productCount": 1 }
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              product { scores: ratings }
              productCount
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    private TestServer CreateSubgraph(string name, string schema, string responseBody)
        => CreateSourceSchema(
            name,
            schema,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                }));

    #endregion

    #region Lookup errors

    [Fact]
    public async Task Execute_Should_ReportFieldError_When_NestedLookupParentFails()
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            { "data": null, "errors": [{ "message": "Lookups unavailable.", "path": ["lookups"] }] }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product {
                id
                label: description
              }
            }
            """);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact]
    public async Task Execute_Should_ReportLookupErrorInDeferredPayload_When_DeferredLookupFails()
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": null } },
              "errors": [{ "message": "Name unavailable.", "path": ["lookups", "byId", "label"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            query($hidePrice: Boolean!) {
              item: product {
                id
                ... @defer(label: "details") {
                  price @skip(if: $hidePrice)
                  label: name
                }
              }
            }
            """,
            onError: ErrorHandlingMode.Null,
            variables: new Dictionary<string, object?> { ["hidePrice"] = true });

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, stableStream: true);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportEachUnresolvedField_When_LookupBubblesToNull(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": null } },
              "errors": [{ "message": "Name unavailable.", "path": ["lookups", "byId", "label"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product {
                id
                price
                label: name
              }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportEachFieldPath_When_LookupFailsForMultipleFields(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": null } },
              "errors": [{ "message": "Product unavailable.", "path": ["lookups", "byId"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product {
                id
                name
                price
              }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_StopAtNonNullFieldInPropagateMode_When_DirectLookupReportsSiblingErrors(
        ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateSubgraph(
            "details",
            """
            type Query { productById(id: Int!): Product @lookup }
            type Product @key(fields: "id") {
              id: Int!
              price: Int
              name: String!
              description: String
            }
            """,
            """
            {
              "data": { "productById": null },
              "errors": [
                { "message": "Description unavailable.", "path": ["productById", "description"] },
                { "message": "Price unavailable.", "path": ["productById", "cost"] },
                { "message": "Name unavailable.", "path": ["productById", "label"] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product {
                id
                cost: price
                label: name
                description
              }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }

    [Fact]
    public async Task Execute_Should_ReportOnlyFieldError_When_LookupReturnsNullSiblingValue()
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": { "price": null, "label": null } } },
              "errors": [{ "message": "Description unavailable.", "path": ["lookups", "byId", "label"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("details", details)],
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ item: product { id price label: description } }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var result = await response.ReadAsResultAsync(TestContext.Current.CancellationToken);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "item": {
                  "id": 1,
                  "price": null,
                  "label": null
                }
              },
              "errors": [
                {
                  "message": "Description unavailable.",
                  "path": [
                    "item",
                    "label"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_KeepErrorPaths_When_MultipleLookupFieldsFail()
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": null } },
              "errors": [
                { "message": "Rating unavailable.", "path": ["lookups", "byId", "reviews", 0, "stars"] },
                { "message": "Name unavailable.", "path": ["lookups", "byId", "label"] }
              ]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product {
                id
                price
                reviews { stars: rating }
                label: name
              }
            }
            """);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }

    private TestServer CreateProductCatalog()
        => CreateSubgraph(
            "catalog",
            """
            type Query { product: Product }
            type Product @key(fields: "id") { id: Int! }
            """,
            """{ "data": { "item": { "id": 1 } } }""");

    private TestServer CreateProductDetailsSubgraph(string responseBody)
        => CreateSubgraph(
            "details",
            """
            type Query { lookups: Lookups! @internal }
            type Lookups @internal { byId(id: Int!): Product @lookup }

            type Product @key(fields: "id") {
              id: Int!
              name: String!
              price: Int
              description: String
              reviews: [Review!]!
            }

            type Review { rating: Int! }
            """,
            responseBody);

    #endregion

    #region Parallel, Shared Entry Field

    [Fact(Skip = "There should only ever be one error associated with a field")]
    public async Task Resolve_Parallel_SharedEntryField_Nullable_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Flaky in snapshot")]
    public async Task Resolve_Parallel_SharedEntryField_NonNull_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is incorrectly placed")]
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String @error
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String! @error
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String! @error
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
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

    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer
    //             }
    //
    //             type Viewer {
    //               name: String
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer!
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Parallel, No Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_SubField_Nullable_EntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isTimingOut: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other!
            }

            type Other {
              userId: ID! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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
    public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other @error
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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
    public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String
            }
            """,
            isTimingOut: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other! @error
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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

    //     [Fact]
    //     public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   other: Other
    //                 }
    //
    //                 type Other {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           name
    //                         }
    //                         other {
    //                           userId
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    //     [Fact]
    //     public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   other: Other!
    //                 }
    //
    //                 type Other {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           name
    //                         }
    //                         other {
    //                           userId
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Entity Resolver

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              name: String @error
              price: Float
            }

            interface Node {
              id: ID!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              name: String! @error
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              name: String! @error
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int! @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int! @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup @error
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_EntryField_Nullable_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_EntryField_NonNull_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String
    //               price: Float
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String!
    //               price: Float!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product!
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String!
    //               price: Float!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product!
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String
    //                   price: Float
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String!
    //                   price: Float!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product!
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String!
    //                   price: Float!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product!
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Resolve Sequence

    [Fact]
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
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

    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_Nullable_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_NonNull_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_NonNull_Parent_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand!
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region ResolveByKey

    //     [Fact]
    //     public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int! @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int! @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }

    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    // [Fact]
    // public async Task ErrorFilter_Is_Applied()
    // {
    //     // arrange
    //     var server = CreateSourceSchema(
    //         "A",
    //         """
    //         type Query {
    //           field: String @error
    //         }
    //         """);
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("A", server)
    //     ],
    //     configure: builder =>
    //         builder.AddErrorFilter(error => error.WithMessage("REPLACED MESSAGE").WithCode("CUSTOM_CODE")));
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //     using var result = await client.PostAsync(
    //         """
    //         query {
    //           field
    //         }
    //         """,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     using var response = await result.ReadAsResultAsync();
    //     response.MatchInlineSnapshot("""
    //                                {
    //                                  "errors": [
    //                                    {
    //                                      "message": "REPLACED MESSAGE",
    //                                      "locations": [
    //                                        {
    //                                          "line": 2,
    //                                          "column": 3
    //                                        }
    //                                      ],
    //                                      "path": [
    //                                        "field"
    //                                      ],
    //                                      "extensions": {
    //                                        "code": "CUSTOM_CODE"
    //                                      }
    //                                    }
    //                                  ],
    //                                  "data": {
    //                                    "field": null
    //                                  }
    //                                }
    //                                """);
    // }
}
