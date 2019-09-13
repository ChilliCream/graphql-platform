using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using StarWars.Data;
using StarWars.Models;

namespace StarWars
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
        public ICharacter GetHero(Episode episode)
        {
            return _repository.GetHero(episode);
        }

        /// <summary>
        /// Gets a human by Id.
        /// </summary>
        /// <param name="id">The Id of the human to retrieve.</param>
        /// <returns>The human.</returns>
        public Human GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

        /// <summary>
        /// Get a particular droid by Id.
        /// </summary>
        /// <param name="id">The Id of the droid.</param>
        /// <returns>The droid.</returns>
        public Droid GetDroid(string id)
        {
            return _repository.GetDroid(id);
        }

        public IEnumerable<ICharacter> GetCharacter(string[] characterIds, IResolverContext context)
        {
            foreach (string characterId in characterIds)
            {
                ICharacter character = _repository.GetCharacter(characterId);
                if (character == null)
                {
                    context.ReportError(
                        "Could not resolve a charachter for the " +
                        $"character-id {characterId}.");
                }
                else
                {
                    yield return character;
                }
            }
        }

        public IEnumerable<object> Search(string text)
        {
            return _repository.Search(text);
        }
    }
}
