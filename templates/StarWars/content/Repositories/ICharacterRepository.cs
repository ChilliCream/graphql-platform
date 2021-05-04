using System.Collections.Generic;
using System.Linq;
using StarWars.Characters;

namespace StarWars.Repositories
{
    public interface ICharacterRepository
    {
        IQueryable<ICharacter> GetCharacters();

        IEnumerable<ICharacter> GetCharacters(params int[] ids);

        ICharacter GetHero(Episode episode);

        IEnumerable<ISearchResult> Search(string text);
    }
}
