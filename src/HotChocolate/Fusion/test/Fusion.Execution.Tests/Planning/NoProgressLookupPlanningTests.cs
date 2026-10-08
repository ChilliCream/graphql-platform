using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class NoProgressLookupPlanningTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_NotExpandLookupsIntoSchemaThatOnlyEchoesKey_When_RequirementFieldIsExternal()
    {
        // arrange
        var schema = CreateExternalKeySchema();

        // act
        var plan = PlanOperation(
            schema,
            "{ root { f24 } }",
            new OperationPlannerOptions { MaxExpandedNodes = 1_000 });

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_NotDequeueKeyOnlySchema_When_RequirementFieldIsExternal()
    {
        // arrange
        using var listener = new DequeueListener();
        var schema = CreateExternalKeySchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var planner = new OperationPlanner(
            schema,
            new OperationCompiler(schema, pool),
            new OperationPlannerOptions { MaxExpandedNodes = 1_000 });
        var operation = Utf8GraphQLParser.Parse("{ root { f24 } }")
            .Definitions.OfType<OperationDefinitionNode>().First();
        const string operationId = "no_progress_lookup_dequeues";

        // act
        planner.CreatePlan(
            operationId,
            operationId,
            "12345678",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        listener.Dequeues(operationId).MatchInlineSnapshot(
            """
            [
              "OperationRoot:a",
              "FieldRequirementLookup:a",
              "FieldRequirementLookup:a",
              "FieldRequirementInline:a",
              "OperationLookup:c",
              "OperationLookup:c",
              "OperationLookup:c",
              "OperationLookup:c",
              "Complete:c",
              "Complete:c"
            ]
            """);
    }

    [Fact]
    public void CreatePlan_Should_NotDequeueKeyOnlySchema_When_FieldIsDeferred()
    {
        // arrange
        using var listener = new DequeueListener();
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
              user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              name: String!
            }
            """,
            """
            # name: b
            type Query {
              userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            type Query {
              userByKey(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              email: String!
            }
            """);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var planner = new OperationPlanner(
            schema,
            new OperationCompiler(schema, pool),
            new OperationPlannerOptions { MaxExpandedNodes = 1_000 });
        var operation = Utf8GraphQLParser.Parse(
                """
                { user(id: "1") { name ... @defer { id email } } }
                """)
            .Definitions.OfType<OperationDefinitionNode>().First();
        const string operationId = "no_progress_lookup_deferred_dequeues";

        // act
        planner.CreatePlan(
            operationId,
            operationId,
            "12345678",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        listener.Dequeues(operationId + "#defer_0").MatchInlineSnapshot(
            """
            [
              "OperationLookup:c",
              "OperationRoot:a",
              "Complete:a",
              "OperationLookup:a"
            ]
            """);
    }

    [Fact]
    public void CreatePlan_Should_ReturnPlan_When_NoProgressLookupCandidatesMeetTheAdmissibleBound()
    {
        // arrange
        var schema = CreateExternalKeySchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var planner = new OperationPlanner(schema, new OperationCompiler(schema, pool));
        var operation = Utf8GraphQLParser.Parse("{ root { f24 } }")
            .Definitions.OfType<OperationDefinitionNode>().First();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        // act
        var plan = planner.CreatePlan(
            "no_progress_lookup_cancellation",
            "no_progress_lookup_cancellation",
            "12345678",
            operation,
            cts.Token);

        // assert
        plan.AllNodes.Select(node => node.SchemaName).MatchInlineSnapshot(
            """
            [
              "a",
              "a",
              "c"
            ]
            """);
    }

    private static FusionSchemaDefinition CreateExternalKeySchema()
    {
        var sources = new List<SourceSchemaText>
        {
            new(
                "a",
                """
                extend schema
                  @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

                type Query {
                  root: T
                }

                type T @key(fields: "id") @key(fields: "id k") {
                  id: ID!
                  k: Int @external
                  f24: Int @requires(fields: "k")
                }
                """),
            new(
                "b",
                """
                extend schema
                  @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external"])

                type T @key(fields: "id") @key(fields: "id k") {
                  id: ID!
                  k: Int @external
                }
                """),
            new(
                "c",
                """
                extend schema
                  @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

                type T @key(fields: "id") {
                  id: ID!
                  k: Int
                }
                """)
        };

        var options = new SchemaComposerOptions();
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

        var log = new CompositionLog();
        var result = new SchemaComposer(sources, options, log).Compose();

        if (!result.IsSuccess)
        {
            var errors = new StringBuilder();
            foreach (var entry in log)
            {
                errors.AppendLine($"[{entry.Severity}] {entry.Code}: {entry.Message}");
            }

            throw new InvalidOperationException(errors.ToString());
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private sealed class DequeueListener : EventListener
    {
        private readonly ConcurrentQueue<(string OperationId, string Dequeue)> _dequeues = [];

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals(PlannerEventSource.EventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == PlannerEventSource.PlanDequeueEventId
                && eventData.Payload is [string operationId, _, _, string nextWorkItem, string schemaName])
            {
                _dequeues.Enqueue((operationId, $"{nextWorkItem}:{schemaName}"));
            }
        }

        public string[] Dequeues(string operationId)
            => _dequeues
                .Where(t => t.OperationId.Equals(operationId, StringComparison.Ordinal))
                .Select(t => t.Dequeue)
                .ToArray();
    }
}
