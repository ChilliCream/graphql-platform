using HotChocolate.Execution.Configuration;
using HotChocolate.StarWars.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.StarWars;

public static class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddStarWarsTypes(
        this ISchemaBuilder builder)
    {
        return builder
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddSubscriptionType<SubscriptionType>()
            .AddType<HumanType>()
            .AddType<DroidType>()
            .AddType<EpisodeType>();
    }

    public static IRequestExecutorBuilder AddStarWarsTypes(
        this IRequestExecutorBuilder builder) =>
        builder.ConfigureSchema(b => b.AddStarWarsTypes());

    public static IRequestExecutorBuilder AddStarWars(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .AddStarWarsRepositories()
            .AddStarWarsTypes();
    }
}
