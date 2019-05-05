using System.Collections.Generic;

namespace StarWars.Models
{
    /// <summary>
    /// A character in the Star Wars universe.
    /// </summary>
    public interface ICharacter
    {
        /// <summary>
        /// The unique identifier for the character.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the character.
        /// </summary>
        string Name { get; }

        IReadOnlyList<string> Friends { get; }

        IReadOnlyList<Episode> AppearsIn { get; }

        double Height { get; }
    }
}
