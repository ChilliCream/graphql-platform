using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.StarWars.Data;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars
{
    public class Query
    {
        private readonly CharacterRepository _repository;

        public Query(CharacterRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Retrieve a hero by a particular Star Wars episode.
        /// </summary>
        /// <param name="episode">The episode to look up by.</param>
        /// <returns>The character.</returns>
        public Task<ICharacter> GetHero(Episode episode)
        {
            return _repository.GetHero(episode);
        }

        /// <summary>
        /// Gets a human by Id.
        /// </summary>
        /// <param name="id">The Id of the human to retrieve.</param>
        /// <returns>The human.</returns>
        public Task<Human> GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

        /// <summary>
        /// Get a particular droid by Id.
        /// </summary>
        /// <param name="id">The Id of the droid.</param>
        /// <returns>The droid.</returns>
        public Task<Droid> GetDroid(string id)
        {
            return _repository.GetDroid(id);
        }

        public async Task<IEnumerable<ICharacter>> GetCharacter(string[] characterIds, IResolverContext context)
        {
            var characters = new ICharacter[characterIds.Length];

            for (var i = 0; i < characterIds.Length; i++)
            {
                string characterId = characterIds[i];
                ICharacter character = await _repository.GetCharacter(characterId);

                if (character == null)
                {
                    context.ReportError(
                        "Could not resolve a charachter for the " +
                        $"character-id {characterId}.");
                }
                else
                {
                    characters[i] = character;
                }
            }

            return characters;
        }

        public Task<IEnumerable<object>> Search(string text)
        {
            return _repository.Search(text);
        }
    }
}
