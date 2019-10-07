using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public class StarWarsClient
        : IStarWarsClient
    {
        private readonly IOperationExecutor _executor;

        public StarWarsClient(IOperationExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode) =>
            GetHeroAsync(episode, CancellationToken.None);

        public Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode,
            CancellationToken cancellationToken)
        {

            return _executor.ExecuteAsync(
                new GetHeroOperation {Episode = episode },
                cancellationToken);
        }

        public Task<IOperationResult<IGetHuman>> GetHumanAsync(
            string id) =>
            GetHumanAsync(id, CancellationToken.None);

        public Task<IOperationResult<IGetHuman>> GetHumanAsync(
            string id,
            CancellationToken cancellationToken)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return _executor.ExecuteAsync(
                new GetHumanOperation {Id = id },
                cancellationToken);
        }

        public Task<IOperationResult<ISearch>> SearchAsync(
            string text) =>
            SearchAsync(text, CancellationToken.None);

        public Task<IOperationResult<ISearch>> SearchAsync(
            string text,
            CancellationToken cancellationToken)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return _executor.ExecuteAsync(
                new SearchOperation {Text = text },
                cancellationToken);
        }
    }
}
