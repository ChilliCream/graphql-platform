using StarWars.Characters;

namespace StarWars.Reviews
{
    /// <summary>
    /// This payload allows us to query the created review object.
    /// </summary>
    public class CreateReviewPayload
    {
        /// <summary>
        /// Creates a new instance of <see cref="CreateReviewPayload"/>.
        /// </summary>
        /// <param name="episode">
        /// The episode for which a review was created.
        /// </param>
        /// <param name="review">
        /// The review that was being created.
        /// </param>
        public CreateReviewPayload(Episode episode, Review review)
        {
            Episode = episode;
            Review = review;
        }

        /// <summary>
        /// The episode for which a review was created.
        /// </summary>
        public Episode Episode { get; }

        /// <summary>
        /// The review that was being created.
        /// </summary>
        public Review Review { get; }
    }
}
