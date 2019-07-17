namespace StarWars.Models
{
    /// <summary>
    /// A review of a particular movie.
    /// </summary>
    public class Review
    {
        /// <summary>
        /// The number of stars given for this review.
        /// </summary>
        public int Stars { get; set; }

        /// <summary>
        /// An explanation for the rating.
        /// </summary>
        public string Commentary { get; set; }
    }
}
