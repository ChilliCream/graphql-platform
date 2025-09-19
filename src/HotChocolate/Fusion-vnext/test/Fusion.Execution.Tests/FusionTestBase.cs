using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase : IDisposable
{
    private readonly TestServerSession _testServerSession = new();
    private bool _disposed;

    protected static FusionSchemaDefinition CreateCompositeSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition CreateCompositeSchema(
        [StringSyntax("graphql")] string schema)
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(schema);
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    public static FusionSchemaDefinition ComposeShoppingSchema()
    {
        return ComposeSchema(
            """"
            interface Node {
              id: ID!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Query {
              node("ID of the object." id: ID!): Node @lookup
              nodes("The list of node IDs." ids: [ID!]!): [Node]!
              userById(id: ID!): User @lookup
              userByUsername(username: String!): User @lookup
              users(first: Int after: String last: Int before: String): UsersConnection
            }

            type User implements Node {
              id: ID!
              name: String!
              birthdate: String!
              username: String!
            }

            type UsersConnection {
              pageInfo: PageInfo!
              edges: [UsersEdge!]
              nodes: [User!]
            }

            type UsersEdge {
              cursor: String!
              node: User!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type InventoryItem implements Node {
              product: Product!
              id: ID!
              quantity: Int!
            }

            type Mutation {
              restockProduct(input: RestockProductInput!): RestockProductPayload! @cost(weight: "10")
            }

            type Product {
              quantity: Int! @cost(weight: "10")
              id: ID!
              item: InventoryItem
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              inventoryItemById(id: ID!): InventoryItem @lookup
              productByIdAsync(id: ID!): Product @lookup @internal
            }

            type RestockProductPayload {
              product: Product
            }

            input RestockProductInput {
              id: ID!
              quantity: Int!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type CreateOrderPayload {
              order: Order
            }

            type Mutation {
              createOrder(input: CreateOrderInput!): CreateOrderPayload! @cost(weight: "10")
            }

            type Order implements Node {
              user: User!
              id: ID!
              items: [OrderItem!]!
              weight: Int!
            }

            type OrderItem {
              product: Product!
              id: Int!
              quantity: Int!
              price: Float!
              orderId: Int!
              order: Order
            }

            type Product {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              orderById(id: ID!): Order @lookup
              userById(id: ID!): User! @lookup @internal
            }

            type User {
              id: ID!
            }

            input CreateOrderInput {
              userId: ID!
              items: [OrderItemInput!]!
              weight: Int!
            }

            input OrderItemInput {
              productId: ID!
              quantity: Int!
              price: Float!
            }
            """",
            """"
            "The node interface is implemented by entities that have a global unique identifier."
            interface Node {
              id: ID!
            }

            type CreatePaymentPayload {
              payment: Payment
            }

            type Mutation {
              createPayment(input: CreatePaymentInput!): CreatePaymentPayload!
            }

            type Order {
              payments: [Payment!]!
              id: ID!
            }

            type Payment implements Node {
              order: Order!
              id: ID!
              amount: Float!
              status: PaymentStatus!
              createdAt: String!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              paymentById(id: ID!): Payment @lookup
              orderById(id: ID!): Order! @lookup
            }

            input CreatePaymentInput {
              orderId: ID!
            }

            enum PaymentStatus {
              PENDING
              AUTHORIZED
              DECLINED
              REFUNDED
            }
            """",
            """"
            interface Error {
              message: String!
            }

            interface Node {
              id: ID!
            }

            type Mutation {
              uploadProductPicture(input: UploadProductPictureInput!): UploadProductPicturePayload!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Product implements Node {
              dimension: ProductDimension!
              pictureString: String
              id: ID!
              name: String!
              price: Float!
              weight: Int!
              pictureFileName: String
            }

            type ProductDimension {
              length: Float!
              width: Float!
              height: Float!
            }

            type ProductsConnection {
              pageInfo: PageInfo!
              edges: [ProductsEdge!]
              nodes: [Product!]
            }

            type ProductsEdge {
              cursor: String!
              node: Product!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              productById(id: ID!): Product @lookup
              products(first: Int after: String last: Int before: String): ProductsConnection
            }

            type UnknownProductError implements Error {
              productId: Int!
              message: String!
            }

            type UploadProductPicturePayload {
              product: Product
              errors: [UploadProductPictureError!]
            }

            union UploadProductPictureError = UnknownProductError

            input UploadProductPictureInput {
              productId: Int!
              picture: String!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type CreateReviewPayload {
              review: Review
            }

            type Mutation {
              createReview(input: CreateReviewInput!): CreateReviewPayload!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Product {
              reviews(first: Int after: String last: Int before: String): ProductReviewsConnection
              id: ID!
            }

            type ProductReviewsConnection {
              pageInfo: PageInfo!
              edges: [ProductReviewsEdge!]
              nodes: [Review!]
            }

            type ProductReviewsEdge {
              cursor: String!
              node: Review!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              productById(id: ID!): Product! @lookup @internal
              reviewById(id: ID!): Review @lookup
              userById(id: ID!): User @lookup @internal
            }

            type Review implements Node {
              product: Product!
              author: User
              id: ID!
              body: String!
              stars: Int!
              createAt: String!
            }

            type Subscription {
              onCreateReview: Review
            }

            type User {
              reviews(first: Int after: String last: Int before: String): UserReviewsConnection
              id: ID!
              name: String!
            }

            type UserReviewsConnection {
              pageInfo: PageInfo!
              edges: [UserReviewsEdge!]
              nodes: [Review!]
            }

            type UserReviewsEdge {
              cursor: String!
              node: Review!
            }

            input CreateReviewInput {
              body: String!
              stars: Int!
              productId: ID!
              authorId: ID!
            }
            """",
            """"
            type Product {
              deliveryEstimate(
                zip: String!
                dimension: ProductDimensionInput!
                  @require(field:
                    """
                    {
                      weight
                      length: dimension.length
                      width: dimension.width
                      height: dimension.height
                    }
                    """)): Int!
              id: ID!
            }

            type Query {
              productById(id: ID!): Product! @lookup @internal
            }

            input ProductDimensionInput {
              weight: Int!
              length: Float!
              width: Float!
              height: Float!
            }
            """");
    }

    protected static FusionSchemaDefinition ComposeSchema(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var sourceSchemas = CreateSourceSchemaTexts(schemas);

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions
        {
            EnableGlobalObjectIdentification = true
        };
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        var compositeSchemaDoc = result.Value.ToSyntaxNode();
        return FusionSchemaDefinition.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition ComposeSchema(params TestSourceSchema[] schemas)
        => ComposeSchema(schemas.Select(t => t.Schema).ToArray());

    protected static DocumentNode ComposeSchemaDocument(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var sourceSchemas = CreateSourceSchemaTexts(schemas);

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions { EnableGlobalObjectIdentification = false };
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }

    public TestServer CreateSourceSchema(
        string schemaName,
        Action<IRequestExecutorBuilder> configureBuilder,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null)
    {
        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL(schemaName: schemaName));
            };

        return _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();
                var builder = services.AddGraphQLServer(schemaName);
                configureBuilder(builder);
                configureServices?.Invoke(services);
            },
            configureApplication);
    }

    protected static OperationPlan PlanOperation(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());

        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);
        const string id = "123456789101112";
        return planner.CreatePlan(id, id, id, operation);
    }

    protected static void MatchInline(
        OperationPlan plan,
        [StringSyntax("yaml")] string expected)
    {
        var formatter = new YamlOperationPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }

    protected static void MatchSnapshot(
        OperationPlan plan)
    {
        var formatter = new YamlOperationPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchSnapshot(extension: ".yaml");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testServerSession.Dispose();
        }
    }

    private static List<SourceSchemaText> CreateSourceSchemaTexts(IEnumerable<string> schemas)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var autoName = 'a';

        foreach (var schema in schemas)
        {
            string name;
            string sourceText;

            var lines = schema.Split(["\r\n", "\n"], StringSplitOptions.None);

            if (lines.Length > 0 && lines[0].StartsWith("# name:"))
            {
                name = lines[0]["# name:".Length..].Trim();
                sourceText = string.Join(Environment.NewLine, lines.Skip(1));
            }
            else
            {
                name = autoName.ToString();
                autoName++;
                sourceText = schema;
            }

            sourceSchemas.Add(new SourceSchemaText(name, sourceText));
        }

        return sourceSchemas;
    }

    protected record TestSourceSchema([StringSyntax("graphql")] string Schema);
}
