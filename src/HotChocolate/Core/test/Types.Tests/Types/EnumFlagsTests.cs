using System;
using System.Threading.Tasks;
using CookieCrumble;
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
            
            "The @tag directive is used to apply arbitrary string\nmetadata to a schema location. Custom tooling can use\nthis metadata during any step of the schema delivery flow,\nincluding composition, static analysis, and documentation.\n            \n\ninterface Book {\n  id: ID! @tag(name: \"your-value\")\n  title: String!\n  author: String!\n}"
            directive @tag("The name of the tag." name: String!) repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
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
        Baz = 4
    }
}
