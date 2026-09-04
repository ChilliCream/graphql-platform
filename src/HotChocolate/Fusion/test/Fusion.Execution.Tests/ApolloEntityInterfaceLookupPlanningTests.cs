using System.Text;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public sealed class ApolloEntityInterfaceLookupPlanningTests : FusionTestBase
{
    private const string CorruptedNodeSchemaA =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.3"
            import: ["@key", "@shareable", "@external"])

        type Query {
          node(id: ID!): Node @shareable
        }

        interface Node {
          id: ID!
        }

        type Account implements Node @key(fields: "id") {
          id: ID!
          username: String!
        }

        type Chat implements Node @key(fields: "id") {
          id: ID! @external
          account: Account!
        }
        """;

    private const string CorruptedNodeSchemaB =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.3"
            import: ["@key", "@shareable", "@external"])

        type Query {
          node(id: ID!): Node @shareable
        }

        interface Node {
          id: ID!
        }

        type Account implements Node @key(fields: "id") {
          id: ID! @external
          chats: [Chat!]!
        }

        type Chat implements Node @key(fields: "id") {
          id: ID!
          text: String!
        }
        """;

    private const string RequiredNodeSchemaA =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.3"
            import: ["@key", "@shareable", "@external", "@requires"])

        type Query {
          node(id: ID!): Node @shareable
        }

        interface Node {
          id: ID!
        }

        type Account implements Node @key(fields: "id") {
          id: ID! @external
          displayName: String! @requires(fields: "id")
        }
        """;

    private const string RequiredNodeSchemaB =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.3"
            import: ["@key", "@shareable"])

        type Query {
          node(id: ID!): Node @shareable
        }

        interface Node {
          id: ID!
        }

        type Account implements Node @key(fields: "id") {
          id: ID!
        }
        """;

    private const string LocallyRequiredNodeSchemaA =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
          accountById(id: ID! @is(field: "id")): Account @lookup @internal
        }

        interface Node {
          id: ID!
        }

        type Account implements Node {
          id: ID! @shareable
          displayName(id: ID! @require(field: "id")): String!
        }
        """;

    private const string LocallyRequiredNodeSchemaB =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
        }

        interface Node {
          id: ID!
        }

        type Account implements Node {
          id: ID! @shareable
        }
        """;

    // An Apollo Federation subgraph that owns the root 'product' field and the entity interface
    // 'Media'. The entity interface exercises the @key-on-interface path through composition
    // (its key fields must not be stamped @shareable). Product exposes only 'id' here.
    private const string Catalog =
        """
        schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
          query: Query
        }

        type Query {
          product: Product
          media: [Media!]!
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        interface Media @key(fields: "id") {
          id: ID!
          title: String!
        }

        type Article implements Media @key(fields: "id") {
          id: ID!
          title: String!
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String!
        }

        type _Service { sdl: String! }

        union _Entity = Product | Article

        scalar FieldSet
        scalar _Any

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        """;

    // A second Apollo subgraph that resolves 'Product.reviews'. Because 'reviews' lives only here,
    // reaching it from a Product produced by 'catalog' forces an entity lookup into this subgraph,
    // which must be issued as an Apollo '_entities(representations:)' query rather than a synthetic
    // 'productById' root field (which the subgraph does not expose).
    private const string Reviews =
        """
        schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
          query: Query
        }

        type Query {
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        type Product @key(fields: "id") {
          id: ID!
          reviews: [Review!]!
        }

        type Review { body: String! }

        type _Service { sdl: String! }

        union _Entity = Product

        scalar FieldSet
        scalar _Any

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        """;

    // Apollo Federation subgraphs reproducing issue #10312: 'X.b' is declared @external in
    // 'sourceu' (it is only referenced there by @key(fields: "b")) and is resolvable solely in
    // 'sourcev', so the planner must never select 'b' from 'sourceu'. The only correct plan is the
    // chain arrival ('c') -> sourceu ('a' by key 'c') -> sourcev ('b' by key 'a') -> target
    // ('result' by key 'b').
    private const string ExternalKeyFieldArrival =
        """
        # name: arrival
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.9"
            import: ["@key"])

        type Query {
          x: X!
        }

        type X {
          c: ID!
        }
        """;

    private const string ExternalKeyFieldSourceU =
        """
        # name: sourceu
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.9"
            import: ["@key", "@external"])

        type X @key(fields: "b") @key(fields: "c") {
          b: ID! @external
          c: ID! @external
          a: ID!
        }
        """;

    private const string ExternalKeyFieldSourceV =
        """
        # name: sourcev
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.9"
            import: ["@key", "@external"])

        type X @key(fields: "a") {
          a: ID! @external
          b: ID!
        }
        """;

    private const string ExternalKeyFieldTarget =
        """
        # name: target
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.9"
            import: ["@key", "@external"])

        type X @key(fields: "b") {
          b: ID! @external
          result: String!
        }
        """;

    [Fact]
    public void Plan_Should_FetchKeyFieldFromResolvingSchema_When_KeyFieldIsSourceExternal()
    {
        // arrange
        var schema = ComposeSchema(
            ExternalKeyFieldArrival,
            ExternalKeyFieldSourceU,
            ExternalKeyFieldSourceV,
            ExternalKeyFieldTarget);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              x {
                result
              }
            }
            """);

        var yaml = new YamlOperationPlanFormatter().Format(plan);

        // assert
        // 'X.b' is @external in 'sourceu' and only resolvable in 'sourcev', so any operation that
        // selects 'b' must be directed at 'sourcev'.
        var offendingSchemas = GetSourceOperations(plan)
            .Where(node => node.SchemaName != "sourcev" && SelectsField(node.OperationText, "b"))
            .Select(node => node.SchemaName)
            .ToArray();

        if (offendingSchemas.Length > 0)
        {
            Assert.Fail(
                "'X.b' can only be resolved by 'sourcev', but the plan selects 'b' from "
                + $"'{string.Join("', '", offendingSchemas)}':\n{yaml}");
        }

        Assert.Contains(
            GetSourceOperations(plan),
            node => node.SchemaName == "sourcev" && SelectsField(node.OperationText, "b"));
    }

    [Fact]
    public void Plan_Should_Route_Apollo_Entity_Lookup_Through_Entities_Query()
    {
        // arrange
        // Composition succeeds only because the @key on the 'Media' interface no longer stamps its
        // key fields @shareable.
        var schema = ComposeSchema(Catalog, Reviews);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                reviews { body }
              }
            }
            """);

        var yaml = new YamlOperationPlanFormatter().Format(plan);

        // assert
        // The lookup into the reviews subgraph is issued as an Apollo '_entities' query.
        Assert.Contains("_entities", yaml);
    }

    [Fact]
    public void Plan_Should_Fail_When_AbstractFieldIsExternalForSourceLocalRuntimeType()
    {
        // arrange
        var schema = CreateCorruptedNodeSchema();
        var chat = schema.Types.GetType<FusionObjectTypeDefinition>("Chat");
        var account = schema.Types.GetType<FusionObjectTypeDefinition>("Account");

        Assert.False(chat.Fields["id"].Sources["a"].IsExternal);
        Assert.True(chat.Fields["id"].Sources["a"].IsSourceExternal);
        Assert.False(account.Fields["id"].Sources["b"].IsExternal);
        Assert.True(account.Fields["id"].Sources["b"].IsSourceExternal);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  node(id: "a1") {
                    id
                  }
                }
                """));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_FailWithoutCycling_When_SharedFieldHasNoCommonSource()
    {
        // arrange
        var schema = CreateCorruptedNodeSchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  account: node(id: "a1") {
                    id
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                }
                """,
                new OperationPlannerOptions
                {
                    MaxExpandedNodes = 100
                }));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_FailWithoutCycling_When_RootFieldOrderIsReversed()
    {
        // arrange
        var schema = CreateCorruptedNodeSchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                  account: node(id: "a1") {
                    id
                    ... on Chat {
                      id
                    }
                  }
                }
                """,
                new OperationPlannerOptions
                {
                    MaxExpandedNodes = 100
                }));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_UseViableSource_When_ConcreteSelectionHasLocalOwner()
    {
        // arrange
        var schema = CreateCorruptedNodeSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              node(id: "a1") {
                ... on Account {
                  id
                  username
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Complete_When_MismatchedConcreteSelectionsHaveExternalKeys()
    {
        // arrange
        var schema = CreateCorruptedNodeSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              account: node(id: "a1") {
                __typename
                ... on Chat {
                  id
                }
              }
              chat: node(id: "c1") {
                __typename
                ... on Account {
                  id
                }
              }
            }
            """);

        // assert
        Assert.Equal(
            ["a", "b"],
            plan.AllNodes
                .OfType<OperationExecutionNode>()
                .Where(node => node.SchemaName is not null)
                .Select(node => node.SchemaName!));
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseSafeSource_When_LocalFieldRequiresSourceExternalField()
    {
        // arrange
        var schema = CreateNodeSchema(
            new SourceSchemaText("a", RequiredNodeSchemaA),
            new SourceSchemaText("b", RequiredNodeSchemaB));
        var account = schema.Types.GetType<FusionObjectTypeDefinition>("Account");

        Assert.True(account.Fields["id"].Sources["a"].IsSourceExternal);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              node(id: "a1") {
                ... on Account {
                  displayName
                }
              }
            }
            """);

        // assert
        Assert.Equal(
            ["b", "a"],
            plan.AllNodes
                .Select(node => node switch
                {
                    OperationExecutionNode operation => operation.SchemaName,
                    ApolloOperationExecutionNode operation => operation.SchemaName,
                    _ => null
                })
                .OfType<string>());
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseSource_When_FieldRequirementIsLocallyResolvable()
    {
        // arrange
        var schema = CreateNodeSchema(
            new SourceSchemaText("a", LocallyRequiredNodeSchemaA),
            new SourceSchemaText("b", LocallyRequiredNodeSchemaB));
        var account = schema.Types.GetType<FusionObjectTypeDefinition>("Account");

        Assert.False(account.Fields["id"].Sources["a"].IsSourceExternal);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              node(id: "a1") {
                ... on Account {
                  displayName
                }
              }
            }
            """);

        // assert
        Assert.Equal(
            ["b", "a"],
            plan.AllNodes
                .OfType<OperationExecutionNode>()
                .Where(node => node.SchemaName is not null)
                .Select(node => node.SchemaName!));
    }

    private static IEnumerable<(string SchemaName, string OperationText)> GetSourceOperations(
        OperationPlan plan)
    {
        foreach (var node in plan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode { SchemaName: { } schemaName } operation:
                    yield return (schemaName, Encoding.UTF8.GetString(operation.Operation.Value.Span));
                    break;

                case ApolloOperationExecutionNode operation:
                    yield return (operation.SchemaName, Encoding.UTF8.GetString(operation.Operation.Value.Span));
                    break;
            }
        }
    }

    private static bool SelectsField(string operationText, string fieldName)
    {
        var document = Utf8GraphQLParser.Parse(operationText);

        return document.Definitions
            .OfType<OperationDefinitionNode>()
            .Any(operation => SelectsField(operation.SelectionSet, fieldName));
    }

    private static bool SelectsField(SelectionSetNode selectionSet, string fieldName)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    if (field.Name.Value == fieldName)
                    {
                        return true;
                    }

                    if (field.SelectionSet is not null && SelectsField(field.SelectionSet, fieldName))
                    {
                        return true;
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    if (SelectsField(inlineFragment.SelectionSet, fieldName))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static FusionSchemaDefinition CreateCorruptedNodeSchema()
        => CreateNodeSchema(
            new SourceSchemaText("a", CorruptedNodeSchemaA),
            new SourceSchemaText("b", CorruptedNodeSchemaB));

    private static FusionSchemaDefinition CreateNodeSchema(params SourceSchemaText[] sources)
    {
        var options = new SchemaComposerOptions();
        options.Merger.EnableGlobalObjectIdentification = true;
        options.Merger.NodeResolution = NodeResolution.SourceSchema;

        foreach (var source in sources)
        {
            options.SourceSchemas[source.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        var result = new SchemaComposer(sources, options, new CompositionLog()).Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        var features = new FeatureCollection();
        var fusionOptions = new FusionOptions();
        features.Set(fusionOptions);
        features.Set<IFusionSchemaOptions>(fusionOptions);
        return FusionSchemaDefinition.Create(
            result.Value.ToSyntaxNode(),
            features: features);
    }
}
