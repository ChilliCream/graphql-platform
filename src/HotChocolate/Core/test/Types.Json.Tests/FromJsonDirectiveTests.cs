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

    [Fact]
    public async Task MapField_AutomaticScalars()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                type Query {
                    foo: Foo
                }

                type Foo {
                    string: String @fromJson
                    id: ID @fromJson
                    boolean: Boolean @fromJson
                    short: Short @fromJson
                    int: Int @fromJson
                    long: Long @fromJson
                    float: Float @fromJson
                    decimal: Decimal @fromJson
                    url: URL @fromJson
                    uuid: UUID @fromJson
                    byte: Byte @fromJson
                    byteArray: ByteArray @fromJson
                    date: Date @fromJson
                    dateTime: DateTime @fromJson
                }
                ")
            .AddResolver("Query", "foo", _ => JsonDocument.Parse(
                @"{
                    ""string"": ""string"",
                    ""id"": ""id"",
                    ""boolean"": true,
                    ""short"": 1,
                    ""int"": 2,
                    ""long"": 3,
                    ""float"": 1.2,
                    ""decimal"": 3.4,
                    ""url"": ""http://abc"",
                    ""uuid"":""2d25e877-aecc-4a9e-a191-cf75def49e42"",
                    ""byte"": 1,
                    ""byteArray"": ""Zm9v"",
                    ""date"": ""1979-12-20"",
                    ""dateTime"": ""1979-12-20T15:00Z""
                }").RootElement)
            .AddJsonSupport()
            .ExecuteRequestAsync(
                @"{
                    foo {
                        string
                        id
                        boolean
                        short
                        int
                        long
                        float
                        decimal
                        url
                        uuid
                        byte
                        byteArray
                        date
                        dateTime
                    }
                }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": ""abc"" }").RootElement;
    }
}
