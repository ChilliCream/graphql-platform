using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class Issue8756ReproTests
{
    [Fact]
    public async Task Enum_Value_Is_Parsed_In_Reference_Resolver_When_Representation_Uses_String()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Issue8756Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        var representations = new List<Representation>
        {
            new(
                nameof(Issue8756Entity),
                new ObjectValueNode(
                    new ObjectFieldNode("id", "1"),
                    new ObjectFieldNode("enumValue", "B")))
        };

        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);

        var entity = Assert.IsType<Issue8756Entity>(result[0]);
        Assert.Equal(Issue8756Enum.B, entity.EnumValue);
    }

    public class Issue8756Query
    {
        public Issue8756Entity Issue8756Entity { get; set; } = null!;
    }

    [ReferenceResolver(EntityResolver = nameof(Resolve))]
    public class Issue8756Entity
    {
        public string Id { get; set; } = null!;

        public Issue8756Enum EnumValue { get; set; }

        public static Issue8756Entity Resolve(string id, Issue8756Enum enumValue)
            => new()
            {
                Id = id,
                EnumValue = enumValue
            };
    }

    public enum Issue8756Enum
    {
        A,
        B
    }
}
