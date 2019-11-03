using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public interface IStarWarsClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync(
            Optional<Episode?> episode = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            GetHeroOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<IGetHuman>> GetHumanAsync(
            Optional<string> id = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<IGetHuman>> GetHumanAsync(
            GetHumanOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<ISearch>> SearchAsync(
            Optional<string> text = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<ISearch>> SearchAsync(
            SearchOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            Optional<Episode> episode = default,
            Optional<ReviewInput> review = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            CreateReviewOperation operation,
            CancellationToken cancellationToken = default);

        Task<IResponseStream<IOnReview>> OnReviewAsync(
            Optional<Episode> episode = default,
            CancellationToken cancellationToken = default);

        Task<IResponseStream<IOnReview>> OnReviewAsync(
            OnReviewOperation operation,
            CancellationToken cancellationToken = default);
    }
}
