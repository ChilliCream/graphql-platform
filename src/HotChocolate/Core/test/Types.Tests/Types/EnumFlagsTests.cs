using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class EnumFlagsTests
{
    [Fact]
    public async Task Execute_Request_With_Flags()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableFlagEnums = true)
                .ExecuteRequestAsync(
                    """
                    {
                        foo(input: { isFoo: true, isBaz: true }) {
                            isFoo
                            isBar
                            isBaz
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "isFoo": true,
                  "isBar": false,
                  "isBaz": true
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Schema_With_Flags()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o =>
                {
                    o.EnableFlagEnums = true;
                    o.EnableDefer = false;
                    o.EnableStream = false;
                })
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type FooBarBazFlags {
              isFoo: Boolean!
              isBar: Boolean!
              isBaz: Boolean!
            }

            type Query {
              foo(input: FooBarBazFlagsInput!): FooBarBazFlags!
            }

            input FooBarBazFlagsInput {
              isFoo: Boolean
              isBar: Boolean
              isBaz: Boolean
            }
            """);
    }

    public class Query
    {
        public FooBarBaz GetFoo(FooBarBaz input)
            => input;
    }

    [Flags]
    public enum FooBarBaz
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }
}
