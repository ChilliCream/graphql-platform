using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Json.Tests;

public class FromJsonDirectiveTests
{
    [Fact]
    public async Task MapField()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                type Query {
                    foo: Foo
                }

                type Foo {
                    bar: String @fromJson
                }
                ")
            .BindRuntimeType<Query>()
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { bar } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_With_Name()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                type Query {
                    foo: Foo
                }

                type Foo {
                    baz: String @fromJson(name: ""bar"")
                }
                ")
            .BindRuntimeType<Query>()
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { baz } }")
            .MatchSnapshotAsync();
    }


    public class Query
    {
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": ""abc"" }").RootElement;
    }
}
