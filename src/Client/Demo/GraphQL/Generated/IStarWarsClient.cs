using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public interface IStarWarsClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode);

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode,
            CancellationToken cancellationToken);

        Task<IOperationResult<IGetHuman>> GetHumanAsync(
            string id);

        Task<IOperationResult<IGetHuman>> GetHumanAsync(
            string id,
            CancellationToken cancellationToken);

        Task<IOperationResult<ISearch>> SearchAsync(
            string text);

        Task<IOperationResult<ISearch>> SearchAsync(
            string text,
            CancellationToken cancellationToken);

        Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            Episode episode,
            ReviewInput review);

        Task<IOperationResult<ICreateReview>> CreateReviewAsync(
            Episode episode,
            ReviewInput review,
            CancellationToken cancellationToken);
    }
}
