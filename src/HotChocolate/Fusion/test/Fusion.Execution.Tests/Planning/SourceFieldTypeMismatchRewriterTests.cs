using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SourceFieldTypeMismatchRewriterTests
{
    [Fact]
    public void Rewrite_Should_AliasField_When_SourceLeafTypeNamesDiffer()
    {
        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                """
                schema {
                  query: Query
                }

                type Query @fusion__type(schema: A) {
                  accounts: [Account!]! @fusion__field(schema: A)
                }

                union Account
                  @fusion__type(schema: A)
                  @fusion__unionMember(schema: A, member: "User")
                  @fusion__unionMember(schema: A, member: "Admin")
                  = User | Admin

                type User @fusion__type(schema: A) {
                  id: String! @fusion__field(schema: A, sourceType: "UserId!")
                }

                type Admin @fusion__type(schema: A) {
                  id: String! @fusion__field(schema: A, sourceType: "AdminId!")
                }

                enum fusion__Schema {
                  A
                }
                """));
        var operation = Utf8GraphQLParser.Parse(
                """
                {
                  accounts {
                    ... on User { id }
                    ... on Admin { id }
                  }
                }
                """)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        var result = SourceFieldTypeMismatchRewriter.Rewrite(
            operation,
            schema.QueryType,
            "A",
            schema);

        Assert.Equal(
            "{ accounts { ... on User { id } ... on Admin { fusion__field_1: id } } }",
            result.Operation.ToString(indented: false));
    }

    [Fact]
    public void RewriteDynamic_Should_AliasField_When_AnySourceLeafTypeNamesDiffer()
    {
        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                """
                schema {
                  query: Query
                }

                type Query @fusion__type(schema: A) @fusion__type(schema: B) {
                  accounts: [Account!]!
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                }

                union Account
                  @fusion__type(schema: A)
                  @fusion__type(schema: B)
                  @fusion__unionMember(schema: A, member: "User")
                  @fusion__unionMember(schema: A, member: "Admin")
                  @fusion__unionMember(schema: B, member: "User")
                  @fusion__unionMember(schema: B, member: "Admin")
                  = User | Admin

                type User @fusion__type(schema: A) @fusion__type(schema: B) {
                  id: String!
                    @fusion__field(schema: A, sourceType: "UserId!")
                    @fusion__field(schema: B, sourceType: "UserId!")
                }

                type Admin @fusion__type(schema: A) @fusion__type(schema: B) {
                  id: String!
                    @fusion__field(schema: A, sourceType: "UserId!")
                    @fusion__field(schema: B, sourceType: "AdminId!")
                }

                enum fusion__Schema {
                  A
                  B
                }
                """));
        var operation = Utf8GraphQLParser.Parse(
                """
                {
                  accounts {
                    ... on User { id }
                    ... on Admin { id }
                  }
                }
                """)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        var result = SourceFieldTypeMismatchRewriter.RewriteDynamic(
            operation,
            schema.QueryType,
            schema);

        Assert.Equal(
            "{ accounts { ... on User { id } ... on Admin { fusion__field_1: id } } }",
            result.Operation.ToString(indented: false));
    }
}
