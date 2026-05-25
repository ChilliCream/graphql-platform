using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue6970ReproTests
{
    [Fact]
    public async Task Schema_With_IDictionary_String_Object_Output_Field_Builds()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public sealed class Query
    {
        public PageVOAllergyIntolerance Item => new();
    }

    public sealed class PageVOAllergyIntolerance
    {
        [GraphQLType<NonNullType<AnyType>>]
        public IDictionary<string, object> Extension { get; } =
            new Dictionary<string, object>
            {
                ["foo"] = "bar"
            };
    }
}
