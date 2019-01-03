using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        protected override Task<IReadOnlyList<Result<Human>>> FetchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var result = _repository.GetHumans(keys).ToDictionary(t => t.Id);
            var list = new List<Result<Human>>();

            foreach (string key in keys)
            {
                if (result.TryGetValue(key, out Human human))
                {
                    list.Add(human);
                }
                else
                {
                    list.Add((Human)null);
                }
            }

            return Task.FromResult<IReadOnlyList<Result<Human>>>(list);
        }
    }
}
