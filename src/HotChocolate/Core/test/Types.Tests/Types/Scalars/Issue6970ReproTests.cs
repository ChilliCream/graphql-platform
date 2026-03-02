using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue6970ReproTests
{
    [Fact]
    public async Task Schema_With_IDictionary_String_Object_Output_Field_Builds()
    {
        var exception = await Record.ExceptionAsync(async () =>
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .BuildSchemaAsync());

        Assert.Null(exception);
    }

    public sealed class Query
    {
        public PageVOAllergyIntolerance Item => new();
    }

    public sealed class PageVOAllergyIntolerance
    {
        public IDictionary<string, object> Extension { get; } =
            new Dictionary<string, object>
            {
                ["foo"] = "bar"
            };
    }
}
