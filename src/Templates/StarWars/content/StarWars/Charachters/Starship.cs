namespace StarWars.Characters
{
    /// <summary>
    /// A starship in the Star Wars universe.
    /// </summary>
    public class Starship : ISearchResult
    {
        public Starship(int id, string name, double length)
        {
            Id = id;
            Name = name;
            Length = length;
        }

        /// <summary>
        /// The Id of the starship.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The name of the starship.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The length of the starship.
        /// </summary>
        [UseConvertUnit]
        public double Length { get; }
    }
}
