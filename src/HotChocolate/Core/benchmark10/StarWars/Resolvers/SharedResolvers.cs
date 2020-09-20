using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Resolvers
{
    public class SharedResolvers
    {
        public Task<IReadOnlyList<ICharacter>> GetCharacterAsync(
            [Parent]ICharacter character,
            [Service]CharacterRepository repository)
        {
            return repository.GetCharacters(character.Friends);
        }

        public double GetHeight(Unit? unit, [Parent]ICharacter character)
        {
            return ConvertToUnit(character.Height, unit);
        }

        public double GetLength(Unit? unit, [Parent]Starship starship)
        {
            return ConvertToUnit(starship.Length, unit);
        }

        private double ConvertToUnit(double length, Unit? unit)
        {
            return (unit == Unit.Foot) ? length * 3.28084d : length;
        }
    }
}
