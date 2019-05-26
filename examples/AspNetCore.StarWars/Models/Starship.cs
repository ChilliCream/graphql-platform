namespace StarWars.Models
{
    /// <summary>
    /// A starship in the Star Wars universe.
    /// </summary>
    public class Starship
    {
        /// <summary>
        /// The Id of the starship.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the starship.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The length of the starship.
        /// </summary>
        public double Length { get; set; }
    }
}
