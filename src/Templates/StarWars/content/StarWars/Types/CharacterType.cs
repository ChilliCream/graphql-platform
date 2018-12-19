using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using StarWars.Data;
using StarWars.Models;

namespace StarWars.Types
{
    public class CharacterType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Character");

            descriptor.Field("id")
                .Type<NonNullType<IdType>>();

            descriptor.Field("name")
                .Type<StringType>();

            descriptor.Field("friends")
                .Type<ListType<CharacterType>>();

            descriptor.Field("appearsIn")
                .Type<ListType<EpisodeType>>();

            descriptor.Field("height")
                .Type<FloatType>()
                .Argument("unit", a => a.Type<EnumType<Unit>>());
        }
    }
}
