using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using StarWars.Repositories;

namespace StarWars.Characters
{
    /// <summary>
    /// The queries related to characters.
    /// </summary>
    [ExtendObjectType(OperationTypeNames.Query)]
    public class CharacterQueries
    {
        /// <summary>
        /// Retrieve a hero by a particular Star Wars episode.
        /// </summary>
        /// <param name="episode">The episode to retrieve the hero.</param>
        /// <param name="repository">The character repository.</param>
        /// <returns>The hero character.</returns>
        public ICharacter GetHero(
            Episode episode,
            [Service] ICharacterRepository repository) =>
            repository.GetHero(episode);

        /// <summary>
        /// Gets all character.
        /// </summary>
        /// <param name="repository">The character repository.</param>
        /// <returns>The character.</returns>
        [UsePaging(typeof(InterfaceType<ICharacter>))]
        [UseFiltering]
        [UseSorting]
        public IEnumerable<ICharacter> GetCharacters(
            [Service] ICharacterRepository repository) =>
            repository.GetCharacters();

        /// <summary>
        /// Gets a character by it`s id.
        /// </summary>
        /// <param name="ids">The ids of the human to retrieve.</param>
        /// <param name="repository">The character repository.</param>
        /// <returns>The character.</returns>
        public IEnumerable<ICharacter> GetCharacter(
            int[] ids,
            [Service] ICharacterRepository repository) =>
            repository.GetCharacters(ids);

        /// <summary>
        /// Search the repository for objects that contain the text.
        /// </summary>
        /// <param name="text">
        /// The text we are searching for.
        /// </param>
        /// <param name="repository">The character repository.</param>
        /// <returns>Returns the union type <see cref="ISearchResult"/>.</returns>
        public IEnumerable<ISearchResult> Search(
            string text,
            [Service] ICharacterRepository repository) =>
            repository.Search(text);
    }
}
