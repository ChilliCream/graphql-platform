using System.Collections.Generic;

namespace StarWars.Models
{
    /// <summary>
    /// A droid in the Star Wars universe.
    /// </summary>
    public class Droid
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
        /// The droid's primary function.
        /// </summary>
        public string PrimaryFunction { get; set; }

        /// <inheritdoc />
        public double Height { get; } = 1.72d;
    }
}
