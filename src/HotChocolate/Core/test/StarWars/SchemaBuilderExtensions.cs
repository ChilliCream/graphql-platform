using HotChocolate.StarWars.Types;

namespace HotChocolate.StarWars
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddStarWarsTypes(
            this ISchemaBuilder schemaBuilder)
        {
            return schemaBuilder
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>();
        }
    }
}
