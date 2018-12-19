using HotChocolate.Types;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetHero(default))
                .Type<CharacterType>()
                .Argument("episode", a => a.DefaultValue(Episode.NewHope));

            descriptor.Field(t => t.GetHeroes(default))
                .Type<NonNullType<ListType<NonNullType<CharacterType>>>>()
                .Argument("episodes", a => a.Type<NonNullType<ListType<NonNullType<EpisodeType>>>>());

            descriptor.Field(t => t.Search(default))
                .Type<ListType<SearchResultType>>();
        }
    }

}
