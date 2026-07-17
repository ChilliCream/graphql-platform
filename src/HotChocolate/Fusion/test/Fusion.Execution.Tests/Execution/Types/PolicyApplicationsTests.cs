using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class PolicyApplicationsTests : FusionTestBase
{
    [Fact]
    public void Create_Should_SetHasPoliciesAndStoreApplications_When_ObjectAndFieldHavePolicies()
    {
        var schema = CreateSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadQuery", onDenied: ERROR) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadProduct", onDenied: ABORT)
            }

            type Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            "CanReadQuery",
            "CanReadProduct");

        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var product = query.Fields["product"];

        Format(schema, query, product).MatchInlineSnapshot(
            """
            HasPolicies: True
            Type: Query
              CanReadQuery: Error
            Field: Query.product
              CanReadProduct: Abort
            """);
    }

    [Fact]
    public void Create_Should_PreserveAllApplications_When_CoordinateHasMultiplePolicies()
    {
        var schema = CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead")
                @fusion__policy(names: [["CanAudit", "CanRead"], "CanAbort"], onDenied: ERROR)
                @fusion__policy(names: "CanAbort", onDenied: ABORT)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            "CanRead",
            "CanAudit",
            "CanAbort");

        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var field = query.Fields["field"];

        Format(schema, query, field).MatchInlineSnapshot(
            """
            HasPolicies: True
            Type: Query
              <empty>
            Field: Query.field
              CanRead: Null
              CanAbort OR (CanAudit AND CanRead): Error
              CanAbort: Abort
            """);
    }

    [Fact]
    public void Create_Should_LeaveApplicationsEmpty_When_SchemaHasNoPolicies()
    {
        var schema = CreateCompositeSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var field = query.Fields["field"];

        Format(schema, query, field).MatchInlineSnapshot(
            """
            HasPolicies: False
            Type: Query
              <empty>
            Field: Query.field
              <empty>
            """);
    }

    [Theory]
    [InlineData(
        "@fusion__policy",
        "The `names` argument is required on the @fusion__policy directive.")]
    [InlineData(
        "@fusion__policy(names: [])",
        "The `names` argument on @fusion__policy must contain at least one policy name group.")]
    [InlineData(
        "@fusion__policy(names: [[]])",
        "A policy name group on @fusion__policy must contain at least one policy name.")]
    [InlineData(
        "@fusion__policy(names: [[1]])",
        "A policy name on @fusion__policy must be a string.")]
    [InlineData(
        "@fusion__policy(names: [1])",
        "A policy name group on @fusion__policy must be a string or a list of strings.")]
    [InlineData(
        "@fusion__policy(names: 1)",
        "The `names` argument on @fusion__policy must be a string or a list of policy name groups.")]
    public void Create_Should_Throw_When_PolicyNamesArgumentIsMalformed(
        string policyDirective,
        string expectedMessage)
    {
        // arrange
        var schemaText =
            $$"""
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String @fusion__field(schema: A) {{policyDirective}}
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """;

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(schemaText)));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Equals_Should_UseValueSemantics_When_GroupsContainSameNames()
    {
        // arrange
        var first = new PolicyApplication
        {
            Groups = [["a", "b"], ["c"]],
            OnDenied = PolicyDenialBehavior.Error
        };
        var second = new PolicyApplication
        {
            Groups = [["a", "b"], ["c"]],
            OnDenied = PolicyDenialBehavior.Error
        };
        var third = new PolicyApplication
        {
            Groups = [["a", "b"]],
            OnDenied = PolicyDenialBehavior.Error
        };

        // act
        var equalByValue = first.Equals(second);
        var equalHashCodes = first.GetHashCode() == second.GetHashCode();
        var equalToDifferentGroups = first.Equals(third);

        // assert
        Assert.True(equalByValue);
        Assert.True(equalHashCodes);
        Assert.False(equalToDifferentGroups);
    }

    private static string Format(
        FusionSchemaDefinition schema,
        FusionObjectTypeDefinition type,
        FusionOutputFieldDefinition field)
    {
        var builder = new StringBuilder();
        builder.Append("HasPolicies: ");
        builder.AppendLine(schema.HasPolicies.ToString());
        AppendPolicyApplications(builder, $"Type: {type.Name}", type.PolicyApplications);
        AppendPolicyApplications(builder, $"Field: {type.Name}.{field.Name}", field.PolicyApplications);

        return builder.ToString().TrimEnd();
    }

    private static FusionSchemaDefinition CreateSchema(
        string schemaText,
        params string[] policyNames)
    {
        var policies = policyNames
            .Select(static name => (IAuthorizationPolicy)new TestAuthorizationPolicy(name))
            .ToArray();
        var services = new ServiceCollection()
            .AddSingleton<IAuthorizationPolicyProvider>(
                _ => new TestAuthorizationPolicyProvider(policies))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(schemaText), services);
    }

    private static void AppendPolicyApplications(
        StringBuilder builder,
        string title,
        ImmutableArray<PolicyApplication> applications)
    {
        builder.AppendLine(title);

        if (applications.IsDefaultOrEmpty)
        {
            builder.AppendLine("  <empty>");
            return;
        }

        foreach (var application in applications)
        {
            builder.Append("  ");
            builder.Append(application.Format());
            builder.Append(": ");
            builder.AppendLine(application.OnDenied.ToString());
        }
    }
}
