using System.Collections.Generic;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
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
