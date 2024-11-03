using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.Spec;

public class InputObjectsCanBeVariablesTest
{
    // http://facebook.github.io/graphql/draft/#sec-All-Variable-Usages-are-Allowed
    [Fact]
    public async Task EnsureInputObjectsCanBeVariablesTest()
    {
        await ExpectValid(
                """
                query ($a: String! $b: String!) {
                    anything(foo: {
                        a: $a
                        b: $b
                    }) {
                        a
                        b
                    }
                }
                """,
            r => r.AddQueryType<Query>(),
            r => r.SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "a", "a" },
                        { "b", "b" },
                    }))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task EnsureInputObjectsCanBeVariablesAndLiteralsTest()
    {
        await ExpectValid(
            """
            query ($a: String!) {
                anything(foo: {
                    a: $a
                    b: "b"
                }) {
                    a
                    b
                }
            }
            """,
            r => r.AddQueryType<Query>(),
            r => r.SetVariableValues(new Dictionary<string, object?> { { "a", "a" }, }))
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task EnsureInputObjectsCanBeLiteralsTest()
    {
        await ExpectValid(
            @"
                    {
                        anything(foo: {
                            a: ""a""
                            b: ""b""
                        }) {
                            a
                            b
                        }
                    }
                ",
            r => r.AddQueryType<Query>(),
            r => { }
        ).MatchSnapshotAsync();
    }

    public class Query
    {
        public Foo Anything(Foo foo)
        {
            return foo;
        }
    }

    public class Foo(string a, string b)
    {
        public string A { get; set; } = a;
        public string B { get; set; } = b;
    }
}
