using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Benchmark.Tests.Execution
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

        public static IEnumerable<ICharacter> GetCharacter(
            IResolverContext context)
        {
            ICharacter character = context.Parent<ICharacter>();
            CharacterRepository repository = context.Service<CharacterRepository>();
            foreach (string friendId in character.Friends)
            {
                ICharacter friend = repository.GetCharacter(friendId);
                if (friend != null)
                {
                    yield return friend;
                }
            }
        }

        public static double GetHeight(
            IResolverContext context)
        {
            double height = context.Parent<ICharacter>().Height;
            if (context.Argument<Unit?>("unit") == Unit.Foot)
            {
                return height * 3.28084d;
            }
            return height;
        }
    }
}
