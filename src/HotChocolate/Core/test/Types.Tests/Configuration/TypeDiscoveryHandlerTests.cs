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
