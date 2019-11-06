using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
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
            Optional<Episode?> episode = default,
            CancellationToken cancellationToken = default)
        {

            return _executor.ExecuteAsync(
                new GetHeroOperation { Episode = episode },
                cancellationToken);
        }

        public Task<IOperationResult<IGetHero>> GetHeroAsync(
            GetHeroOperation operation,
            CancellationToken cancellationToken = default)
        {
            if(operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<IGetHuman>> GetHumanAsync(
            Optional<string> id = default,
            CancellationToken cancellationToken = default)
        {
            if (id.HasValue && id.Value is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return _executor.ExecuteAsync(
                new GetHumanOperation { Id = id },
                cancellationToken);
        }

        public Task<IOperationResult<IGetHuman>> GetHumanAsync(
            GetHumanOperation operation,
            CancellationToken cancellationToken = default)
        {
            if(operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<IGetCharacter>> GetCharacterAsync(
            Optional<IReadOnlyList<string?>> ids = default,
            CancellationToken cancellationToken = default)
        {
            if (ids.HasValue && ids.Value is null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return _executor.ExecuteAsync(
                new GetCharacterOperation { Ids = ids },
                cancellationToken);
        }

        public Task<IOperationResult<IGetCharacter>> GetCharacterAsync(
            GetCharacterOperation operation,
            CancellationToken cancellationToken = default)
        {
            if(operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<ISearch>> SearchAsync(
            Optional<string> text = default,
            CancellationToken cancellationToken = default)
        {
            if (text.HasValue && text.Value is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return _executor.ExecuteAsync(
                new SearchOperation { Text = text },
                cancellationToken);
        }

        public Task<IOperationResult<ISearch>> SearchAsync(
            SearchOperation operation,
            CancellationToken cancellationToken = default)
        {
            if(operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            Optional<Episode> episode = default,
            Optional<ReviewInput> review = default,
            CancellationToken cancellationToken = default)
        {
            if (review.HasValue && review.Value is null)
            {
                throw new ArgumentNullException(nameof(review));
            }

            return _executor.ExecuteAsync(
                new CreateReviewOperation
                {
                    Episode = episode, 
                    Review = review
                },
                cancellationToken);
        }

        public Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            CreateReviewOperation operation,
            CancellationToken cancellationToken = default)
        {
            if(operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }
    }
}
