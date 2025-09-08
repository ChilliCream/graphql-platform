namespace HotChocolate.StarWars.Models;

/// <summary>
/// A review of a particular movie.
/// </summary>
public class Review(int stars, string? commentary = null)
{
    /// <summary>
    /// The number of stars given for this review.
    /// </summary>
    public int Stars { get; set; } = stars;

    /// <summary>
    /// An explanation for the rating.
    /// </summary>
    public string? Commentary { get; set; } = commentary;
}
