using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class CharacterType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Character");
            descriptor.Field("id").Type<NonNullType<StringType>>();
            descriptor.Field("name").Type<StringType>();
            descriptor.Field("friends").Type<ListType<CharacterType>>();
            descriptor.Field("appearsIn").Type<ListType<EpisodeType>>();
            descriptor.Field("height").Type<FloatType>()
                .Argument("unit", a => a.Type<EnumType<Unit>>());
        }
    }

    public class CommonResolvers
    {
        public IEnumerable<ICharacter> GetCharacter(
            [Parent]ICharacter character,
            [Service]CharacterRepository repository)
        {
            foreach (string friendId in character.Friends)
            {
                ICharacter friend = repository.GetCharacter(friendId);
                if (friend != null)
                {
                    yield return friend;
                }
            }
        }

        public double GetHeight(Unit? unit, [Parent]ICharacter character)
        {
            double height = character.Height;
            if (unit == Unit.Foot)
            {
                return height * 3.28084d;
            }
            return height;
        }
    }
}
