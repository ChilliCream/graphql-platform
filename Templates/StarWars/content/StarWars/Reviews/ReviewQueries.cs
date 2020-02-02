using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using StarWars.Characters;
using StarWars.Repositories;

namespace StarWars.Reviews
{
    [ExtendObjectType(Name = "Query")]
    public class ReviewQueries
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IEnumerable<Review> GetReviews(
            Episode episode,
            [Service]IReviewRepository repository) =>
            repository.GetReviews(episode);
    }
}
