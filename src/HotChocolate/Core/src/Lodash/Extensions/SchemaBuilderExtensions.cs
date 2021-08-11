using HotChocolate.Lodash;

namespace HotChocolate
{
    public static class AggregationDirectiveSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddAggregationDirectives(this ISchemaBuilder schemaBuilder) =>
            schemaBuilder
                .AddDirectiveType<ChunkDirectiveType>()
                .AddDirectiveType<CountByDirectiveType>()
                .AddDirectiveType<DropDirectiveType>()
                .AddDirectiveType<DropRightDirectiveType>()
                .AddDirectiveType<FlattenDirectiveType>()
                .AddDirectiveType<GroupByDirectiveType>()
                .AddDirectiveType<KeyByDirectiveType>()
                .AddDirectiveType<KeysDirectiveType>()
                .AddDirectiveType<MapDirectiveType>()
                .AddDirectiveType<MaxByDirectiveType>()
                .AddDirectiveType<MeanByDirectiveType>()
                .AddDirectiveType<MinByDirectiveType>()
                .AddDirectiveType<SumByDirectiveType>()
                .AddDirectiveType<TakeDirectiveType>()
                .AddDirectiveType<TakeRightDirectiveType>()
                .AddDirectiveType<UniqDirectiveType>()
                .AddDirectiveType<UniqByDirectiveType>();
    }
}
