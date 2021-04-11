using System;

namespace StarWars.Reviews
{
    /// <summary>
    /// A review of a particular movie.
    /// </summary>
    public class Review
    {
        /// <summary>
        /// Creates a new instance of <see cref="Review"/>.
        /// </summary>
        /// <param name="stars">
        /// The number of stars given for this review.
        /// </param>
        /// <param name="commentary">
        /// The explanation for the rating.
        /// </param>
        public Review(int stars, string commentary)
        {
            Id = Guid.NewGuid();
            Stars = stars;
            Commentary = commentary;
        }

        /// <summary>
        /// The ID of the review.
        /// </summary>
        public Guid Id { get; }

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
