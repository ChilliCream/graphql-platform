using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using StarWars.Repositories;

namespace StarWars.Characters
{
    [ExtendObjectType(Name = "Query")]
    public class CharacterQueries
    {
        /// <summary>
        /// Retrieve a hero by a particular Star Wars episode.
        /// </summary>
        /// <param name="episode">The episode to look up by.</param>
        /// <param name="repository"></param>
        /// <returns>The character.</returns>
        public ICharacter GetHero(
            Episode episode,
            [Service]ICharacterRepository repository) =>
            repository.GetHero(episode);

        /// <summary>
        /// Gets all character.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>The character.</returns>
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IEnumerable<ICharacter> GetCharacters(
            [Service]ICharacterRepository repository) =>
            repository.GetCharacters();

        /// <summary>
        /// Gets a character by it`s id.
        /// </summary>
        /// <param name="ids">The ids of the human to retrieve.</param>
        /// <param name="repository"></param>
        /// <returns>The character.</returns>
        public IEnumerable<ICharacter> GetCharacter(
            int[] ids,
            [Service]ICharacterRepository repository) =>
            repository.GetCharacters(ids);

        public IEnumerable<ISearchResult> Search(
            string text,
            [Service]ICharacterRepository repository) =>
            repository.Search(text);
    }
}
