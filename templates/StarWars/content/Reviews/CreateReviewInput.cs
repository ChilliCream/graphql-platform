using StarWars.Characters;

namespace StarWars.Reviews
{
    /// <summary>
    /// This input represents the data needed to create a review.
    /// </summary>
    public class CreateReviewInput
    {
        /// <summary>
        /// Creates a new instance of <see cref="CreateReviewInput"/>.
        /// </summary>
        /// <param name="episode">
        /// The review for which to create the review.
        /// </param>
        /// <param name="stars">
        /// The number of stars given for this review.
        /// </param>
        /// <param name="commentary">
        /// An explanation for the rating.
        /// </param>
        public CreateReviewInput(Episode episode, int stars, string commentary)
        {
            Episode = episode;
            Stars = stars;
            Commentary = commentary;
        }

        /// <summary>
        /// The review for which to create the review.
        /// </summary>
        public Episode Episode { get; }

        /// <summary>
        /// The number of stars given for this review.
        /// </summary>
        public int Stars { get; }

        /// <summary>
        /// An explanation for the rating.
        /// </summary>
        public string Commentary { get; }
    }
}
