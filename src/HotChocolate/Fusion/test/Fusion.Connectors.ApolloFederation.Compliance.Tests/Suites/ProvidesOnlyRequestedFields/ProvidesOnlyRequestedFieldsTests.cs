using HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphB;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Explicit overlay of graphql-hive/federation-gateway-audit PR #335 at
/// c785b8ba31d4e087cdf90cc00b4fef949f585aa8. The PR is not part of the pinned
/// official audit revision, so this suite deliberately remains outside the
/// <see cref="OfficialV2SuiteAttribute"/> inventory. The explicit
/// <c>punishForPoorPlans: false</c> arguments mirror the PR's checked-in default;
/// strict-mode evidence is tracked separately from this official overlay.
/// </summary>
public sealed class ProvidesOnlyRequestedFieldsTests : ComplianceTestBase
{
    private const string DisableBatchingSettings =
        """
        {
          "transports": {
            "http": {
              "capabilities": {
                "batching": {
                  "variableBatching": false,
                  "requestBatching": false
                }
              }
            }
          }
        }
        """;

    private static readonly Pr335Case[] s_cases =
    [
        new Pr335Case(
            "provides-only-requested-fields/000",
            """
            query {
              entity {
                id
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/001",
            """
            query {
              entity {
                id
                name
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "name": "Entity One"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/002",
            """
            query {
              entity {
                id
                displayName: name
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "displayName": "Entity One"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/003",
            """
            query {
              entity {
                id
                ... on Entity {
                  name
                }
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "name": "Entity One"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/004",
            """
            query {
              entity {
                id
                ...EntityName
              }
            }

            fragment EntityName on Entity {
              name
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "name": "Entity One"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/005",
            """
            query {
              entity {
                id
                name
                extra
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "name": "Entity One",
                "extra": "Extra One"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/006",
            """
            query {
              entities {
                id
                name
              }
            }
            """,
            """
            {
              "entities": [
                {
                  "id": "e1",
                  "name": "Entity One"
                },
                {
                  "id": "e2",
                  "name": "Entity Two"
                }
              ]
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/007",
            """
            query {
              entities {
                id
                name
                extra
              }
            }
            """,
            """
            {
              "entities": [
                {
                  "id": "e1",
                  "name": "Entity One",
                  "extra": "Extra One"
                },
                {
                  "id": "e2",
                  "name": "Entity Two",
                  "extra": "Extra Two"
                }
              ]
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/008",
            """
            query {
              entity {
                id
                ... on Entity @skip(if: true) {
                  description
                }
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/009",
            """
            query {
              entity {
                id
                ... on Entity @include(if: false) {
                  description
                }
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1"
              }
            }
            """),
        new Pr335Case(
            "provides-only-requested-fields/010",
            """
            query {
              entity {
                id
                ... {
                  name
                }
                ... {
                  extra
                }
              }
            }
            """,
            """
            {
              "entity": {
                "id": "e1",
                "name": "Entity One",
                "extra": "Extra One"
              }
            }
            """)
    ];

    private static readonly OfficialAuditSuite s_suite = CreateSuite();

    public static TheoryData<string> Cases
    {
        get
        {
            var cases = new TheoryData<string>();

            foreach (var testCase in s_cases)
            {
                cases.Add(testCase.Id);
            }

            return cases;
        }
    }

    protected override Task<FusionGateway> BuildGatewayAsync()
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SubgraphASubgraph.Name] = DisableBatchingSettings,
            [SubgraphBSubgraph.Name] = DisableBatchingSettings
        };

        return FusionGatewayBuilder.ComposeAsync(
            capture: null,
            settings,
            (SubgraphASubgraph.Name, () => SubgraphASubgraph.BuildAsync(
                punishForPoorPlans: false)),
            (SubgraphBSubgraph.Name, () => SubgraphBSubgraph.BuildAsync(
                punishForPoorPlans: false)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "Pr335Overlay")]
    public Task Pr335Case_Should_MatchExpectedResult_When_Executed(string caseId)
        => OfficialAuditSuiteRun<ProvidesOnlyRequestedFieldsTests>.AssertCaseAsync(
            caseId,
            BuildGatewayAsync,
            static () => s_suite);

    private static OfficialAuditSuite CreateSuite()
        => new(
            "provides-only-requested-fields",
            s_cases
                .Select(testCase => new AuditTestCase(
                    testCase.Id,
                    testCase.Query,
                    Variables: null,
                    HasExpectedData: true,
                    testCase.ExpectedData,
                    HasExpectedErrors: false,
                    ExpectsErrors: null))
                .ToArray(),
            Sources: [],
            FixtureModules: [],
            V1Sources: []);

    private sealed record Pr335Case(string Id, string Query, string ExpectedData)
    {
        public override string ToString() => Id;
    }
}
