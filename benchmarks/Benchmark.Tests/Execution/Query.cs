using System.Collections.Generic;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public class Query
    {
        private readonly CharacterRepository _repository;

        public Query(CharacterRepository repository)
        {
            _repository = repository
                ?? throw new System.ArgumentNullException(nameof(repository));
        }

        public ICharacter GetHero(Episode episode)
        {
            return _repository.GetHero(episode);
        }

        public IEnumerable<ICharacter> GetHeroes(Episode[] episodes)
        {
            List<ICharacter> result = new List<ICharacter>();
            foreach (Episode episode in episodes)
            {
                result.Add(_repository.GetHero(episode));
            }

            return result;
        }

        public Human GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

        public Droid GetDroid(string id)
        {
            return _repository.GetDroid(id);
        }

        public IEnumerable<object> Search(string text)
        {
            return _repository.Search(text);
        }
    }
}
