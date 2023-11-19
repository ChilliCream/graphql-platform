using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Configuration;

public class TypeDiscoveryHandlerTests
{
    // https://github.com/ChilliCream/graphql-platform/issues/5942
    [Fact]
    public async Task Ensure_Inputs_Are_Not_Used_As_Outputs()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<Foo>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }
            
            type Query {
              foo(foo: TestMeInput): TestMe
            }
            
            type TestMe {
              bar: String
            }
            
            input TestMeInput {
              bar: String
            }
            
            "The @tag directive is used to apply arbitrary string\nmetadata to a schema location. Custom tooling can use\nthis metadata during any step of the schema delivery flow,\nincluding composition, static analysis, and documentation.\n            \n\ninterface Book {\n  id: ID! @tag(name: \"your-value\")\n  title: String!\n  author: String!\n}"
            directive @tag("The name of the tag." name: String!) repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            """);
    }

    public class Query
    {
        public Foo GetFoo(Foo foo) => foo;
    }

    [GraphQLName("TestMe")]
    public class Foo
    {
        public string Bar { get; set; }
    }
}
