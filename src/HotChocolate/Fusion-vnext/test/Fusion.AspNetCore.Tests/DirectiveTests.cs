using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class DirectiveTests : FusionTestBase
{
    [Fact]
    public async Task Custom_Directive_Is_Composed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            directive @customAuth(
              role: String!
              permission: String = "read"
            ) on FIELD_DEFINITION | OBJECT

            type Query {
              publicField: String
              protectedField: String @customAuth(role: "admin", permission: "write")
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            directive @customAuth(
              role: String!
              permission: String = "read"
            ) on FIELD_DEFINITION | OBJECT

            type Query {
              otherField: String @customAuth(role: "user")
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new Transport.OperationRequest(
            """
            query CustomDirectiveIntrospection {
              __schema {
                directives {
                  name
                  description
                  locations
                  args {
                    name
                    description
                    defaultValue
                    type {
                      kind
                      name
                      ofType {
                        kind
                        name
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Can_Execute_Operation_With_Custom_Directive_On_Field()
    {
        // arrange: define a custom directive usable in operations (FIELD location)
        using var server = CreateSourceSchema(
            "A",
            """
            directive @flag(note: String) on FIELD
            type Query { a: String b: String }
            """);

        using var gateway = await CreateCompositeSchemaAsync([
            ("A", server)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new Transport.OperationRequest(
            """
            query CustomDirectiveUsage {
              first: a @flag(note: "one")
              second: b @flag
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}
