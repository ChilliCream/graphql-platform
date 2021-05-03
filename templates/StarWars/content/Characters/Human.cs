using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace StarWars.Characters
{
    /// <summary>
    /// A human character in the Star Wars universe.
    /// </summary>
    public class Human : ICharacter
    {
        public Human(
            int id, 
            string name, 
            IReadOnlyList<int> friends, 
            IReadOnlyList<Episode> appearsIn, 
            string? homePlanet = null, 
            double height = 1.72d)
        {
            Id = id;
            Name = name;
            Friends = friends;
            AppearsIn = appearsIn;
            HomePlanet = homePlanet;
            Height = height;
        }

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IReadOnlyList<int> Friends { get; }

        /// <inheritdoc />
        public IReadOnlyList<Episode> AppearsIn { get; }

        /// <summary>
        /// The planet the character is originally from.
        /// </summary>
        public string? HomePlanet { get; }

        /// <inheritdoc />
        [UseConvertUnit]
        public double Height { get; }
    }
}
