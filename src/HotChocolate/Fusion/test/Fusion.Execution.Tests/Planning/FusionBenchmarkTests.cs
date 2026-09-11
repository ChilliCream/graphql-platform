using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

// If one of these tests fails, when fixing, you also need to update the
// FusionBenchmarkBase.cs in Fusion.Execution.Benchmarks.
public class FusionBenchmarkTests : FusionTestBase
{
    [Fact]
    public void Simple_Query_With_Requirements()
    {
        // arrange
        var schema = CreateSchema();

        var doc = CreateSimpleQueryWithRequirementsDocument().ToString();

        // act
        var plan = PlanOperation(schema, doc);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Complex_Query()
    {
        // arrange
        var schema = CreateSchema();

        var doc = CreateComplexDocument().ToString();

        // act
        var plan = PlanOperation(schema, doc);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Conditional_Redundancy_Query()
    {
        // arrange
        var schema = CreateSchema();

        var doc = CreateConditionalRedundancyDocument().ToString();

        // act
        var plan = PlanOperation(schema, doc);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Conditional_Redundancy_Query_Should_HaveExactDownstreamDispatchMatrix()
    {
        // arrange
        var plan = PlanOperation(
            CreateSchema(),
            CreateConditionalRedundancyDocument().ToString());
        var allFalse = new Dictionary<string, bool>
        {
            ["includeExpensive"] = false,
            ["includeReviews"] = false,
            ["includeMetadata"] = false,
            ["includeDetails"] = false
        };
        var reviewsAndDetails = new Dictionary<string, bool>(allFalse)
        {
            ["includeReviews"] = true,
            ["includeDetails"] = true
        };
        var allTrue = allFalse.ToDictionary(entry => entry.Key, _ => true);

        // act
        var dispatches = new[]
        {
            GetConditionEligibleDownstreamOperations(plan, allFalse),
            GetConditionEligibleDownstreamOperations(plan, reviewsAndDetails),
            GetConditionEligibleDownstreamOperations(plan, allTrue)
        };

        // assert
        dispatches.MatchInlineSnapshot(
            """
            [
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_6, users:$:Op_123456789101112_10, reviews:$.searchContent<Product>:Op_123456789101112_5, reviews:$.products.edges.node:Op_123456789101112_7, reviews:$.productById:Op_123456789101112_8",
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_6, users:$.productById.reviews.edges.node.author:Op_123456789101112_9, users:$:Op_123456789101112_10, reviews:$.viewer.reviews.edges.node.author.reviews.nodes:Op_123456789101112_12, users:$.searchContent<Article>.author:Op_123456789101112_4, users:$.viewer.reviews.edges.node.author:Op_123456789101112_11, reviews:$.searchContent<Product>:Op_123456789101112_5, reviews:$.products.edges.node:Op_123456789101112_7, reviews:$.productById:Op_123456789101112_8",
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_6, users:$.productById.reviews.edges.node.author:Op_123456789101112_9, users:$:Op_123456789101112_10, reviews:$.viewer.reviews.edges.node.author.reviews.nodes:Op_123456789101112_12, users:$.searchContent<Article>.author:Op_123456789101112_4, users:$.viewer.reviews.edges.node.author:Op_123456789101112_11, reviews:$.searchContent<Product>:Op_123456789101112_5, reviews:$.products.edges.node:Op_123456789101112_7, reviews:$.productById:Op_123456789101112_8"
            ]
            """);
    }

    [Fact]
    public void Complex_Query_Should_HaveExactDownstreamDispatchMatrix()
    {
        // arrange
        var plan = PlanOperation(CreateSchema(), CreateComplexDocument().ToString());
        var allFalse = new Dictionary<string, bool>
        {
            ["level1"] = false,
            ["level2"] = false,
            ["level3"] = false,
            ["level4"] = false,
            ["level5"] = false,
            ["level6"] = false,
            ["includeExpensive"] = false,
            ["includeReviews"] = false,
            ["includeMetadata"] = false
        };
        var alternating = new Dictionary<string, bool>(allFalse)
        {
            ["level1"] = true,
            ["level3"] = true,
            ["level5"] = true,
            ["includeReviews"] = true
        };
        var allTrue = allFalse.ToDictionary(entry => entry.Key, _ => true);

        // act
        var dispatches = new[]
        {
            GetConditionEligibleDownstreamOperations(plan, allFalse),
            GetConditionEligibleDownstreamOperations(plan, alternating),
            GetConditionEligibleDownstreamOperations(plan, allTrue)
        };

        // assert
        dispatches.MatchInlineSnapshot(
            """
            [
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_10, users:$:Op_123456789101112_25, users:$.searchContent<Article>.author:Op_123456789101112_4, reviews:$.products.edges.node:Op_123456789101112_17",
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_10, users:$:Op_123456789101112_25, users:$.searchContent<Article>.author:Op_123456789101112_4, users:$.viewer.reviews.edges.node.author:Op_123456789101112_26, reviews:$.searchContent<Product>:Op_123456789101112_9, reviews:$.products.edges.node:Op_123456789101112_17, reviews:$.productById:Op_123456789101112_23, reviews:$.searchContent<Article>.author.reviews.nodes:Op_123456789101112_5, reviews:$.searchContent<Article>.author.reviews.nodes:Op_123456789101112_6, users:$.productById.reviews.edges.node.author:Op_123456789101112_24, products:$.searchContent<Article>.author.reviews.nodes.product:Op_123456789101112_7, products:$.searchContent<Article>.author.reviews.nodes.product:Op_123456789101112_8",
              "reviews:$:Op_123456789101112_1, products:$:Op_123456789101112_2, search:$:Op_123456789101112_3, products:$.searchContent<Product>:Op_123456789101112_10, users:$:Op_123456789101112_25, users:$.searchContent<Article>.author:Op_123456789101112_4, users:$.viewer.reviews.edges.node.author:Op_123456789101112_26, reviews:$.searchContent<Product>:Op_123456789101112_9, reviews:$.products.edges.node:Op_123456789101112_17, reviews:$.searchContent<Article>.author.reviews.nodes:Op_123456789101112_5"
            ]
            """);
    }

    private static string GetConditionEligibleDownstreamOperations(
        OperationPlan plan,
        IReadOnlyDictionary<string, bool> variables)
    {
        var operations = new List<string>();
        var eligibility = new Dictionary<IOperationPlanNode, bool>();

        foreach (var node in plan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operation
                    when IsEligible(operation):
                    operations.Add(
                        $"{operation.SchemaName}:{operation.Target}:{operation.Operation.Name}");
                    break;

                case ApolloOperationExecutionNode operation
                    when IsEligible(operation):
                    operations.Add(
                        $"{operation.SchemaName}:{operation.Target}:{operation.Operation.Name}");
                    break;

                case OperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (IsEligible(definition))
                        {
                            operations.Add(
                                $"{definition.SchemaName}:{GetTarget(definition)}:"
                                + definition.SourceText.Name);
                        }
                    }
                    break;

                case ApolloOperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (IsEligible(definition))
                        {
                            operations.Add(
                                $"{definition.SchemaName}:{definition.Target}:"
                                + definition.SourceText.Name);
                        }
                    }
                    break;
            }
        }

        return string.Join(", ", operations);

        bool IsEligible(IOperationPlanNode node)
        {
            if (eligibility.TryGetValue(node, out var eligible))
            {
                return eligible;
            }

            var conditions = node switch
            {
                ExecutionNode executionNode => executionNode.Conditions,
                OperationDefinition definition => definition.Conditions,
                _ => []
            };
            eligible = ConditionsMatch(conditions, variables);

            if (eligible)
            {
                var dependencies = node.Dependencies;
                if (node is BatchOperationDefinition)
                {
                    eligible = dependencies.IsEmpty;
                    foreach (var dependency in dependencies)
                    {
                        if (IsEligible(dependency))
                        {
                            eligible = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var dependency in dependencies)
                    {
                        if (!IsEligible(dependency))
                        {
                            eligible = false;
                            break;
                        }
                    }
                }
            }

            eligibility[node] = eligible;
            return eligible;
        }

        static string GetTarget(OperationDefinition definition)
            => definition switch
            {
                SingleOperationDefinition single => single.Target.ToString(),
                BatchOperationDefinition batch => string.Join(
                    "+",
                    batch.Targets.ToArray().Select(target => target.ToString())),
                _ => throw new InvalidOperationException()
            };
    }

    private static bool ConditionsMatch(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        IReadOnlyDictionary<string, bool> variables)
    {
        foreach (var condition in conditions)
        {
            if (variables[condition.VariableName] != condition.PassingValue)
            {
                return false;
            }
        }

        return true;
    }

    private FusionSchemaDefinition CreateSchema()
    {
        var sourceSchemas = CreateSourceSchemas();

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions
        {
            Merger = new SourceSchemaMergerOptions { EnableGlobalObjectIdentification = true }
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

    private List<SourceSchemaText> CreateSourceSchemas()
    {
        return [
            new SourceSchemaText(
                "products",
                """
                type Query {
                  productById(id: ID!): Product @lookup
                  products(first: Int, after: String, last: Int, before: String): ProductConnection
                }

                type Product {
                  id: ID!
                  name: String!
                  description: String @shareable
                  price: Float!
                  dimension: ProductDimension!
                  estimatedDelivery(postCode: String): Int!
                }

                type ProductDimension {
                  height: Int!
                  width: Int!
                }

                type ProductConnection {
                  pageInfo: PageInfo!
                  edges: [ProductEdge!]
                  nodes: [Product!]
                }

                type ProductEdge {
                  cursor: String!
                  node: Product!
                }

                type PageInfo @shareable {
                  hasNextPage: Boolean!
                  hasPreviousPage: Boolean!
                  startCursor: String
                  endCursor: String
                }
                """),
            new SourceSchemaText(
                "reviews",
                """
                type Query {
                  reviewById(id: ID!): Review @lookup
                  productById(id: ID!): Product @lookup @internal
                  viewer: Viewer @shareable
                }

                type Viewer {
                  reviews(first: Int, after: String, last: Int, before: String): ProductReviewConnection
                }

                type Product {
                  id: ID!
                  averageRating: Int!
                  reviews(first: Int, after: String, last: Int, before: String): ProductReviewConnection
                }

                type Review {
                  id: ID!
                  body: String!
                  stars: Int!
                  author: User
                  product: Product
                }

                type User {
                  id: ID! @shareable
                }

                type ProductReviewConnection {
                  pageInfo: PageInfo!
                  edges: [ProductReviewEdge!]
                  nodes: [Review!]
                }

                type ProductReviewEdge {
                  cursor: String!
                  node: Review!
                }

                type PageInfo @shareable {
                  hasNextPage: Boolean!
                  hasPreviousPage: Boolean!
                  startCursor: String
                  endCursor: String
                }
                """),
            new SourceSchemaText(
                "users",
                """
                type Query {
                  userById(id: ID!): User @lookup
                  viewer: Viewer @shareable
                }

                type Viewer {
                  displayName: String!
                }

                type User {
                  id: ID!
                  displayName: String!
                  reviews(first: Int, after: String, last: Int, before: String): UserReviewConnection
                }

                type UserReviewConnection {
                  pageInfo: PageInfo!
                  edges: [UserReviewEdge!]
                  nodes: [Review!]
                }

                type UserReviewEdge {
                  cursor: String!
                  node: Review!
                }

                type Review {
                  id: ID! @shareable
                }

                type PageInfo @shareable {
                  hasNextPage: Boolean!
                  hasPreviousPage: Boolean!
                  startCursor: String
                  endCursor: String
                }
                """),
            new SourceSchemaText(
                "search",
                """
                type Query {
                  searchContent(query: String!): [SearchResult!]!
                  productById(id: ID!): Product @lookup @internal
                }

                interface SearchResult {
                  id: ID!
                  title: String!
                  description: String
                }

                type Product implements SearchResult {
                  id: ID!
                  title: String!
                  description: String @shareable
                }

                type Article implements SearchResult {
                  id: ID!
                  title: String!
                  description: String
                  content: String!
                  author: User!
                  publishedAt: String!
                  tags: [String!]!
                }

                type User {
                  id: ID! @shareable
                }
                """)
        ];
    }

    private DocumentNode CreateSimpleQueryWithRequirementsDocument()
    {
        return Utf8GraphQLParser.Parse(
            """
            query($productId: ID!) {
              productById(id: $id) {
                name
                reviews {
                  nodes {
                    id
                    body
                    author {
                      displayName
                    }
                  }
                }
              }
            }
            """);
    }

    private DocumentNode CreateComplexDocument()
    {
        return Utf8GraphQLParser.Parse(
            """
            query($level1: Boolean!, $level2: Boolean!, $level3: Boolean!, $level4: Boolean!, $level5: Boolean!, $level6: Boolean!, $includeExpensive: Boolean!, $includeReviews: Boolean!, $includeMetadata: Boolean!) {
                # Deep conditional nesting (6+ levels)
                productById(id: "1") {
                    id
                    name
                    ... @include(if: $level1) {
                        description
                        ... @skip(if: $level2) {
                            price
                            ... @include(if: $level3) {
                                dimension {
                                    height
                                    width
                                }
                                ... @skip(if: $level4) {
                                    # Level 4 nesting
                                    ... @include(if: $level5) {
                                        # Level 5 nesting
                                        ... @skip(if: $level6) {
                                            # Level 6 nesting - extreme depth
                                            reviews(first: 10) {
                                                pageInfo {
                                                    hasNextPage
                                                    ... @include(if: $includeMetadata) {
                                                        hasPreviousPage
                                                        startCursor
                                                        endCursor
                                                    }
                                                }
                                                edges {
                                                    cursor
                                                    node {
                                                        id
                                                        body
                                                        stars
                                                        ... @include(if: $includeReviews) {
                                                            author {
                                                                id
                                                                displayName
                                                                ... @skip(if: $level1) {
                                                                    reviews(first: 5) {
                                                                        nodes {
                                                                            id
                                                                            body
                                                                            ... @include(if: $level2) {
                                                                                stars
                                                                                product {
                                                                                    id
                                                                                    name
                                                                                    ... @skip(if: $level3) {
                                                                                        price
                                                                                        ... @include(if: $level4) {
                                                                                            dimension {
                                                                                                height
                                                                                                width
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                # Many fields with same response name but different conditionals
                products(first: 3) {
                    edges {
                        node {
                            # Multiple 'name' fields with different conditionals
                            name
                            name @include(if: $level1)
                            name @skip(if: $level2)
                            name @include(if: $level3) @skip(if: $level4)
                            name @skip(if: $level5) @include(if: $level6)
                            # Multiple 'description' fields with complex conditionals
                            description
                            description @include(if: $includeExpensive)
                            description @skip(if: $level1) @include(if: $level2)
                            description @include(if: $level3) @skip(if: $level4) @include(if: $level5)
                            # Multiple 'price' fields with nested conditionals
                            price
                            price @include(if: $includeExpensive)
                            price @skip(if: $level1)
                            price @include(if: $level2) @skip(if: $level3)
                            price @skip(if: $level4) @include(if: $level5) @skip(if: $level6)
                            # Complex field merging with fragments
                            ... ProductBasicFields
                            ... @include(if: $level1) {
                                ... ProductBasicFields
                            }
                            ... @skip(if: $level2) {
                                ... ProductBasicFields
                            }
                            ... @include(if: $level3) @skip(if: $level4) {
                                ... ProductBasicFields
                            }
                            # Interface type refinements with deep conditionals
                            ... on Product {
                                ... @include(if: $includeReviews) {
                                    reviews(first: 5) {
                                        nodes {
                                            id
                                            body
                                            stars
                                            ... @skip(if: $level1) {
                                                author {
                                                    id
                                                    displayName
                                                    ... @include(if: $level2) {
                                                        reviews(first: 3) {
                                                            nodes {
                                                                id
                                                                body
                                                                ... @skip(if: $level3) {
                                                                    stars
                                                                    product {
                                                                        id
                                                                        name
                                                                        ... @include(if: $level4) {
                                                                            price
                                                                            ... @skip(if: $level5) {
                                                                                dimension {
                                                                                    height
                                                                                    width
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                # Complex conditional merging scenarios
                searchContent(query: "extreme") {
                    # Interface field with multiple conditionals
                    id
                    id @include(if: $level1)
                    id @skip(if: $level2)
                    title
                    title @include(if: $includeExpensive)
                    title @skip(if: $level3)
                    description
                    description @include(if: $level4)
                    description @skip(if: $level5) @include(if: $level6)
                    # Type refinements with extreme conditional complexity
                    ... on Product {
                        name
                        name @include(if: $level1)
                        name @skip(if: $level2) @include(if: $level3)
                        price
                        price @include(if: $includeExpensive)
                        price @skip(if: $level4) @include(if: $level5) @skip(if: $level6)
                        ... @include(if: $includeReviews) {
                            reviews(first: 3) {
                                nodes {
                                    id
                                    body
                                    stars
                                    ... @skip(if: $level1) {
                                        author {
                                            id
                                            displayName
                                            ... @include(if: $level2) {
                                                reviews(first: 2) {
                                                    nodes {
                                                        id
                                                        body
                                                        ... @skip(if: $level3) {
                                                            stars
                                                            product {
                                                                id
                                                                name
                                                                ... @include(if: $level4) {
                                                                    price
                                                                    ... @skip(if: $level5) {
                                                                        dimension {
                                                                            height
                                                                            width
                                                                        }
                                                                        ... @include(if: $level6) {
                                                                            # Extreme nesting level - move reviews to Product level
                                                                            reviews(first: 1) {
                                                                                nodes {
                                                                                    id
                                                                                    body
                                                                                    stars
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ... on Article {
                        content
                        content @include(if: $includeExpensive)
                        content @skip(if: $level1) @include(if: $level2)
                        author {
                            id
                            displayName
                            ... @include(if: $level3) {
                                reviews(first: 2) {
                                    nodes {
                                        id
                                        body
                                        ... @skip(if: $level4) {
                                            stars
                                            product {
                                                id
                                                name
                                                ... @include(if: $level5) {
                                                    price
                                                    ... @skip(if: $level6) {
                                                        dimension {
                                                            height
                                                            width
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        publishedAt
                        publishedAt @include(if: $includeMetadata)
                        publishedAt @skip(if: $level1) @include(if: $level2) @skip(if: $level3)
                        tags
                        tags @include(if: $level4)
                        tags @skip(if: $level5) @include(if: $level6)
                    }
                }
                # Viewer with extreme conditional complexity
                viewer {
                    displayName
                    displayName @include(if: $level1)
                    displayName @skip(if: $level2) @include(if: $level3)
                    ... @include(if: $includeReviews) {
                        reviews(first: 5) {
                            pageInfo {
                                hasNextPage
                                hasNextPage @include(if: $level4)
                                hasNextPage @skip(if: $level5) @include(if: $level6)
                                hasPreviousPage
                                hasPreviousPage @include(if: $includeMetadata)
                                hasPreviousPage @skip(if: $level1) @include(if: $level2)
                                startCursor
                                startCursor @include(if: $level3)
                                startCursor @skip(if: $level4) @include(if: $level5)
                                endCursor
                                endCursor @include(if: $level6)
                                endCursor @skip(if: $level1) @include(if: $level2) @skip(if: $level3)
                            }
                            edges {
                                cursor
                                cursor @include(if: $level4)
                                cursor @skip(if: $level5) @include(if: $level6)
                                node {
                                    id
                                    id @include(if: $level1)
                                    id @skip(if: $level2) @include(if: $level3)
                                    body
                                    body @include(if: $includeExpensive)
                                    body @skip(if: $level4) @include(if: $level5) @skip(if: $level6)
                                    stars
                                    stars @include(if: $level1)
                                    stars @skip(if: $level2)
                                    ... @include(if: $includeReviews) {
                                        author {
                                            id
                                            displayName
                                            ... @skip(if: $level3) {
                                                reviews(first: 3) {
                                                    nodes {
                                                        id
                                                        body
                                                        stars
                                                        ... @include(if: $level4) {
                                                            product {
                                                                id
                                                                name
                                                                ... @skip(if: $level5) {
                                                                    price
                                                                    ... @include(if: $level6) {
                                                                        dimension {
                                                                            height
                                                                            width
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fragment ProductBasicFields on Product {
                id
                name
                description
                price
                averageRating
            }
            """);
    }

    private DocumentNode CreateConditionalRedundancyDocument()
    {
        return Utf8GraphQLParser.Parse(
            """
            query($includeExpensive: Boolean!, $includeReviews: Boolean!, $includeMetadata: Boolean!, $includeDetails: Boolean!) {
                # Unconditional selections that are also inside conditionals
                productById(id: "1") {
                    # These fields exist unconditionally
                    id
                    name
                    description
                    price
                    averageRating
                    # Same fields inside conditionals - should be deduplicated
                    ... @include(if: $includeExpensive) {
                        id
                        name
                        description
                        price
                        averageRating
                    }
                    # More redundancy with different conditionals
                    ... @include(if: $includeReviews) {
                        id
                        name
                        description
                        price
                        averageRating
                    }
                    # Nested redundancy
                    dimension {
                        height
                        width
                        ... @include(if: $includeDetails) {
                            height
                            width
                        }
                    }
                    # Reviews with redundant selections
                    reviews(first: 5) {
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            ... @include(if: $includeMetadata) {
                                hasNextPage
                                hasPreviousPage
                                startCursor
                                endCursor
                            }
                        }
                        edges {
                            cursor
                            node {
                                id
                                body
                                stars
                                # Same fields in conditional
                                ... @include(if: $includeReviews) {
                                    id
                                    body
                                    stars
                                    author {
                                        id
                                        displayName
                                        # Nested redundancy
                                        ... @include(if: $includeDetails) {
                                            id
                                            displayName
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                # Products with extensive redundancy
                products(first: 3) {
                    edges {
                        node {
                            # Unconditional fields
                            id
                            name
                            description
                            price
                            averageRating
                            # Redundant conditional selections
                            ... @include(if: $includeExpensive) {
                                id
                                name
                                description
                                price
                                averageRating
                                dimension {
                                    height
                                    width
                                }
                            }
                            ... @include(if: $includeReviews) {
                                id
                                name
                                description
                                price
                                averageRating
                                reviews(first: 3) {
                                    nodes {
                                        id
                                        body
                                        stars
                                        # More redundancy
                                        ... @include(if: $includeDetails) {
                                            id
                                            body
                                            stars
                                        }
                                    }
                                }
                            }
                            # Fragment redundancy
                            ... ProductBasicInfo
                            ... @include(if: $includeExpensive) {
                                ... ProductBasicInfo
                            }
                            ... @include(if: $includeReviews) {
                                ... ProductBasicInfo
                            }
                        }
                    }
                }
                # Search content with interface redundancy
                searchContent(query: "redundant") {
                    # Interface fields unconditionally
                    id
                    title
                    description
                    # Same interface fields in conditionals
                    ... @include(if: $includeExpensive) {
                        id
                        title
                        description
                    }
                    # Type-specific redundancy
                    ... on Product {
                        # Unconditional product fields
                        id
                        name
                        price
                        averageRating
                        # Redundant conditional selections
                        ... @include(if: $includeExpensive) {
                            id
                            name
                            price
                            averageRating
                            dimension {
                                height
                                width
                            }
                        }
                        ... @include(if: $includeReviews) {
                            id
                            name
                            price
                            averageRating
                            reviews(first: 2) {
                                nodes {
                                    id
                                    body
                                    stars
                                    # Nested redundancy
                                    ... @include(if: $includeDetails) {
                                        id
                                        body
                                        stars
                                    }
                                }
                            }
                        }
                    }
                    ... on Article {
                        # Unconditional article fields
                        id
                        title
                        description
                        content
                        publishedAt
                        # Redundant conditional selections
                        ... @include(if: $includeExpensive) {
                            id
                            title
                            description
                            content
                            publishedAt
                            tags
                        }
                        ... @include(if: $includeDetails) {
                            id
                            title
                            description
                            content
                            publishedAt
                            author {
                                id
                                displayName
                                # More redundancy
                                ... @include(if: $includeReviews) {
                                    id
                                    displayName
                                }
                            }
                        }
                    }
                }
                # Viewer with extensive redundancy
                viewer {
                    # Unconditional fields
                    displayName
                    # Redundant conditional selections
                    ... @include(if: $includeReviews) {
                        displayName
                        reviews(first: 5) {
                            pageInfo {
                                hasNextPage
                                hasPreviousPage
                                # Redundant pageInfo fields
                                ... @include(if: $includeMetadata) {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                            edges {
                                cursor
                                node {
                                    # Unconditional review fields
                                    id
                                    body
                                    stars
                                    # Redundant conditional selections
                                    ... @include(if: $includeDetails) {
                                        id
                                        body
                                        stars
                                        author {
                                            id
                                            displayName
                                            # Nested redundancy
                                            ... @include(if: $includeReviews) {
                                                id
                                                displayName
                                                reviews(first: 2) {
                                                    nodes {
                                                        id
                                                        body
                                                        stars
                                                        # Deep redundancy
                                                        ... @include(if: $includeExpensive) {
                                                            id
                                                            body
                                                            stars
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fragment ProductBasicInfo on Product {
                id
                name
                description
                price
                averageRating
            }
            """);
    }
}
