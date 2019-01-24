using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using StarWars.Models;

namespace StarWars.Types
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetHero(default))
                .Type<CharacterType>()
                .Argument("episode", a => a.DefaultValue(Episode.NewHope));

            descriptor.Field(t => t.GetCharacter(default, default))
                .Type<NonNullType<ListType<NonNullType<CharacterType>>>>()
                .Argument("characterIds",
                    a => a.Type<NonNullType<ListType<NonNullType<IdType>>>>());

            descriptor.Field(t => t.GetHuman(default))
                .Argument("id", a => a.Type<NonNullType<IdType>>());

            descriptor.Field(t => t.GetDroid(default))
                .Argument("id", a => a.Type<NonNullType<IdType>>());

            // the search can only be executed if the current
            // identity has a country
            descriptor.Field(t => t.Search(default))
                .Type<ListType<SearchResultType>>()
                .Directive(new AuthorizeDirective("HasCountry"));
        }
    }

}
