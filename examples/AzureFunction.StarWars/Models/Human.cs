using System.Collections.Generic;

namespace StarWars.Models
{
    /// <summary>
    /// A human character in the Star Wars universe.
    /// </summary>
    public class Human
        : ICharacter
    {
        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<string> Friends { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<Episode> AppearsIn { get; set; }

        /// <summary>
        /// The planet the character is originally from.
        /// </summary>
        public string HomePlanet { get; set; }

        /// <inheritdoc />
        public double Height { get; } = 1.72d;
    }
}
