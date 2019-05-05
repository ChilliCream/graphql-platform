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

        public IReadOnlyList<string> Friends { get; set; }

        public IReadOnlyList<Episode> AppearsIn { get; set; }

        public string PrimaryFunction { get; set; }

        public double Height { get; } = 1.72d;
    }
}
