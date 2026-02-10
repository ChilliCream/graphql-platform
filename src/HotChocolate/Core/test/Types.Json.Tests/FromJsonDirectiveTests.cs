using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class FromJsonDirectiveTests
{
    [Fact]
    public async Task MapField()
    {
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
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    foo: Foo
                }

                type Foo {
                    baz: String @fromJson(name: "bar")
                }
                """)
            .BindRuntimeType<Query>()
            .AddJsonSupport()
            .ExecuteRequestAsync("{ foo { baz } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MapField_AutomaticScalars()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                type Query {
                    foo: Foo
                }

                type Foo {
                    base64String: Base64String @fromJson
                    boolean: Boolean @fromJson
                    byte: Byte @fromJson
                    byteArray: ByteArray @fromJson
                    date: Date @fromJson
                    dateTime: DateTime @fromJson
                    decimal: Decimal @fromJson
                    float: Float @fromJson
                    id: ID @fromJson
                    int: Int @fromJson
                    long: Long @fromJson
                    short: Short @fromJson
                    string: String @fromJson
                    url: URL @fromJson
                    uuid: UUID @fromJson
                }
                """)
            .AddResolver("Query", "foo", _ => JsonDocument.Parse(
                """
                {
                    "base64String": "Zm9v",
                    "boolean": true,
                    "byte": 1,
                    "byteArray": "Zm9v",
                    "date": "1979-12-20",
                    "dateTime": "1979-12-20T15:00Z",
                    "decimal": 3.4,
                    "float": 1.2,
                    "id": "id",
                    "int": 2,
                    "long": 3,
                    "short": 1,
                    "string": "string",
                    "url": "https://abc",
                    "uuid":"2d25e877-aecc-4a9e-a191-cf75def49e42"
                }
                """).RootElement)
            .AddJsonSupport()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        base64String
                        boolean
                        byte
                        byteArray
                        date
                        dateTime
                        decimal
                        float
                        id
                        int
                        long
                        short
                        string
                        url
                        uuid
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": ""abc"" }").RootElement;
    }
}
