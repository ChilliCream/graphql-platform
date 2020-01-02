using System;

namespace StarWars.Reviews
{
    /// <summary>
    /// A review of a particular movie.
    /// </summary>
    public class Review
    {
        public Review(int stars, string commentary)
        {
            Id = Guid.NewGuid();
            Stars = stars;
            Commentary = commentary;
        }

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
