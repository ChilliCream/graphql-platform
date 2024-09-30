using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.Spec;

public class ListTypeTests
{
    // http://facebook.github.io/graphql/draft/#sec-All-Variable-Usages-are-Allowed
    [Fact]
    public async Task Ensure_List_Elements_Can_Be_Variables()
    {
        await ExpectValid(
                """
                query ($a: String! $b: String!) {
                    list(items: [$a $b])
                }
                """,
                b => b.AddQueryType<Query>(),
                r => r.SetVariableValues(new Dictionary<string, object?> { {"a", "a" }, {"b", "b" }, }))
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public List<string> GetList(List<string> items) => items;
    }
}
