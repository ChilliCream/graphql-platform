using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class HumanDataLoader
        : DataLoaderBase<string, Human>
    {
        private readonly CharacterRepository _repository;

        public HumanDataLoader(CharacterRepository repository)
            : base(new DataLoaderOptions<string>())
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public List<IReadOnlyList<string>> Loads { get; } =
            new List<IReadOnlyList<string>>();

        protected override Task<IReadOnlyList<IResult<Human>>> Fetch(
            IReadOnlyList<string> keys)
        {
            var result = _repository.GetHumans(keys).ToDictionary(t => t.Id);
            var list = new List<Result<Human>>();

            foreach (string key in keys)
            {
                if (result.TryGetValue(key, out Human human))
                {
                    list.Add(Result<Human>.Resolve(human));
                }
                else
                {
                    list.Add(Result<Human>.Resolve(null));
                }
            }

            return Task.FromResult<IReadOnlyList<IResult<Human>>>(list);
        }
    }
}
