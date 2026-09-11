using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Planning;

public sealed class PolicyPlanTraceFormatterTests : FusionTestBase
{
    [Fact]
    public void Format_Should_ListStaticPolicyConditions_When_TraceIsProvided()
    {
        // arrange
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => new TestPolicyProvider(new TestPolicy("CanReadSecret")))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  secret: String @policy(names: "CanReadSecret")
                }
                """),
            services);
        var plan = PlanOperation(schema, "{ secret }");
        var trace = new OperationPlanTrace { Duration = TimeSpan.Zero };

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan, trace);
        var json = new JsonOperationPlanFormatter().Format(plan, trace);

        // assert
        yaml.MatchSnapshot(extension: ".yaml");
        json.MatchSnapshot(extension: ".json");
    }
}
