using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace StarWars.Characters
{
    /// <summary>
    /// A droid in the Star Wars universe.
    /// </summary>
    public class Droid : ICharacter
    {
        public Droid(
            int id,
            string name,
            IReadOnlyList<int> friends,
            IReadOnlyList<Episode> appearsIn,
            string primaryFunction,
            double height = 1.72d)
        {
            Id = id;
            Name = name;
            Friends = friends;
            AppearsIn = appearsIn;
            PrimaryFunction = primaryFunction;
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
        /// The droid's primary function.
        /// </summary>
        public string PrimaryFunction { get; }

        /// <inheritdoc />
        [UseConvertUnit]
        public double Height { get; }
    }
}
