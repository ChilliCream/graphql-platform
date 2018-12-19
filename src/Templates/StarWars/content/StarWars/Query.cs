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

        public ICharacter GetHero(Episode episode)
        {
            return _repository.GetHero(episode);
        }

        public Human GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

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
