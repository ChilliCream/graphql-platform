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
              @fusion__policy(name: "CanReadQuery", onDenied: ERROR) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(name: "CanReadProduct", onDenied: ABORT)
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
                @fusion__policy(name: "CanRead")
                @fusion__policy(name: "CanAudit", onDenied: ERROR)
                @fusion__policy(name: "CanAbort", onDenied: ABORT)
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
              CanAudit: Error
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
            builder.Append(application.Name);
            builder.Append(": ");
            builder.AppendLine(application.OnDenied.ToString());
        }
    }
}
