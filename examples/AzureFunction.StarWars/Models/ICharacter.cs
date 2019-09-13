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

        /// <summary>
        /// The names of the character's friends.
        /// </summary>
        IReadOnlyList<string> Friends { get; }

        /// <summary>
        /// The episodes the character appears in.
        /// </summary>
        IReadOnlyList<Episode> AppearsIn { get; }

        /// <summary>
        /// The height of the character.
        /// </summary>
        double Height { get; }
    }
}
