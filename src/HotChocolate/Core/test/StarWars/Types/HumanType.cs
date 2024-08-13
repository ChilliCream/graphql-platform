using HotChocolate.Types;
using HotChocolate.StarWars.Models;
using HotChocolate.StarWars.Resolvers;

namespace HotChocolate.StarWars.Types;

public class HumanType : ObjectType<Human>
{
    protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
    {
        descriptor.Implements<CharacterType>();

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Name).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.AppearsIn).Type<ListType<EpisodeType>>();

        descriptor
            .Field<SharedResolvers>(r => r.GetCharacter(default!, default!))
            .UsePaging<CharacterType>()
            .Name("friends")
            .Parallel();

        descriptor.Field<SharedResolvers>(r => r.GetOtherHuman(default!, default!));

        descriptor.Field<SharedResolvers>(t => t.GetHeight(default, default!))
            .Type<FloatType>()
            .Argument("unit", a => a.Type<EnumType<Unit>>())
            .Name("height");
    }
}
