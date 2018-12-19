using System.Collections.Generic;

namespace StarWars.Models
{
    public class Human
        : ICharacter
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public IReadOnlyList<string> Friends { get; set; }

        public IReadOnlyList<Episode> AppearsIn { get; set; }

        public string HomePlanet { get; set; }

        public double Height { get; } = 1.72d;
    }
}
