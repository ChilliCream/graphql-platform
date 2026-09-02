using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class PolicyPlanningTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_GateFieldAndObjectCoordinates_When_PoliciesAreRequestCacheable()
    {
        // arrange
        var schema = CreatePolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($includeName: Boolean!) {
              product {
                id
                name @include(if: $includeName)
              }
            }
            """);
        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicyNode()
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              secret
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        using var roundTripBuffer = new PooledArrayWriter();
        formatter.Format(roundTripBuffer, parsedPlan);

        var original = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var roundTripped = Encoding.UTF8.GetString(roundTripBuffer.WrittenSpan);

        // assert
        Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Single(parsedPlan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicyNode_When_GuardedProducerIsBatched()
    {
        // arrange
        var schema = CreateBatchedPolicySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              first {
                id
                rating
              }
              second {
                id
                rating
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        Assert.Single(parsedPlan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Single(parsedPlan.AllNodes.OfType<OperationBatchExecutionNode>());
    }

    [Fact]
    public void CreatePlan_Should_RejectSiblingDeferredBatchedPolicyTargets()
    {
        // arrange
        var schema = CreateBatchedPolicySchema();

        // act
        // repo-ias owns support for this combined deferred batch shape.
        var exception = Assert.Throws<InvalidOperationException>(() => PlanOperation(
            schema,
            """
            {
              first {
                id
                ... @defer {
                  rating
                }
              }
              second {
                id
                ... @defer {
                  rating
                }
              }
            }
            """));

        // assert
        Assert.Equal(
            "Every required compiled policy occurrence facet must be claimed exactly once.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicyRequirements()
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationDocument = Utf8GraphQLParser.Parse(
            """
            {
              secret
            }
            """);
        var rewritten = new DocumentRewriter(schema).RewriteDocument(
            operationDocument,
            operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().Single();
        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);
        var plan = planner.CreatePlan(
            "123456789101112",
            "123456789101112",
            "123456789101112",
            operation,
            TestContext.Current.CancellationToken);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        using var roundTripBuffer = new PooledArrayWriter();
        formatter.Format(roundTripBuffer, parsedPlan);

        // assert
        Assert.Equal(
            Encoding.UTF8.GetString(buffer.WrittenSpan),
            Encoding.UTF8.GetString(roundTripBuffer.WrittenSpan));
        var target = Assert.Single(
            parsedPlan.AllNodes
                .OfType<PolicyExecutionNode>()
                .SelectMany(t => t.Targets.ToArray()),
            t => t.Policies.Any(
                p => p.Groups.Any(g => g.Contains("CanReadSecret", StringComparer.Ordinal))));
        var requirement = Assert.Single(target.Requirements);
        Assert.Equal(
            ("CanReadSecret", "{ role }", true),
            (
                requirement.PolicyName,
                requirement.SelectionSet.ToString(indented: false),
                new YamlOperationPlanFormatter()
                    .Format(parsedPlan)
                    .Contains("selectionSet: { role }", StringComparison.Ordinal)));
    }

    [Fact]
    public void CreatePlan_Should_UseNewRequirement_When_PolicyChangesFromEmptyRequirement()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("CanReadSecret"));
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                """
                schema {
                  query: Query
                }

                type Query @fusion__type(schema: A) {
                  secret: String
                    @fusion__field(schema: A)
                    @fusion__policy(names: "CanReadSecret")
                  id: ID! @fusion__field(schema: A)
                  ownerId: ID! @fusion__field(schema: A)
                }

                enum fusion__Schema {
                  A @fusion__schema_metadata(name: "A")
                }
                """),
            services);

        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);

        var operationDocument = Utf8GraphQLParser.Parse(
            """
            {
              secret
            }
            """);
        var rewritten = new DocumentRewriter(schema).RewriteDocument(
            operationDocument,
            operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var firstPlan = planner.CreatePlan(
            "123456789101112",
            "123456789101112",
            "123456789101112",
            operation,
            TestContext.Current.CancellationToken);

        // Republish the policy with a resource requirement through the same planner instance.
        provider.Emit(
            new TestPolicy("CanReadSecret", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id ownerId }")));

        var secondPlan = planner.CreatePlan(
            "223456789101112",
            "223456789101112",
            "223456789101112",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(firstPlan.AllNodes.OfType<PolicyExecutionNode>());
        var secondRequirement = Assert.Single(
            secondPlan.AllNodes
                .OfType<PolicyExecutionNode>()
                .SelectMany(t => t.Targets.ToArray())
                .Single(t => t.Policies.Any(
                    p => p.Groups.Any(g => g.Contains("CanReadSecret", StringComparer.Ordinal))))
                .Requirements);
        var firstPolicy = Assert.Single(firstPlan.Policies, entry => entry.PolicyName == "CanReadSecret");
        var secondPolicy = Assert.Single(secondPlan.Policies, entry => entry.PolicyName == "CanReadSecret");

        Assert.Equal(
            (
                PolicyPlanEntry.ComputeRequirementHash(null),
                "{ id ownerId }",
                PolicyPlanEntry.ComputeRequirementHash(secondRequirement.SelectionSet)),
            (
                firstPolicy.RequirementHash,
                secondRequirement.SelectionSet.ToString(indented: false),
                secondPolicy.RequirementHash));
    }

    [Theory]
    [InlineData(
        "[]",
        "A policy in the operation plan must contain at least one policy name group.")]
    [InlineData(
        "[[]]",
        "A policy name group in the operation plan must contain at least one policy name.")]
    [InlineData(
        "[[1]]",
        "A policy name in the operation plan must be a string.")]
    public void JsonParser_Should_RejectPolicyNode_When_PolicyNamesAreMalformed(
        string namesJson,
        string expectedMessage)
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              secret
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        var policyNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Policy");
        var target = policyNode["targets"]!.AsArray()[0]!.AsObject();
        target["policies"]!.AsArray()[0]!.AsObject()["names"] = JsonNode.Parse(namesJson);
        var planSource = Encoding.UTF8.GetBytes(json.ToJsonString());

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(planSource));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_GuardDownstreamLookup_When_ProtectedEntityIsFetchedFirst()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  topProducts: [Product!]
                }

                type Product @key(fields: "id") @policy(names: "CanReadProduct") {
                  id: ID!
                  name: String!
                }
                """,
                """
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  price: Float!
                }
                """),
            new TestPolicy("CanReadProduct"));

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              topProducts {
                name
                price
              }
            }
            """);
        // assert
        var producer = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            t => t.SchemaName == "a");
        var downstream = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            t => t.SchemaName == "b");
        var slot = Assert.Single(plan.PolicySlots);
        Assert.Contains(
            downstream.Conditions.ToArray(),
            condition => condition.VariableName == slot.VariableName && condition.PassingValue);
        Assert.Empty(plan.AllNodes.OfType<PolicyExecutionNode>());
    }

    [Fact]
    public void CreatePlan_Should_GateRootStep_When_AllRootPoliciesAreRequestCacheable()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                id
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_RetainPolicyNode_When_MixedCoordinateHasDataBearingAbortApplication()
    {
        // arrange
        // A request-cacheable NULL application shares its coordinate with a data-bearing ABORT application.
        var schema = CreateMixedResidualCoordinateSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                id
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_ForwardSlotVariable_When_ProtectedFieldSharesOperationNode()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();

        // act
        var plan = PlanOperation(schema, "{ public secret }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_PreserveObjectCardinality_When_ObjectPolicyUsesSlot()
    {
        // arrange
        var schema = CreateReturnedObjectPolicySchema();

        // act
        var plan = PlanOperation(schema, "{ product { id name } }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_RetainFullExpression_When_ApplicationMixesPolicyKinds()
    {
        // arrange
        var schema = CreateMixedApplicationSchema();

        // act
        var plan = PlanOperation(schema, "{ public secret }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_KeepTautologicalRequestProjectionResidualOnly()
    {
        // arrange
        var schema = CreateTautologicalMixedApplicationSchema();

        // act
        var plan = PlanOperation(schema, "{ secret }");

        // assert
        var target = Assert.Single(
            Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>()).Targets.ToArray());
        ($"slots={plan.PolicySlots.Length}; residuals="
            + $"{plan.AllNodes.OfType<PolicyExecutionNode>().Count()}; "
            + $"expression={Assert.Single(target.Policies).Format()}")
            .MatchInlineSnapshot(
                """
                slots=0; residuals=1; expression=(CanRequest AND CanResource) OR CanResource
                """);
    }

    [Fact]
    public void JsonParser_Should_RejectSlotFacetForTautologicalRequestProjection()
    {
        // arrange
        var schema = CreateTautologicalMixedApplicationSchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ gated secret }"));
        var slot = json["policySlots"]!.AsArray()[0]!.AsObject();
        var coordinate = slot["coordinates"]!.AsArray()[0]!.DeepClone().AsObject();
        var residualTarget = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy")
            ["targets"]!.AsArray()[0]!.AsObject();
        var occurrence = residualTarget["occurrences"]!.AsArray()[0]!.DeepClone().AsObject();
        occurrence["facet"] = "slot-gate";
        coordinate["fieldName"] = "secret";
        coordinate["responseNames"] = new JsonArray("secret");
        coordinate["occurrences"] = new JsonArray(occurrence);
        slot["coordinates"]!.AsArray().Add(coordinate);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy gate coordinate does not match its compiled occurrence.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_KeepPolicyNode_When_ObjectGateWouldHidePolicyRequirement()
    {
        // arrange
        var schema = CreateRequirementFeedSchema();

        // act
        var plan = PlanOperation(schema, "{ product { name } }");

        // assert
        var coordinate = Assert.Single(Assert.Single(plan.PolicySlots).Coordinates);
        Assert.Empty(coordinate.GateGuardMasks);
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_AllowRequestCacheableProtectedRequirementFeed()
    {
        // arrange
        var schema = CreateProtectedRequirementFeedSchema(
            "{ id }",
            new TestPolicy("CanReadId"),
            new TestPolicy("CanReadSecret"));

        // act
        var plan = PlanOperation(schema, "{ product { name } }");

        // assert
        var coordinate = Assert.Single(
            plan.PolicySlots.SelectMany(slot => slot.Coordinates),
            coordinate => coordinate.TypeName == "Product"
                && coordinate.FieldName == "id");
        Assert.Empty(coordinate.GateGuardMasks);
    }

    [Theory]
    [InlineData("{ id }", "Product.id")]
    [InlineData("{ details { secret } }", "Details.secret")]
    public void CreatePlan_Should_RejectDataBearingProtectedRequirementFeed(
        string requirements,
        string coordinate)
    {
        // arrange
        var schema = CreateProtectedRequirementFeedSchema(
            requirements,
            new TestPolicy(
                "CanReadId",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ name }")),
            new TestPolicy(
                "CanReadSecret",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ public }")));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "{ product { name } }"));

        // assert
        Assert.Equal(
            "Authorization policy 'CanReadProduct' requires protected field "
                + $"'{coordinate}', which would create an authorization cycle.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_CapPolicySlots_When_OperationCarriesMoreThanMaxPolicySlots()
    {
        // arrange
        var schema = CreateManyFieldPoliciesSchema(65);

        // act
        var plan = PlanOperation(
            schema,
            "query { " + string.Join(' ', Enumerable.Range(0, 65).Select(i => $"field{i}")) + " }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_JoinLateSMemberAndFallbackLateIdentity_When_PolicySlotsAreFull()
    {
        // arrange
        var schema = CreateCapacityIdentitySchema();

        // act
        var plan = PlanOperation(
            schema,
            "{ "
                + string.Join(' ', Enumerable.Range(0, 64).Select(i => $"mixed{i}"))
                + " lateAllocated lateUnallocated }");
        var (json, parser) = SerializePlan(schema, plan);
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        var allocatedSlot = parsedPlan.PolicySlots.Single(slot =>
            slot.Applications.Any(application =>
                parsedPlan.PolicyExpressions[application.ExpressionOrdinal].Groups
                    .SelectMany(group => group)
                    .Contains("Request0", StringComparer.Ordinal)));
        var lateFallback = parsedPlan.AllNodes
            .OfType<PolicyExecutionNode>()
            .SelectMany(node => node.Targets.ToArray())
            .Single(target => target.Path.Name == "lateUnallocated");
        ($"slots={parsedPlan.PolicySlots.Length}; "
            + $"lateAllocated={allocatedSlot.Coordinates.Any(c => c.FieldName == "lateAllocated")}; "
            + $"lateUnallocated={lateFallback.Policies.Single().Format()}")
            .MatchInlineSnapshot(
                """
                slots=64; lateAllocated=True; lateUnallocated=LateRequest
                """);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicySlots()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                id
              }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var compiler = new OperationCompiler(schema, pool);
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        MatchInline(
            parsedPlan,
            """
            operation:
              - document: |
                  {
                    product {
                      id
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 1
            nodes:
              - id: 1
                type: Operation
                schema: A
                operation: |
                  query Op_123456789101112_1 {
                    product {
                      id
                    }
                  }
                conditions:
                  - variable: $__fusion_policy_0
                    passingValue: true
            policyExpressions:
              - ordinal: 0
                names: [["CanReadQuery"]]
                expression: CanReadQuery
              - ordinal: 1
                names: [["CanAudit"]]
                expression: CanAudit
            policySlots:
              - ordinal: 0
                variable: $__fusion_policy_0
                applications:
                  - expressionOrdinal: 0
                    onDenied: Error
                  - expressionOrdinal: 1
                    onDenied: Null
                rmax: Null
                guardMasks: [0]
                coordinates:
                  - typeName: Query
                    responseNames: []
                    applications:
                      - expressionOrdinal: 0
                        onDenied: Error
                      - expressionOrdinal: 1
                        onDenied: Null
                    isRoot: true
                    liveGuardMasks: [0]
                    gateGuardMasks: [0]
            policies:
              - name: CanAudit
                requirementHash: 17241709254077376921
              - name: CanReadQuery
                requirementHash: 17241709254077376921
            """);
    }

    [Fact]
    public void JsonParser_Should_RoundTripDeferredPolicyArtifacts_When_SelectionIdentityIsPlanLocal()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  before: String
                  topProducts: [Product!]
                }

                type Product @key(fields: "id") {
                  id: ID!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  price: Float @policy(names: "CanReadSecret", onDenied: ERROR)
                }
                """),
            new TestPolicy("CanReadSecret"));
        var plan = PlanOperation(
            schema,
            "{ before topProducts { id ... @defer { price } } }");
        var (json, parser) = SerializePlan(schema, plan);
        var incrementalOperation = plan.IncrementalPlans.Single().Operation;
        var mainAnchor = plan.Operation.RootSelectionSet.Selections.ToArray()
            .Single(selection => selection.ResponseName.Equals("topProducts", StringComparison.Ordinal));
        var incrementalAnchor = incrementalOperation.RootSelectionSet.Selections.ToArray()
            .Single(selection => selection.ResponseName.Equals("topProducts", StringComparison.Ordinal));

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        Assert.NotSame(mainAnchor, incrementalAnchor);
        Assert.Equal(
            new JsonOperationPlanFormatter().Format(plan),
            new JsonOperationPlanFormatter().Format(parsedPlan));
    }

    [Theory]
    [InlineData("missingTable", "The operation-wide include-condition table is required.")]
    [InlineData("missing", "A client include condition is missing from the operation-wide condition table.")]
    [InlineData("extra", "Every operation-wide include condition must be used by a compiled operation.")]
    [InlineData("duplicate", "The operation-wide include-condition table must be unique and contain at most 64 entries.")]
    [InlineData("over64", "The operation-wide include-condition table must be unique and contain at most 64 entries.")]
    [InlineData("order", "The operation-wide include-condition table must be in canonical order.")]
    public void JsonParser_Should_RejectMalformedIncludeConditionTable(
        string mutation,
        string expectedMessage)
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                "query($a: Boolean!, $b: Boolean!) { one: secret @include(if: $a) two: secret @include(if: $b) }"));
        var conditions = json["includeConditions"]!.AsArray();

        switch (mutation)
        {
            case "missingTable":
                json.Remove("includeConditions");
                break;
            case "missing":
                conditions.RemoveAt(0);
                break;
            case "extra":
                conditions.Add(new JsonObject { ["includeVariable"] = "c" });
                break;
            case "duplicate":
                conditions.Add(conditions[0]!.DeepClone());
                break;
            case "over64":
                conditions.Clear();
                for (var i = 0; i < 65; i++)
                {
                    conditions.Add(new JsonObject { ["includeVariable"] = $"v{i:D2}" });
                }
                break;
            case "order":
                var first = conditions[0]!.DeepClone();
                conditions[0] = conditions[1]!.DeepClone();
                conditions[1] = first;
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_UnionClientIncludeMasks_When_GateIsReused()
    {
        // arrange
        var schema = CreateLivenessPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($first: Boolean!, $second: Boolean!) {
              first @include(if: $first)
              second @include(if: $second)
            }
            """);

        // assert
        var gate = Assert.Single(plan.PolicySlots);
        Assert.Equal(new ulong[] { 1, 2 }, gate.GuardMasks);
        Assert.Single(plan.PolicyExpressions);
        Assert.Single(plan.Policies);
    }

    [Fact]
    public void CreatePlan_Should_CollapseGateLiveness_When_ReuseIsUnconditional()
    {
        // arrange
        var schema = CreateLivenessPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($first: Boolean!) {
              first @include(if: $first)
              second
            }
            """);

        // assert
        Assert.Equal(new ulong[] { 0 }, Assert.Single(plan.PolicySlots).GuardMasks);
    }

    [Fact]
    public void CreatePlan_Should_CollapseCoordinateLiveness_When_AliasReuseIsUnconditional()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($include: Boolean!) {
              first: secret
              second: secret @include(if: $include)
            }
            """);

        // assert
        var coordinate = Assert.Single(Assert.Single(plan.PolicySlots).Coordinates);
        Assert.Equal(new ulong[] { 0 }, coordinate.LiveGuardMasks);
    }

    [Fact]
    public void CreatePlan_Should_UnionDuplicateResponseConditionMasks()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($first: Boolean!, $second: Boolean!) {
              secret @include(if: $first)
              secret @include(if: $second)
            }
            """);

        // assert
        var coordinate = Assert.Single(Assert.Single(plan.PolicySlots).Coordinates);
        Assert.Equal(new ulong[] { 1, 2 }, coordinate.LiveGuardMasks);
        Assert.Equal(new[] { "secret" }, coordinate.ResponseNames);
    }

    [Fact]
    public void CreatePlan_Should_CollapseDuplicateResponseLiveness_When_OneOccurrenceIsUnconditional()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($include: Boolean!) {
              secret @include(if: $include)
              secret
            }
            """);

        // assert
        var slot = Assert.Single(plan.PolicySlots);
        var coordinate = Assert.Single(slot.Coordinates);
        $"slot={string.Join(',', slot.GuardMasks)}; coordinate={string.Join(',', coordinate.LiveGuardMasks)}"
            .MatchInlineSnapshot("slot=0; coordinate=0");
    }

    [Fact]
    public void CreatePlan_Should_KeepConcreteAndAbstractOccurrenceLivenessSeparate()
    {
        // arrange
        var schema = CreateMixedConcreteAbstractPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            "query($concrete: Boolean!, $abstract: Boolean!) { "
                + "product @include(if: $concrete) { id } "
                + "result @include(if: $abstract) { ... on Product { id } } } ");

        // assert
        var coordinate = Assert.Single(Assert.Single(plan.PolicySlots).Coordinates);
        $"live={string.Join(',', coordinate.LiveGuardMasks)}; gate={string.Join(',', coordinate.GateGuardMasks)}"
            .MatchInlineSnapshot("live=1,2; gate=2");
    }

    [Fact]
    public void CreatePlan_Should_AllowConditionalFetchGateUnderUnconditionalCoordinateLiveness()
    {
        // arrange
        var schema = CreateMixedConcreteAbstractPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            "query($concrete: Boolean!) { "
                + "product @include(if: $concrete) { id } "
                + "result { ... on Product { id } } } ");

        // assert
        var coordinate = Assert.Single(Assert.Single(plan.PolicySlots).Coordinates);
        $"live={string.Join(',', coordinate.LiveGuardMasks)}; gate={string.Join(',', coordinate.GateGuardMasks)}"
            .MatchInlineSnapshot("live=0; gate=1");
    }

    [Fact]
    public void CreatePlan_Should_RetainDeferredPolicyRegistryAndGlobalIncludeOrdinals()
    {
        // arrange
        var schema = CreateDeferredLivenessPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($main: Boolean!, $deferred: Boolean!) {
              user {
                main @include(if: $main)
                ... @defer {
                  secret @include(if: $deferred)
                }
              }
            }
            """);
        var (json, parser) = SerializePlan(schema, plan);
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));
        var deferredNode = plan.IncrementalPlans.Single().AllNodes
            .OfType<OperationExecutionNode>()
            .Single();

        // assert
        ($"slots={string.Join(',', plan.PolicySlots.Select(gate => $"{gate.Ordinal}:{string.Join(',', gate.GuardMasks)}"))}; "
            + $"deferred={string.Join(',', deferredNode.Conditions.ToArray().Select(condition => condition.VariableName))}; "
            + $"policies={string.Join(',', plan.Policies.Select(policy => policy.PolicyName))}; "
            + $"roundTrip={new JsonOperationPlanFormatter().Format(plan) == new JsonOperationPlanFormatter().Format(parsedPlan)}")
            .MatchInlineSnapshot(
                """
                slots=0:2,1:1; deferred=__fusion_policy_1,deferred; policies=Deferred,Main; roundTrip=True
                """);
    }

    [Fact]
    public void CreatePlan_Should_IncludeDeferredDataBearingPolicyTarget()
    {
        // arrange
        var schema = CreateDeferredDataPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              user {
                ... @defer {
                  secret
                }
              }
            }
            """);
        var incrementalPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(incrementalPlan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Equal(1, policyNode.Targets.Length);
        var target = policyNode.Targets[0];

        // assert
        ($"roots={string.Join(',', incrementalPlan.RootNodes.Select(node => node.GetType().Name))}; "
            + $"all={string.Join(',', incrementalPlan.AllNodes.Select(node => node.GetType().Name))}; "
            + $"target={target.TypeName}.{target.Path}; requirement={target.Requirements[0].SelectionSet}")
            .MatchInlineSnapshot(
                """
                roots=OperationExecutionNode; all=OperationExecutionNode,PolicyExecutionNode; target=User.$.user.secret; requirement={
                  role
                }
                """);
    }

    [Fact]
    public void JsonParser_Should_RejectUnanchoredDeferredPolicyRoot_When_IncrementalDependencyIsRemoved()
    {
        // arrange
        var schema = CreateDeferredDataPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                """
                {
                  user {
                    ... @defer {
                      secret
                    }
                  }
                }
                """));
        var policy = json["incrementalPlans"]!.AsArray()[0]!["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        policy["dependencies"]!.AsArray().Clear();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node dependencies must exactly match its producer and requirement providers.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_KeepDeferredPolicyParentAnchored_When_PairedProviderIsLifted()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var incrementalPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(incrementalPlan.AllNodes.OfType<PolicyExecutionNode>());
        var (json, parser) = SerializePlan(schema, plan);

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));
        var parsedPolicyNode = Assert.Single(
            Assert.Single(parsedPlan.IncrementalPlans).AllNodes.OfType<PolicyExecutionNode>());

        // assert
        ($"roots={string.Join(',', incrementalPlan.RootNodes.Select(node => node.GetType().Name))}; "
            + $"incrementalOperations={string.Join(',', incrementalPlan.AllNodes.OfType<OperationExecutionNode>().Select(node => node.SchemaName))}; "
            + $"dependencies={policyNode.Dependencies.Length}; "
            + $"parentDependencies={string.Join(',', policyNode.ParentDependencies.ToArray())}; "
            + $"roundTripParentDependencies={string.Join(',', parsedPolicyNode.ParentDependencies.ToArray())}; "
            + $"policies={string.Join(',', plan.Policies.Select(entry => entry.PolicyName))}")
            .MatchInlineSnapshot(
                "roots=OperationExecutionNode; incrementalOperations=c; dependencies=1; parentDependencies=1,2; roundTripParentDependencies=1,2; policies=CanReadReviews");
    }

    [Fact]
    public void CreatePlan_Should_RejectNestedDeferredPolicy_When_ImmediateParentScopeCannotAuthorizeIt()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product(id: ID!): Product @lookup
                }

                type Product @key(fields: "id") {
                  id: ID!
                  name: String!
                }
                """,
                """
                # name: b
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  description: String!
                  productSku: String!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  reviews(productSku: String! @require(field: "productSku")): [String!]!
                    @policy(names: "CanReadReviews", onDenied: NULL)
                }
                """),
            new TestPolicy(
                "CanReadReviews",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ productSku }")));

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  product(id: "1") {
                    name
                    ... @defer(label: "outer") {
                      description
                      productSku
                      ... @defer(label: "inner") {
                        reviews
                      }
                    }
                  }
                }
                """));

        // assert
        Assert.Equal(
            "The deferred policy target 'Product.reviews' in nested scope 'inner' "
            + "cannot be authorized from its immediate parent scope.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryFindNestedParentAuthorityGap_Should_RejectAmbiguousProviderAcrossSplitParentPieces(
        bool reversePieces)
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var policyNode = Assert.Single(
            Assert.Single(plan.IncrementalPlans).AllNodes.OfType<PolicyExecutionNode>());
        var outer = new DeliveryGroup("outer", Parent: null, DeferConditionIndex: 0) { Id = 100 };
        var inner = new DeliveryGroup("inner", outer, DeferConditionIndex: 0) { Id = 101 };
        var firstParent = new IncrementalPlan(
            plan.Operation,
            plan.RootNodes,
            plan.AllNodes,
            [outer],
            requirements: []);
        var secondParent = new IncrementalPlan(
            plan.Operation,
            plan.RootNodes,
            plan.AllNodes,
            [outer],
            requirements: []);
        var child = new IncrementalPlan(
            plan.Operation,
            [policyNode],
            [policyNode],
            [inner],
            requirements: []);
        var incrementalPlans = reversePieces
            ? ImmutableArray.Create(secondParent, firstParent, child)
            : ImmutableArray.Create(firstParent, secondParent, child);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            incrementalPlans,
            rootNodes: [],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.reviews", coordinate);
        Assert.Equal("inner", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_UseUnambiguousProviderFromLaterSplitParentPiece()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var policyNode = Assert.Single(
            Assert.Single(plan.IncrementalPlans).AllNodes.OfType<PolicyExecutionNode>());
        var outer = new DeliveryGroup("outer", Parent: null, DeferConditionIndex: 0) { Id = 100 };
        var inner = new DeliveryGroup("inner", outer, DeferConditionIndex: 0) { Id = 101 };
        var firstParent = new IncrementalPlan(
            plan.Operation,
            rootNodes: [],
            allNodes: [new PolicyExecutionNode(999, [], [])],
            deliveryGroups: [outer],
            requirements: []);
        var secondParent = new IncrementalPlan(
            plan.Operation,
            plan.RootNodes,
            plan.AllNodes,
            [outer],
            requirements: []);
        var child = new IncrementalPlan(
            plan.Operation,
            [policyNode],
            [policyNode],
            [inner],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [firstParent, secondParent, child],
            rootNodes: [],
            out var coordinate,
            out var scope);

        // assert
        Assert.False(hasGap);
        Assert.Equal(string.Empty, coordinate);
        Assert.Equal(string.Empty, scope);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("ambiguous")]
    public void TryFindNestedParentAuthorityGap_Should_ReturnPolicyTarget_When_ImmediateParentScopeIsInvalid(
        string topology)
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var policyNode = Assert.Single(
            Assert.Single(plan.IncrementalPlans).AllNodes.OfType<PolicyExecutionNode>());
        var firstOuter = new DeliveryGroup("first", Parent: null, DeferConditionIndex: 0) { Id = 100 };
        var firstInner = new DeliveryGroup("inner", firstOuter, DeferConditionIndex: 0) { Id = 101 };
        var secondOuter = new DeliveryGroup("second", Parent: null, DeferConditionIndex: 0) { Id = 102 };
        var secondInner = new DeliveryGroup("inner", secondOuter, DeferConditionIndex: 0) { Id = 103 };
        var groups = topology is "missing"
            ? ImmutableArray.Create(firstInner)
            : ImmutableArray.Create(firstInner, secondInner);
        var child = new IncrementalPlan(
            plan.Operation,
            [policyNode],
            [policyNode],
            groups,
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [child],
            rootNodes: [],
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.reviews", coordinate);
        Assert.Equal("inner", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_ReportLaterTarget_When_ItLacksParentAuthority()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var incrementalPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(incrementalPlan.AllNodes.OfType<PolicyExecutionNode>());
        var originalTarget = policyNode.Targets[0];
        var unavailableTarget = originalTarget with
        {
            Path = SelectionPath.Parse("$.product.unavailable"),
            Requirements =
            [
                new PolicyRequirement
                {
                    PolicyName = "CanReadReviews",
                    SelectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ unavailable }")
                }
            ]
        };
        policyNode.SetTargets([originalTarget, unavailableTarget]);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [incrementalPlan],
            plan.AllNodes,
            out var coordinate,
            out var scope);

        // assert
        Assert.True(hasGap);
        Assert.Equal("Product.unavailable", coordinate);
        Assert.Equal("$.product", scope);
    }

    [Fact]
    public void TryFindNestedParentAuthorityGap_Should_IgnoreDuplicateIdsInNonPolicySplitParentPieces()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var outer = new DeliveryGroup("outer", Parent: null, DeferConditionIndex: 0) { Id = 100 };
        var inner = new DeliveryGroup("inner", outer, DeferConditionIndex: 0) { Id = 101 };
        var firstParent = new IncrementalPlan(
            plan.Operation,
            rootNodes: [],
            allNodes: [new PolicyExecutionNode(999, [], [])],
            deliveryGroups: [outer],
            requirements: []);
        var secondParent = new IncrementalPlan(
            plan.Operation,
            rootNodes: [],
            allNodes: [new PolicyExecutionNode(999, [], [])],
            deliveryGroups: [outer],
            requirements: []);
        var child = new IncrementalPlan(
            plan.Operation,
            rootNodes: [],
            allNodes: [],
            deliveryGroups: [inner],
            requirements: []);

        // act
        var hasGap = PolicyArtifactBinder.TryFindNestedParentAuthorityGap(
            [firstParent, secondParent, child],
            rootNodes: [],
            out var coordinate,
            out var scope);

        // assert
        Assert.False(hasGap);
        Assert.Equal(string.Empty, coordinate);
        Assert.Equal(string.Empty, scope);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bind_Should_RejectAmbiguousParentAuthorityAcrossSplitPieces(bool reversePieces)
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (incrementalPlans, slots) = CreateStrictSplitParentAuthorityFixture(
            schema,
            plan,
            firstParentNodes: plan.AllNodes,
            secondParentNodes: plan.AllNodes,
            reversePieces);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PolicyArtifactBinder.Bind(
                plan.Operation,
                incrementalPlans,
                plan.PolicyExpressions,
                slots,
                plan.Policies,
                plan.AllNodes));

        // assert
        Assert.Equal(
            "A policy parent requirement provider is ambiguous across immediate parent plan pieces.",
            exception.Message);
    }

    [Fact]
    public void Bind_Should_AcceptAuthorityFromLaterSplitParentPiece()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (incrementalPlans, slots) = CreateStrictSplitParentAuthorityFixture(
            schema,
            plan,
            firstParentNodes: [],
            secondParentNodes: plan.AllNodes,
            reversePieces: false);

        // act
        var boundSlots = PolicyArtifactBinder.Bind(
            plan.Operation,
            incrementalPlans,
            plan.PolicyExpressions,
            slots,
            plan.Policies,
            plan.AllNodes);

        // assert
        Assert.Equal(slots, boundSlots);
    }

    [Fact]
    public void Bind_Should_AcceptDuplicateNonPolicyId_When_ItDoesNotContributeParentAuthority()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var neutralPlan = PlanOperation(schema, "{ product(id: \"1\") { name } }");
        var nonPolicyNode = Assert.Single(neutralPlan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Contains(plan.AllNodes, node => node.Id == nonPolicyNode.Id);
        var (incrementalPlans, slots) = CreateStrictSplitParentAuthorityFixture(
            schema,
            plan,
            firstParentNodes: [nonPolicyNode],
            secondParentNodes: plan.AllNodes,
            reversePieces: false);

        // act
        var boundSlots = PolicyArtifactBinder.Bind(
            plan.Operation,
            incrementalPlans,
            plan.PolicyExpressions,
            slots,
            plan.Policies,
            plan.AllNodes);

        // assert
        Assert.Equal(slots, boundSlots);
    }

    [Theory]
    [InlineData("stripped")]
    [InlineData("extra")]
    [InlineData("swapped")]
    [InlineData("parent-before-numeric")]
    public void JsonParser_Should_RejectPolicyParentDependencies_When_ParentReferencesAreMutated(
        string mutation)
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (json, parser) = SerializePlan(schema, plan);
        var policy = json["incrementalPlans"]!.AsArray()[0]!["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var policyId = policy["id"]!.GetValue<int>();
        var parentDependencies = policy["dependencies"]!.AsArray()
            .Where(dependency => dependency is JsonObject)
            .ToArray();

        switch (mutation)
        {
            case "stripped":
                policy["dependencies"]!.AsArray().Remove(parentDependencies[0]);
                break;

            case "extra":
                policy["dependencies"]!.AsArray().Add(new JsonObject { ["parentNodeId"] = 999 });
                break;

            case "swapped":
                var dependencies = policy["dependencies"]!.AsArray();
                var firstIndex = dependencies.IndexOf(parentDependencies[0]);
                var secondIndex = dependencies.IndexOf(parentDependencies[1]);
                var first = dependencies[firstIndex]!.DeepClone();
                dependencies[firstIndex] = dependencies[secondIndex]!.DeepClone();
                dependencies[secondIndex] = first;
                break;

            case "parent-before-numeric":
                var orderedDependencies = policy["dependencies"]!.AsArray();
                var parentDependency = parentDependencies[0]!.DeepClone();
                orderedDependencies.Remove(parentDependencies[0]);
                orderedDependencies.Insert(0, parentDependency);
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        var expectedMessage = mutation switch
        {
            "swapped" => $"Policy execution node {policyId} parent dependencies must be in canonical order.",
            "parent-before-numeric" =>
                $"Policy execution node {policyId} numeric dependencies must precede parent dependencies.",
            _ => "A policy execution node parent dependencies must exactly match its parent requirement providers."
        };
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectFirstLevelPolicy_When_RootAuthorityAndReferencesAreRemoved()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (json, parser) = SerializePlan(schema, plan);
        var policy = json["incrementalPlans"]!.AsArray()[0]!["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var parentProviderIds = policy["dependencies"]!.AsArray()
            .OfType<JsonObject>()
            .Select(dependency => dependency["parentNodeId"]!.GetValue<int>())
            .ToHashSet();
        var rootNodes = json["nodes"]!.AsArray();

        foreach (var rootNode in rootNodes
            .Where(node => parentProviderIds.Contains(node!["id"]!.GetValue<int>()))
            .ToArray())
        {
            rootNodes.Remove(rootNode);
        }

        foreach (var parentDependency in policy["dependencies"]!.AsArray()
            .OfType<JsonObject>()
            .ToArray())
        {
            policy["dependencies"]!.AsArray().Remove(parentDependency);
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy parent requirement provider cannot be resolved from the immediate parent scope.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectParentProviderThatClaimsAnotherParentScope()
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (json, parser) = SerializePlan(schema, plan);
        var policy = json["incrementalPlans"]!.AsArray()[0]!["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var dependencies = policy["dependencies"]!.AsArray();
        var parentProviderId = dependencies
            .OfType<JsonObject>()
            .Max(dependency => dependency["parentNodeId"]!.GetValue<int>());
        var provider = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<int>() == parentProviderId);
        var parentDependency = dependencies
            .OfType<JsonObject>()
            .Single(dependency => dependency["parentNodeId"]!.GetValue<int>() == parentProviderId);
        dependencies.Remove(parentDependency);
        provider["dependencies"] = new JsonArray(new JsonObject { ["parentNodeId"] = 999 });

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy parent requirement provider cannot reference another parent scope.",
            exception.Message);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("detached")]
    [InlineData("ambiguous")]
    public void JsonParser_Should_RejectNonCanonicalDeliveryGroupTopology(string mutation)
    {
        // arrange
        var (schema, plan) = CreateParentAnchoredDeferredPolicyPlan();
        var (json, parser) = SerializePlan(schema, plan);
        var deliveryGroups = json["deliveryGroups"]!.AsArray();
        var parent = deliveryGroups[0]!.AsObject();
        var parentId = parent["id"]!.GetValue<int>();
        var incrementalPlan = json["incrementalPlans"]!.AsArray()[0]!.AsObject();

        switch (mutation)
        {
            case "duplicate":
                deliveryGroups.Add(parent.DeepClone());
                break;

            case "detached":
                var detached = parent.DeepClone().AsObject();
                detached["id"] = parentId + 1;
                detached["parentId"] = parentId;
                deliveryGroups.Add(detached);
                incrementalPlan["deliveryGroupIds"] = new JsonArray(parentId + 1);
                break;

            case "ambiguous":
                var sibling = parent.DeepClone().AsObject();
                sibling["id"] = parentId + 1;
                deliveryGroups.Add(sibling);
                var firstChild = parent.DeepClone().AsObject();
                firstChild["id"] = parentId + 2;
                firstChild["parentId"] = parentId;
                deliveryGroups.Add(firstChild);
                var secondChild = parent.DeepClone().AsObject();
                secondChild["id"] = parentId + 3;
                secondChild["parentId"] = parentId + 1;
                deliveryGroups.Add(secondChild);
                incrementalPlan["deliveryGroupIds"] = new JsonArray(parentId + 2, parentId + 3);
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        var expectedMessage = mutation switch
        {
            "duplicate" => "An operation plan cannot contain duplicate delivery group identifiers.",
            "detached" => "A non-root delivery group must have a matching immediate parent plan scope.",
            _ => "An incremental plan must have one unambiguous immediate parent delivery group."
        };
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_KeepBelowAnchorPolicyRequirementLocal_When_ProviderIsCrossSubgraph()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product(id: ID!): Product @lookup
                }

                type Product @key(fields: "id") {
                  id: ID!
                  name: String!
                }
                """,
                """
                # name: b
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  details: Details
                }

                type Details @key(fields: "id") {
                  id: ID!
                  ownerId: String!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  detailsById(id: ID!): Details @lookup @internal
                }

                type Details @key(fields: "id") {
                  id: ID!
                  secret: String @policy(names: "CanReadDetails", onDenied: NULL)
                }
                """),
            new TestPolicy(
                "CanReadDetails",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ ownerId }")));

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product(id: "1") {
                name
                ... @defer {
                  details {
                    secret
                  }
                }
              }
            }
            """);
        var incrementalPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(incrementalPlan.AllNodes.OfType<PolicyExecutionNode>());

        // assert
        ($"main={string.Join(',', plan.AllNodes.OfType<OperationExecutionNode>().Select(node => node.SchemaName))}; "
            + $"incremental={string.Join(',', incrementalPlan.AllNodes.OfType<OperationExecutionNode>().Select(node => node.SchemaName))}; "
            + $"dependencies={policyNode.Dependencies.Length}; "
            + $"parentDependencies={policyNode.ParentDependencies.Length}")
            .MatchInlineSnapshot("main=a; incremental=b,c; dependencies=2; parentDependencies=0");
    }

    [Fact]
    public void CreatePlan_Should_RepresentNodeRootFieldAndConcreteObjectPolicies()
    {
        // arrange
        var schema = CreateNodePolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              node(id: "account:1") {
                id
                ... on Account {
                  name
                }
              }
            }
            """);

        // assert
        Assert.Contains(plan.AllNodes, node => node is NodeFieldExecutionNode);
        Assert.Equal(
            new[] { "AccountObject", "NodeField", "QueryRoot" },
            plan.Policies.Select(policy => policy.PolicyName));
        Assert.Equal(3, plan.PolicySlots.Length);
        Assert.Empty(plan.AllNodes.OfType<PolicyExecutionNode>());
    }

    [Theory]
    [InlineData("{ item { id } }")]
    [InlineData("{ nodes { id } }")]
    [InlineData("{ result { ... on Product { id } } }")]
    public void CreatePlan_Should_InjectConcreteRequirementsForAbstractResults(string operation)
    {
        // arrange
        var schema = CreateAbstractRequirementPolicySchema();

        // act
        var plan = PlanOperation(schema, operation);

        // assert
        var source = Encoding.UTF8.GetString(
            Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>()).Operation.Value.Span);
        Assert.Contains("... on Product", source, StringComparison.Ordinal);
        Assert.Contains("ownerId", source, StringComparison.Ordinal);
        Assert.Equal(new[] { "CanReadProduct" }, plan.Policies.Select(policy => policy.PolicyName));
        Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>());
    }

    [Fact]
    public void CreatePlan_Should_CollectUnionFragmentFieldPolicyAndRequirementFeed()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  results: [SearchResult]
                }

                union SearchResult = Product | Viewer

                type Product {
                  secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
                  ownerId: ID! @policy(names: "CanReadOwner")
                }

                type Viewer {
                  name: String!
                }
                """),
            new TestPolicy(
                "CanReadSecret",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ ownerId }")),
            new TestPolicy("CanReadOwner"));

        // act
        var plan = PlanOperation(schema, "{ results { ... on Product { secret } } }");

        // assert
        var source = Encoding.UTF8.GetString(
            Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>()).Operation.Value.Span);
        var targets = plan.AllNodes
            .OfType<PolicyExecutionNode>()
            .SelectMany(node => node.Targets.ToArray())
            .Select(target => $"{target.TypeName}.{target.Path.Name}")
            .Order(StringComparer.Ordinal);
        ($"source={source}; targets={string.Join(',', targets)}; "
            + $"inventory={string.Join(',', plan.Policies.Select(policy => policy.PolicyName))}; "
            + $"slots={plan.PolicySlots.Length}")
            .MatchInlineSnapshot(
                """
                source=query Op_123456789101112_1 {
                  results {
                    __typename
                    ... on Product {
                      secret
                      ownerId
                    }
                  }
                }; targets=Product.secret; inventory=CanReadOwner,CanReadSecret; slots=1
                """);
    }

    [Fact]
    public void CreatePlan_Should_FinalizeInlineSourceRequirementPolicies()
    {
        // arrange
        var schema = CreateInlineSourceRequirementPolicySchema();

        // act
        var plan = PlanOperation(schema, "{ product { details { secret } } }");

        // assert
        ($"nodes={plan.AllNodes.OfType<OperationExecutionNode>().Count()}; "
            + $"inventory={string.Join(',', plan.Policies.Select(policy => policy.PolicyName))}; "
            + $"expressions={string.Join(',', plan.PolicyExpressions.Select(expression => expression.Format()))}; "
            + $"slots={plan.PolicySlots.Length}; "
            + $"residuals={plan.AllNodes.OfType<PolicyExecutionNode>().Count()}")
            .MatchInlineSnapshot(
                """
                nodes=2; inventory=ChildPolicy,FieldPolicy,ObjectPolicy; expressions=FieldPolicy,ObjectPolicy,ChildPolicy; slots=3; residuals=0
                """);
    }

    [Fact]
    public void CreatePlan_Should_FinalizeLookupSourceRequirementAndChildPolicies()
    {
        // arrange
        var schema = CreateLookupSourceRequirementPolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($enabled: Boolean!) {
              product {
                details @include(if: $enabled) {
                  secret
                }
              }
            }
            """);

        // assert
        ($"nodes={plan.AllNodes.OfType<OperationExecutionNode>().Count()}; "
            + $"inventory={string.Join(',', plan.Policies.Select(policy => policy.PolicyName))}; "
            + $"expressions={string.Join(',', plan.PolicyExpressions.Select(expression => expression.Format()))}; "
            + $"slots={plan.PolicySlots.Length}; "
            + $"masks={string.Join(';', plan.PolicySlots.Select(gate => string.Join(',', gate.GuardMasks)))}; "
            + $"residuals={plan.AllNodes.OfType<PolicyExecutionNode>().Count()}")
            .MatchInlineSnapshot(
                """
                nodes=3; inventory=ChildPolicy,FieldPolicy,ObjectPolicy; expressions=FieldPolicy,ObjectPolicy,ChildPolicy; slots=3; masks=1;1;1; residuals=0
                """);
    }

    [Fact]
    public void CreatePlan_Should_VetoCrossSchemaGate_When_ClientSelectionFeedsRequirement()
    {
        // arrange
        var schema = CreateCrossSchemaRequirementFeedPolicySchema();

        // act
        var plan = PlanOperation(schema, "{ product { id name } }");

        // assert
        var coordinate = Assert.Single(
            plan.PolicySlots.SelectMany(slot => slot.Coordinates),
            coordinate => coordinate.TypeName == "Product" && coordinate.FieldName is null);
        Assert.Empty(coordinate.GateGuardMasks);
        Assert.Equal(2, plan.AllNodes.OfType<OperationExecutionNode>().Count());
        Assert.Equal(
            new[] { "CanReadName", "CanReadProduct" },
            plan.Policies.Select(policy => policy.PolicyName));
        Assert.Contains(
            plan.AllNodes.OfType<PolicyExecutionNode>().SelectMany(node => node.Targets.ToArray()),
            target => target.Kind is PolicyTargetKind.Field
                && target.Path.ToString() == "$.product.name");
    }

    [Theory]
    [InlineData("expressionOrdinal", "Policy expression ordinals must be contiguous and zero-based.")]
    [InlineData("emptyApplications", "A policy gate must reference at least one policy expression.")]
    [InlineData("undefinedEnum", "The policy gate residual denial behavior is invalid.")]
    [InlineData("emptyMasks", "A policy gate must contain at least one liveness guard mask.")]
    [InlineData("noncanonicalMasks", "Policy gate guard masks must be canonical and reference defined include conditions.")]
    [InlineData("noncanonicalRefs", "Policy gate expression references must be canonical.")]
    [InlineData("missingCoordinates", "The policy gate is missing the required property 'coordinates'.")]
    [InlineData("emptyCoordinates", "A policy gate must control at least one coordinate.")]
    [InlineData("missingCoordinateApplications", "The policy gate coordinate is missing the required property 'applications'.")]
    [InlineData("emptyResponseNames", "A policy gate coordinate is malformed.")]
    [InlineData("invalidCoordinateRef", "A policy gate coordinate contains an invalid expression reference.")]
    [InlineData("duplicateCoordinate", "A policy gate coordinate is malformed.")]
    [InlineData("noncanonicalLiveMasks", "Policy gate coordinate live guard masks must be canonical and reference defined include conditions.")]
    [InlineData("uncoveredGateMask", "Policy gate coordinate fetch-gate guard masks must be covered by its live guard masks.")]
    [InlineData("droppedCoordinateApplication", "A policy gate coordinate must reference exactly the gate applications.")]
    [InlineData("duplicateCoordinateApplication", "A policy gate coordinate contains a duplicate expression reference.")]
    [InlineData("orphanSlotMask", "Policy gate guard masks must exactly match the coordinate guard masks.")]
    [InlineData("spuriousUnconditionalMask", "Policy gate guard masks must exactly match the coordinate guard masks.")]
    [InlineData("missingLiveGuardMasks", "The policy gate coordinate is missing the required property 'liveGuardMasks'.")]
    [InlineData("missingGateGuardMasks", "The policy gate coordinate is missing the required property 'gateGuardMasks'.")]
    [InlineData("outOfUniverseMask", "Policy gate guard masks must be canonical and reference defined include conditions.")]
    [InlineData("zeroHash", "A policy inventory entry must contain a name and a nonzero requirement fingerprint.")]
    [InlineData("missingInventory", "The policy inventory must exactly cover every policy artifact in the operation plan.")]
    [InlineData("extraInventory", "The policy inventory must exactly cover every policy artifact in the operation plan.")]
    [InlineData("unexpectedGateProperty", "The policy gate contains the unexpected property 'extra'.")]
    [InlineData("duplicateGate", "An operation plan contains duplicate policy gate identities.")]
    [InlineData("orphanExpression", "Every policy expression must be referenced by a policy gate.")]
    [InlineData("tooManyGates", "An operation plan cannot contain more than 64 policy gates.")]
    [InlineData("missingOccurrences", "The policy gate coordinate is missing the required property 'occurrences'.")]
    [InlineData("emptyOccurrences", "A policy gate coordinate must reference at least one compiled occurrence.")]
    [InlineData("inventedOccurrence", "A policy gate coordinate does not match its compiled occurrence.")]
    [InlineData("crossPartOccurrence", "A policy gate coordinate does not match its compiled occurrence.")]
    [InlineData("duplicateOccurrence", "A compiled policy occurrence cannot be claimed by more than one gate coordinate.")]
    public void JsonParser_Should_RejectMalformedPolicyArtifacts(
        string mutation,
        string expectedMessage)
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                "query($a: Boolean!, $b: Boolean!) { product { one: id @include(if: $a) two: id @include(if: $b) } }"));
        var expressions = json["policyExpressions"]!.AsArray();
        var gates = json["policySlots"]!.AsArray();
        var gate = gates[0]!.AsObject();
        var inventory = json["policies"]!.AsArray();

        switch (mutation)
        {
            case "expressionOrdinal":
                expressions[0]!.AsObject()["ordinal"] = 1;
                break;
            case "emptyApplications":
                gate["applications"] = new JsonArray();
                break;
            case "undefinedEnum":
                gate["rmax"] = "Undefined";
                break;
            case "emptyMasks":
                gate["guardMasks"] = new JsonArray();
                break;
            case "noncanonicalMasks":
                gate["guardMasks"] = new JsonArray(2, 1);
                break;
            case "noncanonicalRefs":
                var applications = gate["applications"]!.AsArray();
                var reversed = applications.Select(node => node!.DeepClone()).Reverse().ToArray();
                applications.Clear();
                foreach (var application in reversed)
                {
                    applications.Add(application);
                }
                break;
            case "missingCoordinates":
                gate.Remove("coordinates");
                break;
            case "emptyCoordinates":
                gate["coordinates"] = new JsonArray();
                break;
            case "missingCoordinateApplications":
                gate["coordinates"]!.AsArray()[0]!.AsObject().Remove("applications");
                break;
            case "emptyResponseNames":
                gate["coordinates"]!.AsArray()[0]!.AsObject()["fieldName"] = "product";
                break;
            case "invalidCoordinateRef":
                gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["applications"]!.AsArray()[0]!.AsObject()["expressionOrdinal"] = 99;
                break;
            case "duplicateCoordinate":
                var coordinates = gate["coordinates"]!.AsArray();
                coordinates.Add(coordinates[0]!.DeepClone());
                break;
            case "noncanonicalLiveMasks":
                gate["guardMasks"] = new JsonArray(1, 2);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(2, 1);
                break;
            case "uncoveredGateMask":
                gate["guardMasks"] = new JsonArray(1);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(1);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["gateGuardMasks"] = new JsonArray(2);
                break;
            case "droppedCoordinateApplication":
                gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["applications"]!.AsArray().RemoveAt(0);
                break;
            case "duplicateCoordinateApplication":
                var coordinateApplications = gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["applications"]!.AsArray();
                coordinateApplications.Add(coordinateApplications[0]!.DeepClone());
                break;
            case "orphanSlotMask":
                gate["guardMasks"] = new JsonArray(1, 2);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(1);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["gateGuardMasks"] = new JsonArray(1);
                break;
            case "spuriousUnconditionalMask":
                gate["guardMasks"] = new JsonArray(0);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(1);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["gateGuardMasks"] = new JsonArray(1);
                break;
            case "missingLiveGuardMasks":
                gate["coordinates"]!.AsArray()[0]!.AsObject().Remove("liveGuardMasks");
                break;
            case "missingGateGuardMasks":
                gate["coordinates"]!.AsArray()[0]!.AsObject().Remove("gateGuardMasks");
                break;
            case "outOfUniverseMask":
                gate["guardMasks"] = new JsonArray(4);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(4);
                gate["coordinates"]!.AsArray()[0]!.AsObject()["gateGuardMasks"] = new JsonArray(4);
                break;
            case "zeroHash":
                inventory[0]!.AsObject()["requirementHash"] = 0;
                break;
            case "missingInventory":
                json.Remove("policies");
                break;
            case "extraInventory":
                inventory.Add(new JsonObject
                {
                    ["name"] = "Extra",
                    ["requirementHash"] = 1
                });
                break;
            case "unexpectedGateProperty":
                gate["extra"] = true;
                break;
            case "duplicateGate":
                var duplicate = gate.DeepClone().AsObject();
                duplicate["ordinal"] = 1;
                duplicate["variable"] = "$__fusion_policy_1";
                gates.Add(duplicate);
                break;
            case "orphanExpression":
                expressions.Add(JsonNode.Parse(
                    """{"ordinal":2,"names":[["Orphan"]],"expression":"Orphan"}"""));
                inventory.Add(new JsonObject
                {
                    ["name"] = "Orphan",
                    ["requirementHash"] = PolicyPlanEntry.ComputeRequirementHash(null)
                });
                break;
            case "tooManyGates":
                var template = gate.DeepClone();
                gates.Clear();
                for (var i = 0; i < 65; i++)
                {
                    var item = template.DeepClone().AsObject();
                    item["ordinal"] = i;
                    item["variable"] = $"$__fusion_policy_{i}";
                    gates.Add(item);
                }
                break;
            case "missingOccurrences":
                gate["coordinates"]!.AsArray()[0]!.AsObject().Remove("occurrences");
                break;
            case "emptyOccurrences":
                gate["coordinates"]!.AsArray()[0]!.AsObject()["occurrences"] = new JsonArray();
                break;
            case "inventedOccurrence":
                gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["occurrences"]!.AsArray()[0]!.AsObject()["selectionSetId"] = 999;
                break;
            case "crossPartOccurrence":
                gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["occurrences"]!.AsArray()[0]!.AsObject()["planPart"] = 1;
                break;
            case "duplicateOccurrence":
                var occurrences = gate["coordinates"]!.AsArray()[0]!.AsObject()
                    ["occurrences"]!.AsArray();
                occurrences.Add(occurrences[0]!.DeepClone());
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectFetchGateMaskNotWitnessedByCompiledOperation()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                "query($include: Boolean!) { product { id @include(if: $include) } }"));
        var coordinate = json["policySlots"]!.AsArray()[0]!.AsObject()
            ["coordinates"]!.AsArray()[0]!.AsObject();
        coordinate["gateGuardMasks"] = new JsonArray(1);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy gate coordinate fetch-gate masks do not match its compiled operations.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectFetchGateRemovedFromSerializedOperation()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(schema, "{ product { id } }"));
        json["nodes"]!.AsArray()[0]!.AsObject().Remove("conditions");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A compiled policy occurrence fetch-gate realization does not match its required facet.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectLiveMaskNotWitnessedByCompiledOccurrence()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                "query($include: Boolean!) { secret @include(if: $include) }"));
        var slot = json["policySlots"]!.AsArray()[0]!.AsObject();
        slot["guardMasks"] = new JsonArray(0);
        slot["coordinates"]!.AsArray()[0]!.AsObject()["liveGuardMasks"] = new JsonArray(0);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy gate coordinate liveness masks do not match its compiled occurrences.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectResponseNameNotWitnessedByCompiledOccurrence()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(schema, "{ alias: secret }"));
        json["policySlots"]!.AsArray()[0]!.AsObject()
            ["coordinates"]!.AsArray()[0]!.AsObject()["responseNames"] =
                new JsonArray("invented");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy gate coordinate response names do not match its compiled occurrences.",
            exception.Message);
    }

    [Theory]
    [InlineData("kind", "Undefined", "The policy target kind is invalid.")]
    [InlineData("onDenied", "Undefined", "The policy target denial behavior is invalid.")]
    [InlineData("extra", "true", "The policy target contains the unexpected property 'extra'.")]
    public void JsonParser_Should_RejectMalformedPolicyTarget(
        string property,
        string value,
        string expectedMessage)
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ secret }"));
        var policyNode = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var target = policyNode["targets"]!.AsArray()[0]!.AsObject();
        if (property == "onDenied")
        {
            target["policies"]!.AsArray()[0]!.AsObject()[property] = value;
        }
        else if (property == "extra")
        {
            target[property] = JsonNode.Parse(value);
        }
        else
        {
            target[property] = value;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("missing", "The policy target is missing the required property 'occurrences'.")]
    [InlineData("invented", "A residual policy target does not match its compiled occurrence.")]
    [InlineData("crossPart", "A residual policy target does not match its compiled occurrence.")]
    [InlineData("duplicate", "A compiled residual policy occurrence cannot be claimed more than once in a plan part.")]
    public void JsonParser_Should_RejectInvalidResidualOccurrence(
        string mutation,
        string expectedMessage)
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ secret }"));
        var policyNode = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var targets = policyNode["targets"]!.AsArray();
        var target = targets[0]!.AsObject();

        switch (mutation)
        {
            case "missing":
                target.Remove("occurrences");
                break;
            case "invented":
                target["occurrences"]!.AsArray()[0]!.AsObject()["selectionId"] = 999;
                break;
            case "crossPart":
                target["occurrences"]!.AsArray()[0]!.AsObject()["planPart"] = 1;
                break;
            case "duplicate":
                targets.Add(target.DeepClone());
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("added")]
    [InlineData("removed")]
    [InlineData("reordered")]
    [InlineData("mutated")]
    public void JsonParser_Should_RejectPolicyNodeConditionMutation(string mutation)
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(
                schema,
                "query($include: Boolean!, $skip: Boolean!) { secret @include(if: $include) @skip(if: $skip) }"));
        var policyNode = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var conditions = policyNode["conditions"]!.AsArray();

        switch (mutation)
        {
            case "added":
                conditions.Add(new JsonObject
                {
                    ["variable"] = "invented",
                    ["passingValue"] = true
                });
                break;
            case "removed":
                conditions.RemoveAt(0);
                break;
            case "reordered":
                var first = conditions[0]!.DeepClone();
                conditions[0] = conditions[1]!.DeepClone();
                conditions[1] = first;
                break;
            case "mutated":
                conditions[0]!.AsObject()["passingValue"] =
                    !conditions[0]!.AsObject()["passingValue"]!.GetValue<bool>();
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node conditions do not match its compiled residual targets.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectMovedPolicyNodeBeforeTopologyNormalization()
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ secret }"));
        var nodes = json["nodes"]!.AsArray();
        var policyIndex = nodes
            .Select((node, index) => (Node: node!.AsObject(), Index: index))
            .Single(item => item.Node["type"]!.GetValue<string>() == "Policy")
            .Index;
        var policy = nodes[policyIndex]!.DeepClone();
        nodes.RemoveAt(policyIndex);
        nodes.Insert(0, policy);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy execution node must follow its guarded producer and requirement providers.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectPolicyNodeIdentifierMutationBeforeTopologyNormalization()
    {
        // arrange
        var schema = CreatePolicyNodeWithDependentSchema();
        var plan = PlanOperation(schema, "{ product { name } }");
        var policyNode = Assert.Single(
            plan.AllNodes.OfType<PolicyExecutionNode>(),
            node => node.Dependents.Length > 0);
        var dependent = Assert.Single(policyNode.Dependents.ToArray());
        var (json, parser) = SerializePlan(schema, plan);
        var nodes = json["nodes"]!.AsArray();
        var policy = nodes
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<int>() == policyNode.Id);
        var policyId = policy["id"]!.GetValue<int>();
        policy["id"] = nodes.Max(node => node!["id"]!.GetValue<int>()) + 1;

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            $"Dependency node with ID {policyId} not found for node {dependent.Id}.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectCoordinatedPolicyNodeIdentifierMutation_WhenDependentReferenceIsUpdated()
    {
        // arrange
        var schema = CreatePolicyNodeWithDependentSchema();
        var plan = PlanOperation(schema, "{ product { name } }");
        var policyNode = Assert.Single(
            plan.AllNodes.OfType<PolicyExecutionNode>(),
            node => node.Dependents.Length > 0);
        Assert.Single(policyNode.Dependents.ToArray());
        var (json, parser) = SerializePlan(schema, plan);
        var nodes = json["nodes"]!.AsArray();
        var policy = nodes
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<int>() == policyNode.Id);
        var policyId = policy["id"]!.GetValue<int>();
        var mutatedPolicyId = nodes.Max(node => node!["id"]!.GetValue<int>()) + 1;
        policy["id"] = mutatedPolicyId;

        foreach (var node in nodes.Select(node => node!.AsObject()))
        {
            if (node["dependencies"] is not JsonArray dependencies)
            {
                continue;
            }

            for (var i = 0; i < dependencies.Count; i++)
            {
                if (dependencies[i] is JsonValue dependency
                    && dependency.TryGetValue<int>(out var dependencyId)
                    && dependencyId == policyId)
                {
                    dependencies[i] = mutatedPolicyId;
                }
            }
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal("Policy execution nodes must follow compiled occurrence order.", exception.Message);
    }

    [Fact]
    public void JsonParser_Should_ParsePolicyNodeMovedPastUnrelatedRawNode_WhenTopologyIsUnchanged()
    {
        // arrange
        var schema = CreateTwoProducerRequirementPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ first second }"));
        var nodes = json["nodes"]!.AsArray();
        var policyIndex = nodes
            .Select((node, index) => (Node: node!.AsObject(), Index: index))
            .First(item => item.Node["type"]!.GetValue<string>() == "Policy")
            .Index;
        var unrelatedNodeIndex = nodes
            .Select((node, index) => (Node: node!.AsObject(), Index: index))
            .First(item => item.Index > policyIndex
                && item.Node["type"]!.GetValue<string>() != "Policy")
            .Index;
        var policy = nodes[policyIndex]!.DeepClone();
        nodes.RemoveAt(policyIndex);
        nodes.Insert(unrelatedNodeIndex, policy);

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        Assert.Equal(2, parsedPlan.AllNodes.OfType<PolicyExecutionNode>().Count());
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("removed")]
    [InlineData("extra")]
    [InlineData("reversed")]
    [InlineData("reordered")]
    public void JsonParser_Should_RejectRawPolicyIncidentEdgeMutation(string mutation)
    {
        // arrange
        var schema = CreateRequirementPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ secret }"));
        var nodes = json["nodes"]!.AsArray();
        var policy = nodes
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy");
        var policyId = policy["id"]!.GetValue<int>();
        var dependencies = policy["dependencies"]!.AsArray();
        var producerId = dependencies[0]!.GetValue<int>();
        var producer = nodes
            .Select(node => node!.AsObject())
            .Single(node => node["id"]!.GetValue<int>() == producerId);

        switch (mutation)
        {
            case "duplicate":
                dependencies.Add(producerId);
                break;

            case "removed":
                dependencies.Clear();
                break;

            case "extra":
                dependencies.Add(999);
                break;

            case "reversed":
                dependencies.Clear();
                producer["dependencies"] = new JsonArray(policyId);
                break;

            case "reordered":
                dependencies.Add(policyId);
                var first = dependencies[0]!.DeepClone();
                dependencies[0] = dependencies[1]!.DeepClone();
                dependencies[1] = first;
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        var expectedMessage = mutation switch
        {
            "duplicate" => $"Node {policyId} contains a duplicate dependency identifier.",
            "removed" or "reversed" =>
                "A policy execution node dependencies must exactly match its producer and requirement providers.",
            "extra" => $"Dependency node with ID 999 not found for node {policyId}.",
            "reordered" => $"Node {policyId} dependencies must be in canonical order.",
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("target")]
    [InlineData("source")]
    public void JsonParser_Should_RejectNestedArtifactRemoval_WhenCompiledPolicyFacetIsRequired(
        string artifact)
    {
        // arrange
        var schema = CreateLookupSourceRequirementPolicySchema();
        var (json, parser) = SerializePlan(
            schema,
            PlanOperation(schema, "{ product { details { secret } } }"));
        var node = json["nodes"]!.AsArray()
            .Select(item => item!.AsObject())
            .First(item => item.ContainsKey("operation")
                && item.ContainsKey("target")
                && item.ContainsKey("source"));

        switch (artifact)
        {
            case "operation":
                node["operation"]!.AsObject()["document"] = "query { __typename }";
                break;

            case "target":
                node.Remove("target");
                break;

            case "source":
                node.Remove("source");
                break;
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Contains("policy", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonParser_Should_RejectReorderedCoordinateApplications()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ product { id } }"));
        var applications = json["policySlots"]!.AsArray()[0]!.AsObject()
            ["coordinates"]!.AsArray()[0]!.AsObject()
            ["applications"]!.AsArray();
        var first = applications[0]!.DeepClone();
        applications[0] = applications[1]!.DeepClone();
        applications[1] = first;

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A policy gate coordinate applications do not match declaration order.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectReorderedPolicyCoordinates()
    {
        // arrange
        var schema = CreateLivenessPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ first second }"));
        var coordinates = json["policySlots"]!.AsArray()[0]!.AsObject()
            ["coordinates"]!.AsArray();
        var first = coordinates[0]!.DeepClone();
        coordinates[0] = coordinates[1]!.DeepClone();
        coordinates[1] = first;

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "Policy gate coordinates must follow compiled occurrence order.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectCoordinatedPolicyExpressionRenumbering()
    {
        // arrange
        var schema = CreateRootConditionSlotSchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ product { id } }"));
        var expressions = json["policyExpressions"]!.AsArray();
        var first = expressions[0]!.DeepClone().AsObject();
        var second = expressions[1]!.DeepClone().AsObject();
        first["ordinal"] = 1;
        second["ordinal"] = 0;
        expressions[0] = second;
        expressions[1] = first;

        foreach (var slot in json["policySlots"]!.AsArray())
        {
            var applications = slot!["applications"]!.AsArray();
            foreach (var application in applications)
            {
                var value = application!["expressionOrdinal"]!.GetValue<int>();
                application["expressionOrdinal"] = value == 0 ? 1 : 0;
            }

            var ordered = applications
                .Select(application => application!.DeepClone())
                .OrderBy(application => application!["expressionOrdinal"]!.GetValue<int>())
                .ToArray();
            applications.Clear();
            foreach (var application in ordered)
            {
                applications.Add(application);
            }

            foreach (var application in slot["coordinates"]!.AsArray()
                .SelectMany(coordinate => coordinate!["applications"]!.AsArray()))
            {
                var value = application!["expressionOrdinal"]!.GetValue<int>();
                application["expressionOrdinal"] = value == 0 ? 1 : 0;
            }
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "The policy expression table does not match the compiled policy occurrences.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectCoordinateSplitAcrossPolicySlots()
    {
        // arrange
        var schema = CreateLivenessPolicySchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ first second }"));
        var slots = json["policySlots"]!.AsArray();
        var firstSlot = slots[0]!.AsObject();
        var secondSlot = firstSlot.DeepClone().AsObject();
        var firstCoordinates = firstSlot["coordinates"]!.AsArray();
        var secondCoordinates = secondSlot["coordinates"]!.AsArray();
        firstCoordinates.RemoveAt(1);
        secondCoordinates.RemoveAt(0);
        secondSlot["ordinal"] = 1;
        secondSlot["variable"] = "$__fusion_policy_1";
        slots.Add(secondSlot);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "An operation plan contains duplicate policy gate identities.",
            exception.Message);
    }

    [Theory]
    [InlineData("reordered", "Residual policy targets must follow compiled occurrence order.")]
    [InlineData("merged", "A residual policy target does not match its compiled occurrence.")]
    public void JsonParser_Should_RejectResidualTargetTopologyMutation(
        string mutation,
        string expectedMessage)
    {
        // arrange
        var schema = CreateMultipleResidualTargetSchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ first second }"));
        var targets = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy")
            ["targets"]!.AsArray();

        if (mutation == "reordered")
        {
            var first = targets[0]!.DeepClone();
            targets[0] = targets[1]!.DeepClone();
            targets[1] = first;
        }
        else
        {
            var first = targets[0]!.AsObject();
            var second = targets[1]!.AsObject();
            first["occurrences"]!.AsArray().Add(second["occurrences"]!.AsArray()[0]!.DeepClone());
            first["policies"]!.AsArray().Add(second["policies"]!.AsArray()[0]!.DeepClone());
            first["requirements"]!.AsArray().Add(second["requirements"]!.AsArray()[0]!.DeepClone());
            targets.RemoveAt(1);
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RejectResidualApplicationSplitAcrossTargets()
    {
        // arrange
        var schema = CreateMultipleResidualApplicationSchema();
        var (json, parser) = SerializePlan(schema, PlanOperation(schema, "{ secret }"));
        var targets = json["nodes"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(node => node["type"]!.GetValue<string>() == "Policy")
            ["targets"]!.AsArray();
        var first = targets[0]!.AsObject();
        var second = first.DeepClone().AsObject();
        first["occurrences"]!.AsArray().RemoveAt(1);
        first["policies"]!.AsArray().RemoveAt(1);
        first["requirements"]!.AsArray().RemoveAt(1);
        second["occurrences"]!.AsArray().RemoveAt(0);
        second["policies"]!.AsArray().RemoveAt(0);
        second["requirements"]!.AsArray().RemoveAt(0);
        targets.Add(second);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(Encoding.UTF8.GetBytes(json.ToJsonString())));

        // assert
        Assert.Equal(
            "A residual policy target must claim exactly its compiled occurrence facets.",
            exception.Message);
    }

    [Fact]
    public void PolicyArtifacts_Should_UseContentEqualityAndProduceStablePlanId()
    {
        // arrange
        var schema = CreatePartialFieldPolicySchema();
        var basePlan = PlanOperation(
            schema,
            "query($a: Boolean!, $b: Boolean!) { one: secret @include(if: $a) two: secret @include(if: $b) }");
        var expressionA = Assert.Single(basePlan.PolicyExpressions);
        var expressionB = expressionA with
        {
            Groups = expressionA.Groups
                .Select(group => group.ToImmutableArray())
                .ToImmutableArray()
        };
        var slotA = Assert.Single(basePlan.PolicySlots);
        var slotB = slotA with
        {
            Applications = slotA.Applications.ToImmutableArray(),
            GuardMasks = slotA.GuardMasks.ToImmutableArray(),
            Coordinates = slotA.Coordinates
                .Select(coordinate => coordinate with
                {
                    Occurrences = coordinate.Occurrences.ToImmutableArray(),
                    ResponseNames = coordinate.ResponseNames.ToImmutableArray(),
                    Applications = coordinate.Applications.ToImmutableArray(),
                    LiveGuardMasks = coordinate.LiveGuardMasks.ToImmutableArray(),
                    GateGuardMasks = coordinate.GateGuardMasks.ToImmutableArray()
                })
                .ToImmutableArray()
        };

        // act
        var second = OperationPlan.Create(
            basePlan.Operation,
            basePlan.RootNodes,
            basePlan.AllNodes,
            [],
            [],
            basePlan.IncludeConditions,
            [expressionB],
            [slotB],
            basePlan.Policies,
            0,
            0);

        // assert
        Assert.Equal(expressionA, expressionB);
        Assert.Equal((slotA, slotA.GetHashCode()), (slotB, slotB.GetHashCode()));
        Assert.Equal(basePlan.Id, second.Id);
    }

    private static (JsonObject Json, JsonOperationPlanParser Parser) SerializePlan(
        FusionSchemaDefinition schema,
        OperationPlan plan)
    {
        using var buffer = new PooledArrayWriter();
        new JsonOperationPlanFormatter().Format(buffer, plan);
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        return (JsonNode.Parse(buffer.WrittenSpan)!.AsObject(), new JsonOperationPlanParser(compiler));
    }

    private static (ImmutableArray<IncrementalPlan> IncrementalPlans, ImmutableArray<PolicyConditionSlot> Slots)
        CreateStrictSplitParentAuthorityFixture(
            FusionSchemaDefinition schema,
            OperationPlan plan,
            ImmutableArray<ExecutionNode> firstParentNodes,
            ImmutableArray<ExecutionNode> secondParentNodes,
            bool reversePieces)
    {
        var originalChildPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(originalChildPlan.AllNodes.OfType<PolicyExecutionNode>());
        policyNode.SetTargets(
            policyNode.Targets.ToArray()
                .Select(target => target with
                {
                    Occurrences = target.Occurrences
                        .Select(occurrence => occurrence with { PlanPart = 3 })
                        .ToImmutableArray()
                })
                .ToArray());
        var slots = plan.PolicySlots
            .Select(slot => slot with
            {
                Coordinates = slot.Coordinates
                    .Select(coordinate => coordinate with
                    {
                        Occurrences = coordinate.Occurrences
                            .Select(occurrence => occurrence with { PlanPart = 3 })
                            .ToImmutableArray()
                    })
                    .ToImmutableArray()
            })
            .ToImmutableArray();
        var parentOperation = PlanOperation(schema, "{ product(id: \"1\") { name } }").Operation;
        var outer = new DeliveryGroup("outer", Parent: null, DeferConditionIndex: 0) { Id = 100 };
        var inner = new DeliveryGroup("inner", outer, DeferConditionIndex: 0) { Id = 101 };
        var firstParent = new IncrementalPlan(
            parentOperation,
            firstParentNodes,
            firstParentNodes,
            [outer],
            requirements: []);
        var secondParent = new IncrementalPlan(
            parentOperation,
            secondParentNodes,
            secondParentNodes,
            [outer],
            requirements: []);
        var child = new IncrementalPlan(
            originalChildPlan.Operation,
            originalChildPlan.RootNodes,
            originalChildPlan.AllNodes,
            [inner],
            requirements: []);
        var incrementalPlans = reversePieces
            ? ImmutableArray.Create(secondParent, firstParent, child)
            : ImmutableArray.Create(firstParent, secondParent, child);
        return (incrementalPlans, slots);
    }

    private (FusionSchemaDefinition Schema, OperationPlan Plan) CreateParentAnchoredDeferredPolicyPlan()
    {
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product(id: ID!): Product @lookup
                }

                type Product @key(fields: "id") {
                  id: ID!
                  name: String!
                }
                """,
                """
                # name: b
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  productSku: String!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  reviews(productSku: String! @require(field: "productSku")): [String!]!
                    @policy(names: "CanReadReviews", onDenied: NULL)
                }
                """),
            new TestPolicy(
                "CanReadReviews",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ productSku }")));
        var plan = PlanOperation(
            schema,
            """
            {
              product(id: "1") {
                name
                ... @defer {
                  reviews
                }
              }
            }
            """);

        return (schema, plan);
    }

    private static FusionSchemaDefinition CreateRootConditionSlotSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadQuery", onDenied: ERROR)
              @fusion__policy(names: "CanAudit", onDenied: NULL) {
              product: Product @fusion__field(schema: A)
            }

            type Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanReadQuery"),
            new TestPolicy("CanAudit"));

    private static FusionSchemaDefinition CreateLivenessPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              first: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: NULL)
              second: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: NULL)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanRead"));

    private static FusionSchemaDefinition CreateMultipleResidualTargetSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              role: String @fusion__field(schema: A)
              first: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadFirst", onDenied: ERROR)
              second: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadSecond", onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy(
                "CanReadFirst",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")),
            new TestPolicy(
                "CanReadSecond",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")));

    private static FusionSchemaDefinition CreateTwoProducerRequirementPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  first: String @policy(names: "CanReadFirst")
                  firstRole: String
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  second: String @policy(names: "CanReadSecond")
                  secondRole: String
                }
                """),
            new TestPolicy(
                "CanReadFirst",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ firstRole }")),
            new TestPolicy(
                "CanReadSecond",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ secondRole }")));

    private static FusionSchemaDefinition CreateMultipleResidualApplicationSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              role: String @fusion__field(schema: A)
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: ERROR)
                @fusion__policy(names: "CanAudit", onDenied: ABORT)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy(
                "CanRead",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")),
            new TestPolicy(
                "CanAudit",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")));

    private static FusionSchemaDefinition CreateDeferredLivenessPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              user: User @fusion__field(schema: A)
            }

            type User @fusion__type(schema: A) {
              main: String
                @fusion__field(schema: A)
                @fusion__policy(names: "Main", onDenied: NULL)
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "Deferred", onDenied: NULL)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("Main"),
            new TestPolicy("Deferred"));

    private static FusionSchemaDefinition CreateMixedConcreteAbstractPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  product: Product
                  result: SearchResult
                }

                union SearchResult = Product | Viewer

                type Product @policy(names: "CanReadProduct", onDenied: ABORT) {
                  id: ID!
                }

                type Viewer {
                  name: String!
                }
                """),
            new TestPolicy("CanReadProduct"));

    private static FusionSchemaDefinition CreateDeferredDataPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              user: User @fusion__field(schema: A)
            }

            type User @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "Deferred", onDenied: ERROR)
              role: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy(
                "Deferred",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")));

    private static FusionSchemaDefinition CreateNodePolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query @policy(names: "QueryRoot") {
                  node(id: ID!): Node
                    @lookup
                    @policy(names: "NodeField")
                }

                interface Node {
                  id: ID!
                }

                type Account implements Node @policy(names: "AccountObject") {
                  id: ID!
                  name: String!
                }
                """),
            new TestPolicy("QueryRoot"),
            new TestPolicy("NodeField"),
            new TestPolicy("AccountObject"));

    private static FusionSchemaDefinition CreateAbstractRequirementPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  item: Node
                  nodes: [Node]
                  result: SearchResult
                }

                interface Node {
                  id: ID!
                }

                union SearchResult = Product | Viewer

                type Product implements Node
                  @policy(names: "CanReadProduct", onDenied: ERROR) {
                  id: ID!
                  ownerId: ID!
                }

                type Viewer implements Node {
                  id: ID!
                }
                """),
            new TestPolicy(
                "CanReadProduct",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ ownerId }")));

    private static FusionSchemaDefinition CreateInlineSourceRequirementPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product: Product
                }

                type Product @key(fields: "id") {
                  id: ID!
                  price: Float!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  details(price: Float! @require(field: "price")): Details
                    @policy(names: "FieldPolicy")
                }

                type Details @policy(names: "ObjectPolicy") {
                  secret: String @policy(names: "ChildPolicy")
                }
                """),
            new TestPolicy("FieldPolicy"),
            new TestPolicy("ObjectPolicy"),
            new TestPolicy("ChildPolicy"));

    private static FusionSchemaDefinition CreateLookupSourceRequirementPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  product: Product
                }

                type Product @key(fields: "id") {
                  id: ID!
                }
                """,
                """
                # name: c
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  rating: Int!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  details(rating: Int! @require(field: "rating")): Details
                    @policy(names: "FieldPolicy")
                }

                type Details @policy(names: "ObjectPolicy") {
                  secret: String @policy(names: "ChildPolicy")
                }
                """),
            new TestPolicy("FieldPolicy"),
            new TestPolicy("ObjectPolicy"),
            new TestPolicy("ChildPolicy"));

    private static FusionSchemaDefinition CreateCrossSchemaRequirementFeedPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  product: Product
                }

                type Product @key(fields: "id") @policy(names: "CanReadProduct") {
                  id: ID!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  name: String @policy(names: "CanReadName")
                }
                """),
            new TestPolicy("CanReadProduct"),
            new TestPolicy(
                "CanReadName",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreatePolicyNodeWithDependentSchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  product: Product
                }

                type Product @key(fields: "id") @policy(names: "CanReadProduct") {
                  id: ID!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product {
                  id: ID!
                  name: String @policy(names: "CanReadName")
                }
                """),
            new TestPolicy(
                "CanReadProduct",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            new TestPolicy(
                "CanReadName",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreatePartialFieldPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              public: String @fusion__field(schema: A)
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadSecret", onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanReadSecret"));

    private static FusionSchemaDefinition CreateReturnedObjectPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadProduct", onDenied: NULL) {
              id: ID! @fusion__field(schema: A)
              name: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanReadProduct"));

    private static FusionSchemaDefinition CreateMixedApplicationSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
              public: String @fusion__field(schema: A)
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: [["CanRequest", "CanResource"]], onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanRequest"),
            new TestPolicy(
                "CanResource",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreateTautologicalMixedApplicationSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
              gated: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRequest", onDenied: ERROR)
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(
                  names: [["CanRequest", "CanResource"], ["CanResource"]]
                  onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanRequest"),
            new TestPolicy(
                "CanResource",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreateRequirementFeedSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadProduct") {
              id: ID! @fusion__field(schema: A)
              name: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadName")
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanReadProduct"),
            new TestPolicy(
                "CanReadName",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreateProtectedRequirementFeedSchema(
        string requirements,
        params IPolicy[] feedPolicies)
    {
        var policies = new IPolicy[feedPolicies.Length + 1];
        policies[0] = new TestPolicy(
            "CanReadProduct",
            Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements));
        feedPolicies.CopyTo(policies, 1);

        return CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadProduct") {
              id: ID
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadId")
              name: String @fusion__field(schema: A)
              details: Details @fusion__field(schema: A)
            }

            type Details @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadSecret")
              public: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            policies);
    }

    private static FusionSchemaDefinition CreateMixedResidualCoordinateSchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(names: "CanAudit", onDenied: NULL)
              @fusion__policy(names: "CanReadQueryData", onDenied: ABORT) {
              product: Product @fusion__field(schema: A)
            }

            type Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanAudit"),
            new TestPolicy(
                "CanReadQueryData",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ product { id } }")));

    private static FusionSchemaDefinition CreateManyFieldPoliciesSchema(int count)
    {
        var names = Enumerable.Range(0, count)
            .Select(i => $"CanReadField{i}")
            .ToArray();

        var fieldLines = string.Join(
            "\n",
            names.Select((name, i) =>
                $"  field{i}: String @fusion__field(schema: A) "
                + $"@fusion__policy(names: \"{name}\", onDenied: NULL)"));

        var schemaText = $$"""
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
            {{fieldLines}}
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """;

        return CreateSchema(
            schemaText,
            [.. names.Select(name => new TestPolicy(name))]);
    }

    private static FusionSchemaDefinition CreateCapacityIdentitySchema()
    {
        var fields = new StringBuilder("  id: ID! @fusion__field(schema: A)\n");
        var policies = new List<IPolicy>();

        for (var i = 0; i < 64; i++)
        {
            fields.AppendLine(
                $"  mixed{i}: String @fusion__field(schema: A) "
                    + $"@fusion__policy(names: [[\"Request{i}\", \"Resource{i}\"]], onDenied: NULL)");
            policies.Add(new TestPolicy($"Request{i}"));
            policies.Add(new TestPolicy(
                $"Resource{i}",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        }

        fields.AppendLine(
            "  lateAllocated: String @fusion__field(schema: A) "
                + "@fusion__policy(names: \"Request0\", onDenied: NULL)");
        fields.AppendLine(
            "  lateUnallocated: String @fusion__field(schema: A) "
                + "@fusion__policy(names: \"LateRequest\", onDenied: NULL)");
        policies.Add(new TestPolicy("LateRequest"));

        return CreateSchema(
            $$"""
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
            {{fields}}
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            [.. policies]);
    }

    private static FusionSchemaDefinition CreatePolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadQuery", onDenied: ERROR) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadProductField", onDenied: ABORT)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadProductObject") {
              id: ID! @fusion__field(schema: A)
              name: String
                @fusion__field(schema: A)
                @fusion__policy(names: [["CanReadName", "CanAudit"], ["CanAdmin"]], onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy("CanReadQuery"),
            new TestPolicy("CanReadProductField"),
            new TestPolicy("CanReadProductObject"),
            new TestPolicy("CanReadName"),
            new TestPolicy("CanAudit"),
            new TestPolicy("CanAdmin"));

    private static FusionSchemaDefinition CreateRequirementPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadSecret")
              role: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestPolicy(
                "CanReadSecret",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")));

    private static FusionSchemaDefinition CreateBatchedPolicySchema()
        => CreateSchema(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  first: Product
                  second: Product
                }

                type Product @key(fields: "id") {
                  id: ID!
                }
                """,
                """
                # name: b
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID! @is(field: "id")): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  rating: Int! @policy(names: "CanReadRating")
                }
                """),
            new TestPolicy(
                "CanReadRating",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

    private static FusionSchemaDefinition CreateSchema(
        string schemaText,
        params IPolicy[] policies)
        => CreateSchema(Utf8GraphQLParser.Parse(schemaText), policies);

    private static FusionSchemaDefinition CreateSchema(
        DocumentNode schemaDocument,
        params IPolicy[] policies)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(policies))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(schemaDocument, services);
    }
}
