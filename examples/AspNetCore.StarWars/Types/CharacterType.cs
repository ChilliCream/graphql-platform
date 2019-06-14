using HotChocolate.Types;
using HotChocolate.Types.Relay;
using StarWars.Models;

namespace StarWars.Types
{
    public class CharacterType
        : InterfaceType<ICharacter>
    {
        protected override void Configure(IInterfaceTypeDescriptor<ICharacter> descriptor)
        {
            descriptor.Name("Character");

            descriptor.Field(f => f.Id)
                .Type<NonNullType<IdType>>();

            descriptor.Field(f => f.Name)
                .Type<StringType>();

            descriptor.Field(f => f.Friends)
                .UsePaging<CharacterType>();

            descriptor.Field(f => f.AppearsIn)
                .Type<ListType<EpisodeType>>();

            descriptor.Field(f => f.Height)
                .Type<FloatType>()
                .Argument("unit", a => a.Type<EnumType<Unit>>());
        }
    }
}
