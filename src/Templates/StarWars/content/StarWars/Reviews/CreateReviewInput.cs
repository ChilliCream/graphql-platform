using StarWars.Characters;

namespace StarWars.Reviews
{
    public class CreateReviewInput
    {
        public CreateReviewInput(Episode episode, int stars, string commentary)
        {
            Episode = episode;
            Stars = stars;
            Commentary = commentary;
        }

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
