using HotChocolate.Lodash;

namespace HotChocolate
{
    public static class AggregationDirectiveSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddAggregationDirectives(this ISchemaBuilder schemaBuilder) =>
            schemaBuilder
                .AddDirectiveType<ChunkDirectiveType>()
                .AddDirectiveType<CountDirectiveType>()
                .AddDirectiveType<DropDirectiveType>()
                .AddDirectiveType<DropRightDirectiveType>()
                .AddDirectiveType<FlattenDirectiveType>()
                .AddDirectiveType<GroupByDirectiveType>()
                .AddDirectiveType<KeyByDirectiveType>()
                .AddDirectiveType<KeysDirectiveType>()
                .AddDirectiveType<MapDirectiveType>()
                .AddDirectiveType<MaxDirectiveType>()
                .AddDirectiveType<MeanByDirectiveType>()
                .AddDirectiveType<MinByDirectiveType>()
                .AddDirectiveType<SumByDirectiveType>()
                .AddDirectiveType<TakeDirectiveType>()
                .AddDirectiveType<TakeRightDirectiveType>()
                .AddDirectiveType<UniqueDirectiveType>();
    }
}
