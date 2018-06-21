namespace HotChocolate.Integration
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

        public Human GetHuman(string id)
        {
            return _repository.GetHuman(id);
        }

        public Droid GetDroid(string id)
        {
            return _repository.GetDroid(id);
        }
    }
}
