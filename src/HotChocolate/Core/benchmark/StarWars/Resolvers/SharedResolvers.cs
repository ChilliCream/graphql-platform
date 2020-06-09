using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Resolvers
{
    public class SharedResolvers
    {
        public async Task<IEnumerable<ICharacter>> GetCharacter(
            [Parent]ICharacter character,
            [Service]CharacterRepository repository)
        {
            await Task.Delay(2);

            var results = new List<ICharacter>();

            foreach (string friendId in character.Friends)
            {
                ICharacter friend = await repository.GetCharacter(friendId);

                if (friend != null)
                {
                    results.Add(friend);
                }
            }

            return results;
        }

        public async Task<double> GetHeight(Unit? unit, [Parent]ICharacter character)
        {
            await Task.Delay(2);

            return ConvertToUnit(character.Height, unit);
        }

        public async Task<double> GetLength(Unit? unit, [Parent]Starship starship)
        {
            await Task.Delay(2);

            return ConvertToUnit(starship.Length, unit);
        }

        private double ConvertToUnit(double length, Unit? unit)
        {
            return (unit == Unit.Foot) ? length * 3.28084d : length;
        }
    }
}
