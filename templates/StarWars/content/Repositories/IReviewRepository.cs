using System.Collections.Generic;
using StarWars.Characters;
using StarWars.Reviews;

namespace StarWars.Repositories
{
    public interface IReviewRepository
    {
        void AddReview(Episode episode, Review review);
        IEnumerable<Review> GetReviews(Episode episode);
    }
}
